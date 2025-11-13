using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrowEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private int damage = 2;
    [SerializeField] private float attackRate = 1.2f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackDelay = 0.4f;
    [Tooltip("Distancia máxima a la que empieza a seguir al jugador.")]
    [SerializeField] private float chaseRange = 6f;

    [Header("Knockback (opcional)")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpward = 0.5f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Death Animation")]
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private bool useUnscaledTimeOnDeath = false;
    [SerializeField] private float enterStateTimeout = 1.0f;
    [SerializeField] private float killTimeout = 5.0f;
    [SerializeField] private float destroyDelayAfterDeathAnim = 0.5f;

    [Header("Patrol Path Settings")]
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private float waypointThreshold = 0.3f;
    [SerializeField] private float waitAtPointTime = 1.0f;

    private Transform player;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private Collider2D enemyCollider;
    private Animator animator;
    private Vector3 originalPosition;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool playerInAttackRange = false;
    private bool playerInChaseRange = false;

    private float attackStartTime;
    private float lastAttackTime;

    // Patrol control
    private int currentWaypoint = 0;
    private bool returningToOrigin = false;
    private float waitTimer = 0f;

    private Vector3 originalScale;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;
        originalPosition = transform.position;

        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += OnEnemyDeath;
            enemyHealth.OnDamageTaken += OnEnemyDamage;
        }
    }

    void Update()
    {
        if (isDead) return;

        CheckPlayerRanges();
        UpdateRotation();
        UpdateMovement();
        UpdateAttack();
    }

    private void CheckPlayerRanges()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        playerInChaseRange = distance <= chaseRange;
        playerInAttackRange = distance <= attackRange;
    }

    private void UpdateRotation()
    {
        if (isDead) return;

        if (playerInChaseRange)
        {
            if (player != null)
            {
                bool playerIsToRight = player.position.x > transform.position.x;
                float facing = playerIsToRight ? 1f : -1f;
                transform.localScale = new Vector3(originalScale.x * facing, originalScale.y, originalScale.z);
            }
        }
        else
        {
            // Rotar hacia el waypoint actual si no está persiguiendo
            Vector3 target = GetCurrentTarget();
            bool targetIsToRight = target.x > transform.position.x;
            float facing = targetIsToRight ? 1f : -1f;
            transform.localScale = new Vector3(originalScale.x * facing, originalScale.y, originalScale.z);
        }
    }

    private void UpdateMovement()
    {
        if (player == null || isDead) return;

        // 🦅 PERSECUCIÓN
        if (playerInChaseRange)
        {
            returningToOrigin = false;
            if (!isAttacking)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed;
            }
            else
            {
                rb.linearVelocity *= 0.3f;
            }

            if (playerInAttackRange && !isAttacking && Time.time - lastAttackTime >= attackRate)
                StartAttack();

            return;
        }

        // 🕊️ PATRULLA
        if (patrolPoints.Count > 0)
        {
            Vector3 target = GetCurrentTarget();
            float distance = Vector2.Distance(transform.position, target);

            if (distance <= waypointThreshold)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitAtPointTime)
                {
                    waitTimer = 0f;
                    AdvanceWaypoint();
                }
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                Vector2 dir = (target - transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed * 0.6f;
            }
        }
        else
        {
            // 🌀 Sin waypoints → regresar a posición original
            Vector3 target = returningToOrigin ? originalPosition : originalPosition;
            float distance = Vector2.Distance(transform.position, target);

            if (distance <= waypointThreshold)
                rb.linearVelocity = Vector2.zero;
            else
            {
                Vector2 dir = (target - transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed * 0.6f;
            }
        }
    }

    private Vector3 GetCurrentTarget()
    {
        if (returningToOrigin) return originalPosition;
        if (patrolPoints.Count == 0) return originalPosition;
        return patrolPoints[currentWaypoint].position;
    }

    private void AdvanceWaypoint()
    {
        if (patrolPoints.Count == 0)
        {
            returningToOrigin = true;
            return;
        }

        if (!returningToOrigin)
        {
            currentWaypoint++;
            if (currentWaypoint >= patrolPoints.Count)
            {
                returningToOrigin = true;
                currentWaypoint = 0; // Reseteamos para la próxima vuelta
            }
        }
        else
        {
            returningToOrigin = false; // Ya volvió, reinicia ciclo
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
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);

            if (enableKnockback)
            {
                var knockReceiver = player.GetComponent<PlayerKnockbackReceiver>();
                if (knockReceiver != null)
                {
                    Vector2 dir = (player.position - transform.position).normalized;
                    dir.y += knockbackUpward;
                    knockReceiver.ApplyKnockback(dir, knockbackForce, knockbackDuration);
                }
            }
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
            animator.speed = 1f;
            animator.applyRootMotion = false;
            if (useUnscaledTimeOnDeath)
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            animator.ResetTrigger("Attack");
            animator.SetTrigger("Death");
            StartCoroutine(PlayDeathAndDestroy());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator PlayDeathAndDestroy()
    {
        yield return null;

        int layer = 0;
        float safetyStart = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;

        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(layer);
            if (string.IsNullOrEmpty(deathStateName) || st.IsName(deathStateName)) break;

            float now = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;
            if (now - safetyStart > enterStateTimeout) break;
            yield return null;
        }

        float clipLen = 0.2f;
        var clips = animator.GetCurrentAnimatorClipInfo(layer);
        if (clips != null && clips.Length > 0 && clips[0].clip != null)
            clipLen = clips[0].clip.length;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        float stateSpeed = Mathf.Abs(stateInfo.speed) < 0.0001f ? 1f : stateInfo.speed;
        float waitTime = clipLen / stateSpeed;

        float waited = 0f;
        while (waited < waitTime)
        {
            float dt = useUnscaledTimeOnDeath ? Time.unscaledDeltaTime : Time.deltaTime;
            waited += dt;
            float now = useUnscaledTimeOnDeath ? Time.unscaledTime : Time.time;
            if (now - safetyStart > killTimeout) break;
            yield return null;
        }

        if (destroyDelayAfterDeathAnim > 0f)
        {
            if (useUnscaledTimeOnDeath)
                yield return new WaitForSecondsRealtime(destroyDelayAfterDeathAnim);
            else
                yield return new WaitForSeconds(destroyDelayAfterDeathAnim);
        }

        Destroy(gameObject);
    }

    private void OnEnemyDamage() { }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= OnEnemyDeath;
            enemyHealth.OnDamageTaken -= OnEnemyDamage;
        }
    }
}
