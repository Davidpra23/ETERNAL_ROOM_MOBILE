using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IHealth
{
    [Header("Damage Text Settings")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Transform damageTextSpawnPoint;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int coinReward = 1;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private float deathEffectDelay = 0f;
    [SerializeField] private bool flashOnDamage = true;

    [Header("Coin Drop Settings")]
    [SerializeField] private GameObject coinPrefab; // Prefab de la moneda a soltar
    [SerializeField] private int minCoinsToDrop = 1;
    [SerializeField] private int maxCoinsToDrop = 3;
    [SerializeField] private float coinDropForce = 2f;
    [SerializeField] private float coinSpreadRange = 1f;

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
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

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

        // ✅ Mostrar texto de daño
        ShowDamageText(damageAmount);

        if (currentHealth <= 0)
            Die();
    }

    // IHealth implementation (wrapper) to accept float damage from generic weapons
    public void TakeDamage(float amount)
    {
        TakeDamage(Mathf.RoundToInt(amount));
    }

    private void ShowDamageText(int damageAmount)
    {
        if (damageTextPrefab == null) return;

        Vector3 spawnPos = damageTextSpawnPoint != null ?
            damageTextSpawnPoint.position : transform.position + Vector3.up * 1f;

        GameObject go = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        DamageText dmg = go.GetComponent<DamageText>();

        // Puedes cambiar el color según tipo de daño o crítico
        Color color = Color.white;
        bool isCritical = false;

        if (damageAmount > 20) // ejemplo de crítico
        {
            color = Color.yellow;
            isCritical = true;
        }

        dmg.Setup(damageAmount, color, isCritical);
    }


    private void Die()
    {
        if (isDead) return;

        isDead = true;
        deathPosition = transform.position;

        if (animator != null)
            animator.SetTrigger("Death");

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
            enemyRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // ✅ Soltar monedas al morir
        DropCoins();

        // ✅ Efecto de muerte con delay
        if (deathEffect != null)
        {
            StartCoroutine(SpawnDeathEffectWithDelay());
        }
        Invoke("DestroySelf", 1.0f); // 1.0f = duración del clip DEATH

        PlayerCurrency.Instance?.AddCoins(coinReward);
        OnDeath?.Invoke();
        transform.position = deathPosition;
    }
    private void DestroySelf()
    {
        Destroy(gameObject);
    }
    private void DropCoins()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("Coin prefab no asignado en " + gameObject.name);
            return;
        }

        int coinsToDrop = Random.Range(minCoinsToDrop, maxCoinsToDrop + 1);

        for (int i = 0; i < coinsToDrop; i++)
        {
            // Calcular posición aleatoria alrededor del enemigo
            Vector3 dropPosition = deathPosition + new Vector3(
                Random.Range(-coinSpreadRange, coinSpreadRange),
                Random.Range(-coinSpreadRange, coinSpreadRange),
                0f
            );

            // Instanciar la moneda
            GameObject coin = Instantiate(coinPrefab, dropPosition, Quaternion.identity);

            // Aplicar fuerza física para efecto de salpicadura
            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                Vector2 forceDirection = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f)
                ).normalized;

                coinRb.AddForce(forceDirection * coinDropForce, ForceMode2D.Impulse);

                // Rotación aleatoria
                coinRb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            }

            Debug.Log($"Moneda {i + 1} soltada en posición: {dropPosition}");
        }

        Debug.Log($"{coinsToDrop} moneda(s) soltada(s) por {gameObject.name}");
    }

    private IEnumerator SpawnDeathEffectWithDelay()
    {
        if (deathEffectDelay > 0f)
        {
            yield return new WaitForSeconds(deathEffectDelay);
        }

        Instantiate(deathEffect, transform.position, Quaternion.identity);
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    // Métodos públicos para configurar las monedas en tiempo de ejecución
    public void SetCoinPrefab(GameObject newCoinPrefab)
    {
        coinPrefab = newCoinPrefab;
    }

    public void SetCoinDropRange(int minCoins, int maxCoins)
    {
        minCoinsToDrop = Mathf.Max(0, minCoins);
        maxCoinsToDrop = Mathf.Max(minCoinsToDrop, maxCoins);
    }

    public void SetCoinDropForce(float force)
    {
        coinDropForce = Mathf.Max(0f, force);
    }

    public void SetCoinSpreadRange(float spread)
    {
        coinSpreadRange = Mathf.Max(0f, spread);
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