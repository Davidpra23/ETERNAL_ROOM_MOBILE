using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [System.Serializable]
    public class HealthEvent : UnityEvent<int, int> { } // CurrentHealth, MaxHealth

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

    private int currentHealth;
    private Color originalColor;
    private Coroutine flashCoroutine;

    // Propiedades públicas
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public float HealthPercentage { get { return (float)currentHealth / maxHealth; } }

    void Awake()
    {
        currentHealth = maxHealth;
        if (playerSprite != null)
            originalColor = playerSprite.color;
    }

    void Start()
    {
        // Notificar el valor inicial de salud
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return;

        // Aplicar daño
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        
        // Notificar cambio de salud
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke();

        // Feedback visual
        if (playerSprite != null && flashCoroutine == null)
            flashCoroutine = StartCoroutine(FlashSprite());

        // Comprobar muerte
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHeal?.Invoke();
    }

    public void IncreaseMaxHealth(int increaseAmount)
    {
        maxHealth += increaseAmount;
        currentHealth += increaseAmount;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        // Desactivar movimiento y ataques
        PlayerMovement movement = GetComponent<PlayerMovement>();
        PlayerAttack attack = GetComponent<PlayerAttack>();
        
        if (movement != null) movement.SetMovementEnabled(false);
        if (attack != null) attack.SetAttackEnabled(false);

        // Reproducir animación de muerte
        PlayerAnimation anim = GetComponent<PlayerAnimation>();
        if (anim != null) anim.PlayDeathAnimation();

        // Invocar evento de muerte
        OnDeath?.Invoke();

        Debug.Log("Player died!");
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

    // Métodos para power-ups y mejoras (sin invencibilidad)
    public void SetFlashEffect(Color flashColor, float duration)
    {
        if (playerSprite != null && flashCoroutine == null)
        {
            flashCoroutine = StartCoroutine(CustomFlash(flashColor, duration));
        }
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

    // Para debugging
    [ContextMenu("Take 10 Damage")]
    private void DebugTakeDamage()
    {
        TakeDamage(10);
    }

    [ContextMenu("Heal 20 Health")]
    private void DebugHeal()
    {
        Heal(20);
    }
}