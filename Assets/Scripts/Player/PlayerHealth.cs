using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [System.Serializable]
    public class HealthEvent : UnityEvent<int, int> { }

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Events")]
    public HealthEvent OnHealthChanged;
    public UnityEvent OnDamageTaken;
    public UnityEvent OnDeath;
    public UnityEvent OnHeal;

    [Header("Death Anim Settings")]
    [SerializeField] private Animator animator;            // 🔥 Ahora asignable desde el inspector
    [SerializeField] private string deathTriggerName = "DEATH";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private bool useUnscaledTimeOnDeath = true;
    [SerializeField] private float enterStateTimeout = 1.0f;
    [SerializeField] private bool logDeathDebug = false;

    private int currentHealth;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private Rigidbody2D rb;
    private RigidbodyType2D originalBodyType;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercentage => (float)currentHealth / maxHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalBodyType = rb.bodyType;

        // Autoasignar Animator si no se asigna manualmente
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();

        currentHealth = maxHealth;

        if (playerSprite != null)
            originalColor = playerSprite.color;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (playerSprite == null)
        {
            playerSprite = GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null) originalColor = playerSprite.color;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke();

        if (playerSprite != null && flashCoroutine == null)
            flashCoroutine = StartCoroutine(FlashSprite());

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHeal?.Invoke();
    }

    public void RestoreToMax()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHeal?.Invoke();

        if (rb != null)
        {
            rb.bodyType = originalBodyType == RigidbodyType2D.Static ? RigidbodyType2D.Dynamic : originalBodyType;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        var movement = GetComponent<PlayerMovement>();
        var attack = GetComponent<PlayerAttack>();
        if (movement != null) movement.SetMovementEnabled(true);
        if (attack != null) attack.SetAttackEnabled(true);
    }

    public void IncreaseMaxHealth(int increaseAmount)
    {
        maxHealth += increaseAmount;
        currentHealth += increaseAmount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        var movement = GetComponent<PlayerMovement>();
        var attack = GetComponent<PlayerAttack>();
        if (movement != null) movement.SetMovementEnabled(false);
        if (attack != null) attack.SetAttackEnabled(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        PlayDeathAnim();

        OnDeath?.Invoke();
        if (logDeathDebug) Debug.Log("[PlayerHealth] Player died → death trigger sent.");
        GameOverUI.Instance?.Show();
    }

    private void PlayDeathAnim()
    {
        if (animator == null) return;

        animator.speed = 1f;
        if (useUnscaledTimeOnDeath)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Reset triggers/bools comunes
        SafeReset(animator, "HIT");
        SafeReset(animator, "ATTACK");
        SafeSetBool(animator, "IsMoving", false);
        SafeSetBool(animator, "isMoving", false);
        SafeSetBool(animator, "Alive", false);
        SafeSetBool(animator, "isAlive", false);

        if (!string.IsNullOrEmpty(deathTriggerName))
            animator.SetTrigger(deathTriggerName);

        StartCoroutine(EnsureDeathStateEntered());
    }

    private System.Collections.IEnumerator EnsureDeathStateEntered()
    {
        if (animator == null || string.IsNullOrEmpty(deathStateName)) yield break;

        float start = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;
        int layer = 0;

        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(layer);
            if (st.IsName(deathStateName)) break;

            float now = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;
            if (now - start > enterStateTimeout)
            {
                if (logDeathDebug) Debug.LogWarning("[PlayerHealth] Timeout esperando entrar al estado de muerte. Revisa el nombre del state/transition.");
                yield break;
            }
            yield return null;
        }

        if (logDeathDebug) Debug.Log("[PlayerHealth] Death state entered OK.");
    }

    private void SafeReset(Animator anim, string triggerName)
    {
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                anim.ResetTrigger(triggerName);
    }

    private void SafeSetBool(Animator anim, string boolName, bool value)
    {
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == boolName)
                anim.SetBool(boolName, value);
    }

    private System.Collections.IEnumerator FlashSprite()
    {
        if (playerSprite != null)
        {
            playerSprite.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            playerSprite.color = originalColor;
        }
        flashCoroutine = null;
    }

    public void SetFlashEffect(Color flashColor, float duration)
    {
        if (playerSprite != null && flashCoroutine == null)
            flashCoroutine = StartCoroutine(CustomFlash(flashColor, duration));
    }

    private System.Collections.IEnumerator CustomFlash(Color flashColor, float duration)
    {
        if (playerSprite != null)
        {
            playerSprite.color = flashColor;
            yield return new WaitForSeconds(duration);
            playerSprite.color = originalColor;
        }
        flashCoroutine = null;
    }

    [ContextMenu("Take 10 Damage")] private void DebugTakeDamage() => TakeDamage(10);
    [ContextMenu("Heal 20 Health")] private void DebugHeal() => Heal(20);
}
