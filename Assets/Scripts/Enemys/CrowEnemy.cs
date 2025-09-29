using UnityEngine;
using System.Collections;

public class CrowEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private int damage = 2;
    [SerializeField] private float attackRate = 1.2f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackDelay = 0.4f;

    [Header("Slime Specific")]
    [SerializeField] private GameObject slimeTrailEffect;
    [SerializeField] private float trailSpawnRate = 0.5f;

    [Header("Knockback (opcional)")]
    [SerializeField] private bool enableKnockback = true;
    [Tooltip("Fuerza horizontal de empuje.")]
    [SerializeField] private float knockbackForce = 6f;
    [Tooltip("Impulso vertical adicional (0 = sin lift).")]
    [SerializeField] private float knockbackUpward = 0.5f;
    [Tooltip("Tiempo de control reducido por el empuje.")]
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Death Animation")]
    [SerializeField] private string deathStateName = "Death";       // Nombre exacto del state
    [SerializeField] private bool useUnscaledTimeOnDeath = false;   // Si pausas el juego, ponlo en true
    [SerializeField] private float enterStateTimeout = 1.0f;        // Tiempo máximo para entrar al state
    [SerializeField] private float killTimeout = 5.0f;              // Máximo absoluto antes de forzar Destroy
    [Tooltip("Tiempo a esperar DESPUÉS de terminar la animación antes de destruir al enemigo")]
    [SerializeField] private float destroyDelayAfterDeathAnim = 0.5f;

    private Transform player;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private Collider2D enemyCollider;
    private Animator animator;
    private float lastAttackTime;
    private float lastTrailTime;
    private bool isAttacking = false;
    private float attackStartTime;
    private Vector3 originalScale;
    private bool playerInRange = false;
    private bool isDead = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;

        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += OnEnemyDeath;
            enemyHealth.OnDamageTaken += OnEnemyDamage;
        }
    }

    void Update()
    {
        if (isDead) return;

        UpdateRotation();
        CheckPlayerInRange();
        UpdateMovement();
        UpdateSlimeTrail();
        UpdateAttack();
    }

    private void UpdateRotation()
    {
        if (player == null) return;

        bool playerIsToRight = player.position.x > transform.position.x;
        transform.localScale = playerIsToRight ?
            new Vector3(originalScale.x, originalScale.y, originalScale.z) :
            new Vector3(-originalScale.x, originalScale.y, originalScale.z);
    }

    private void CheckPlayerInRange()
    {
        if (player == null) return;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        playerInRange = distanceToPlayer <= attackRange;
    }

    private void UpdateMovement()
    {
        if (player == null || isDead) return;

        if (!isAttacking)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity *= 0.3f;
        }

        if (playerInRange && !isAttacking && Time.time - lastAttackTime >= attackRate)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackStartTime = Time.time;
        lastAttackTime = Time.time;

        if (animator != null) animator.SetTrigger("Attack");
    }

    private void UpdateAttack()
    {
        if (!isAttacking) return;

        float timeSinceAttackStart = Time.time - attackStartTime;
        if (timeSinceAttackStart >= attackDelay)
        {
            TryApplyDamage();
            isAttacking = false;
        }
    }

    private void TryApplyDamage()
    {
        if (player == null) return;

        float currentDistance = Vector2.Distance(transform.position, player.position);
        if (currentDistance <= attackRange * 1.2f)
        {
            // Daño
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(damage);

            // Efecto/partículas
            if (slimeTrailEffect != null)
                Instantiate(slimeTrailEffect, transform.position, Quaternion.identity);

            // Knockback opcional
            if (enableKnockback)
            {
                var knockReceiver = player.GetComponent<PlayerKnockbackReceiver>();
                if (knockReceiver != null)
                {
                    Vector2 dir = (player.position - transform.position).normalized;
                    dir.y += knockbackUpward; // lift opcional
                    knockReceiver.ApplyKnockback(dir, knockbackForce, knockbackDuration);
                }
            }
        }
    }

    private void UpdateSlimeTrail()
    {
        if (isDead) return;

        if (rb.linearVelocity.magnitude > 0.1f && slimeTrailEffect != null &&
            Time.time - lastTrailTime >= trailSpawnRate)
        {
            Instantiate(slimeTrailEffect, transform.position, Quaternion.identity);
            lastTrailTime = Time.time;
        }
    }

    private void OnEnemyDeath()
    {
        isDead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        if (enemyCollider != null)
            enemyCollider.enabled = false;

        isAttacking = false;

        if (animator != null)
        {
            // Asegurar que la animación corra
            animator.speed = 1f;
            animator.applyRootMotion = false;
            if (useUnscaledTimeOnDeath) animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            animator.ResetTrigger("Attack");
            animator.SetTrigger("Death");
            StartCoroutine(PlayDeathAndDestroy());
        }
        else
        {
            // Sin animator: destruye directo
            Destroy(gameObject);
        }

        // Partículas extra al morir (opcional)
        if (slimeTrailEffect != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 spawnPos = transform.position + new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-0.1f, 0.1f),
                    0
                );
                Instantiate(slimeTrailEffect, spawnPos, Quaternion.identity);
            }
        }
    }

    private IEnumerator PlayDeathAndDestroy()
    {
        yield return null; // dejar que procese el trigger

        int layer = 0;
        float safetyStart = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;

        // 1) Esperar a entrar al estado Death (con timeout)
        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(layer);
            if (string.IsNullOrEmpty(deathStateName) || st.IsName(deathStateName)) break;

            float now = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;
            if (now - safetyStart > enterStateTimeout) break;
            yield return null;
        }

        // 2) Obtener duración del clip actual
        float clipLen = 0.2f; // fallback
        var clips = animator.GetCurrentAnimatorClipInfo(layer);
        if (clips != null && clips.Length > 0 && clips[0].clip != null)
            clipLen = clips[0].clip.length;

        // Ajustar según velocidad del state
        var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        float stateSpeed = Mathf.Abs(stateInfo.speed) < 0.0001f ? 1f : stateInfo.speed;
        float waitTime = clipLen / stateSpeed;

        // 3) Esperar el tiempo del clip
        float waited = 0f;
        while (waited < waitTime)
        {
            float dt = useUnscaledTimeOnDeath ? Time.unscaledDeltaTime : Time.deltaTime;
            waited += dt;

            float now = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;
            if (now - safetyStart > killTimeout) break;
            yield return null;
        }

        // 4) Esperar delay configurable extra tras la animación
        if (destroyDelayAfterDeathAnim > 0f)
        {
            if (useUnscaledTimeOnDeath)
                yield return new WaitForSecondsRealtime(destroyDelayAfterDeathAnim);
            else
                yield return new WaitForSeconds(destroyDelayAfterDeathAnim);
        }

        Destroy(gameObject);
    }

    private void OnEnemyDamage()
    {
        // Comportamiento específico cuando recibe daño (si lo necesitas)
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= OnEnemyDeath;
            enemyHealth.OnDamageTaken -= OnEnemyDamage;
        }
    }
}
