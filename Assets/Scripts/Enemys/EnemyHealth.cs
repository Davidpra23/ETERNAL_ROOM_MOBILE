using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int coinReward = 1;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private float deathEffectDelay = 0f;
    [SerializeField] private bool flashOnDamage = true;

    [Header("Component References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D enemyCollider;
    [SerializeField] private Rigidbody2D enemyRigidbody;

    private int currentHealth;
    private bool isDead = false;
    private Color originalColor;
    private Vector3 deathPosition;

    public System.Action OnDeath;
    public System.Action OnDamageTaken;
    public System.Action<int> OnHealthChanged;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        if (enemyRigidbody == null)
            enemyRigidbody = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        OnDamageTaken?.Invoke();
        OnHealthChanged?.Invoke(currentHealth);

        if (flashOnDamage && spriteRenderer != null)
            StartCoroutine(DamageFlash());

        if (animator != null)
            animator.SetTrigger("Hit");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        deathPosition = transform.position;

        // ✅ Notificar al WaveManager que este enemigo murió
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.RegisterEnemyDeath(gameObject);
        }

        // ✅ Deshabilitar colisiones
        if (enemyCollider != null)
            enemyCollider.enabled = false;

        if (enemyRigidbody != null)
        {
            enemyRigidbody.linearVelocity = Vector2.zero;
            enemyRigidbody.isKinematic = true;

            // ✅ Congelar movimiento y rotación al morir
            enemyRigidbody.constraints = RigidbodyConstraints2D.FreezePositionX |
                                         RigidbodyConstraints2D.FreezePositionY |
                                         RigidbodyConstraints2D.FreezeRotation;
        }

        // ✅ Efecto de muerte con delay
        if (deathEffect != null)
        {
            StartCoroutine(SpawnDeathEffectWithDelay());
        }

        PlayerCurrency.Instance?.AddCoins(coinReward);
        OnDeath?.Invoke();
        transform.position = deathPosition;
    }



    private IEnumerator SpawnDeathEffectWithDelay()
    {
        if (deathEffectDelay > 0f)
        {
            yield return new WaitForSeconds(deathEffectDelay);
        }

        Instantiate(deathEffect, transform.position, Quaternion.identity);
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    public void SetDeathEffectDelay(float delay)
    {
        deathEffectDelay = Mathf.Max(0f, delay);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}