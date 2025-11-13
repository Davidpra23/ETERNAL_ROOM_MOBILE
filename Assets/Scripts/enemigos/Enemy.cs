using UnityEngine;

public enum EnemyType
{
    BasicZombie,
    FastRunner,
    Tank,
    RangedShooter,
    Swarmling,
    Exploder,
    Healer,
    Elite
}

public class Enemy : MonoBehaviour
{
    [Header("Stats Base")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int coinReward = 1;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDelay = 0.3f; // ✅ Delay antes del daño

    [Header("Component References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D collision;
    [SerializeField] private Animator animator;

    [Header("Special Abilities")]
    [SerializeField] private bool isRanged = false;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float rangedAttackRange = 8f;
    [SerializeField] private int projectileDamage = 2;
    [SerializeField] private bool explodesOnDeath = false;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private int explosionDamage = 5;
    [SerializeField] private bool canHealOthers = false;
    [SerializeField] private float healRadius = 3f;
    [SerializeField] private float healAmount = 5f;

    private int currentHealth;
    private Transform player;
    private Rigidbody2D rb;
    private float lastAttackTime;
    private bool isDead = false;
    private bool isMoving = false;
    private bool isAttacking = false;
    private float attackStartTime;
    private Vector3 originalScale;

    public System.Action OnDeath;
    public System.Action OnDamageTaken;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        
        InitializeEnemyBehavior();
    }

    void Update()
    {
        if (isDead) return;

        UpdateRotation(); // ✅ Rotación basada en posición del player
        UpdateMovement();
        UpdateSpecialBehavior();
        UpdateAnimations();
        UpdateAttack(); // ✅ Verificación de ataque con delay
    }

    private void InitializeEnemyBehavior()
    {
        // ✅ LOS VALORES SE MANTIENEN CON LOS DEL INSPECTOR
        // Solo se sobreescriben si no se han configurado manualmente
        if (maxHealth == 10) // Si está en valor default
        {
            switch (enemyType)
            {
                case EnemyType.BasicZombie:
                    moveSpeed = 2f;
                    damage = 1;
                    maxHealth = 10;
                    attackRange = 1.5f;
                    attackDelay = 0.3f;
                    break;
                case EnemyType.FastRunner:
                    moveSpeed = 4f;
                    damage = 1;
                    maxHealth = 5;
                    attackRange = 1.2f;
                    attackDelay = 0.2f;
                    break;
                case EnemyType.Tank:
                    moveSpeed = 1f;
                    damage = 2;
                    maxHealth = 30;
                    attackRange = 1.8f;
                    attackDelay = 0.4f;
                    break;
                case EnemyType.RangedShooter:
                    moveSpeed = 1.5f;
                    damage = 2;
                    maxHealth = 15;
                    isRanged = true;
                    rangedAttackRange = 8f;
                    projectileDamage = 2;
                    attackDelay = 0.5f;
                    break;
                case EnemyType.Swarmling:
                    moveSpeed = 3f;
                    damage = 1;
                    maxHealth = 3;
                    attackRange = 1f;
                    attackDelay = 0.1f;
                    break;
                case EnemyType.Exploder:
                    moveSpeed = 2f;
                    damage = 3;
                    maxHealth = 8;
                    explodesOnDeath = true;
                    explosionDamage = 5;
                    attackRange = 1.5f;
                    attackDelay = 0.3f;
                    break;
                case EnemyType.Healer:
                    moveSpeed = 1f;
                    damage = 1;
                    maxHealth = 20;
                    canHealOthers = true;
                    attackRange = 1.5f;
                    attackDelay = 0.3f;
                    break;
                case EnemyType.Elite:
                    moveSpeed = 2.5f;
                    damage = 3;
                    maxHealth = 25;
                    attackRange = 2f;
                    attackDelay = 0.2f;
                    break;
            }
        }
    }

    // ✅ NUEVO MÉTODO: Rotación del sprite según posición del player
    private void UpdateRotation()
    {
        if (player == null) return;

        // Determinar dirección del player
        bool playerIsToRight = player.position.x > transform.position.x;
        
        // Rotar el sprite (no el GameObject completo)
        if (playerIsToRight)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
        }
    }

    private void UpdateMovement()
    {
        if (player == null || isAttacking) return; // ✅ No moverse durante ataque

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool shouldMove = true;

        if (!isRanged && distanceToPlayer <= attackRange)
        {
            shouldMove = false;
            if (!isAttacking && Time.time - lastAttackTime >= attackRate)
            {
                StartAttack();
            }
        }

        if (shouldMove)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            
            switch (enemyType)
            {
                case EnemyType.FastRunner:
                    if (Time.time % 1f < 0.5f)
                    {
                        direction = Quaternion.Euler(0, 0, 45) * direction;
                    }
                    else
                    {
                        direction = Quaternion.Euler(0, 0, -45) * direction;
                    }
                    break;
                    
                case EnemyType.RangedShooter:
                    if (distanceToPlayer < 5f)
                    {
                        direction = -direction;
                    }
                    break;
            }

            rb.linearVelocity = direction * moveSpeed;
            isMoving = true;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
        }
    }

    // ✅ NUEVO MÉTODO: Iniciar ataque con delay
    private void StartAttack()
    {
        isAttacking = true;
        attackStartTime = Time.time;
        lastAttackTime = Time.time;
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    // ✅ NUEVO MÉTODO: Verificar ataque con delay y rango
    private void UpdateAttack()
    {
        if (!isAttacking) return;

        float timeSinceAttackStart = Time.time - attackStartTime;
        
        // Verificar si ya pasó el delay y aplicar daño
        if (timeSinceAttackStart >= attackDelay)
        {
            TryApplyDamage();
            isAttacking = false;
        }
    }

    // ✅ MÉTODO MODIFICADO: Verificar rango antes de aplicar daño
    private void TryApplyDamage()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // ✅ Solo aplicar daño si el player sigue en rango
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
        else
        {
            Debug.Log("Player salió del rango de ataque");
        }
    }

    private void UpdateSpecialBehavior()
    {
        switch (enemyType)
        {
            case EnemyType.RangedShooter:
                TryRangedAttack();
                break;
                
            case EnemyType.Healer:
                TryHealOthers();
                break;
                
            case EnemyType.Exploder:
                CheckExplosion();
                break;
        }
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("Move", isMoving);
            animator.SetFloat("Speed", rb.linearVelocity.magnitude);
            animator.SetBool("IsAttacking", isAttacking); // ✅ Nueva variable para animación
        }
    }

    private void TryRangedAttack()
    {
        if (!isRanged || projectilePrefab == null || isAttacking) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= rangedAttackRange && Time.time - lastAttackTime >= attackRate)
        {
            StartAttack(); // ✅ Usar el mismo sistema de delay para ranged
        }
    }

    // ✅ MÉTODO MODIFICADO: Aplicar delay también para ranged
    private void ExecuteRangedAttack()
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Vector2 direction = (player.position - transform.position).normalized;
        
        SetupProjectile(projectile, direction);
    }

    private void SetupProjectile(GameObject projectile, Vector2 direction)
    {
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        EnemyProjectile enemyProjectile = projectile.AddComponent<EnemyProjectile>();
        enemyProjectile.SetDamage(projectileDamage);
        enemyProjectile.SetSpeed(projectileSpeed);
    }

    private void TryHealOthers()
    {
        if (!canHealOthers) return;
        
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, healRadius);
        
        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null && enemy != this)
            {
                enemy.Heal(healAmount * Time.deltaTime);
            }
        }
    }

    private void CheckExplosion()
    {
        if (!explodesOnDeath) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= 2f)
        {
            Explode();
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        OnDamageTaken?.Invoke();

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.RoundToInt(healAmount));
    }

    private void Die()
    {
        isDead = true;
        isMoving = false;
        isAttacking = false;
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
            StartCoroutine(DestroyAfterAnimation());
        }
        else
        {
            if (explodesOnDeath)
            {
                Explode();
            }
            else
            {
                PlayerCurrency.Instance?.AddCoins(coinReward);
                OnDeath?.Invoke();
                Destroy(gameObject);
            }
        }
    }

    private System.Collections.IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(1f);
        
        if (explodesOnDeath)
        {
            Explode();
        }
        else
        {
            PlayerCurrency.Instance?.AddCoins(coinReward);
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    private void Explode()
    {
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(explosionDamage);
            }
            
            Enemy otherEnemy = hit.GetComponent<Enemy>();
            if (otherEnemy != null && otherEnemy != this)
            {
                otherEnemy.TakeDamage(explosionDamage / 2);
            }
        }
        
        PlayerCurrency.Instance?.AddCoins(coinReward);
        OnDeath?.Invoke();
        
        Destroy(gameObject);
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (explodesOnDeath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
        
        if (canHealOthers)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healRadius);
        }
        
        if (isRanged)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
        }
    }
}

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 2;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private GameObject hitEffect;

    private Vector2 direction;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, transform.position, Quaternion.identity);
                }
                
                Destroy(gameObject);
            }
        }
        else if (collision.CompareTag("Wall") || collision.CompareTag("Obstacle"))
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}