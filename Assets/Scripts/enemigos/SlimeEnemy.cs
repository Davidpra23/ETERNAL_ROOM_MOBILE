using UnityEngine;
using System.Collections;

public class SlimeEnemy : MonoBehaviour
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
    [SerializeField] private float destroyDelay = 2f;

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
        player = GameObject.FindGameObjectWithTag("Player").transform;
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
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // ✅ Aplicar daño directamente sin verificar invencibilidad
                playerHealth.TakeDamage(damage);
                
                if (slimeTrailEffect != null)
                    Instantiate(slimeTrailEffect, transform.position, Quaternion.identity);
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
            animator.SetTrigger("Death");
            StartCoroutine(DisableAnimatorAfterDeath());
        }

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
        
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DisableAnimatorAfterDeath()
    {
        yield return null;
        float deathAnimationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(deathAnimationLength);
        animator.enabled = false;
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    private void OnEnemyDamage()
    {
        // Comportamiento específico cuando recibe daño
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