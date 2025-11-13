using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BossController : MonoBehaviour
{
    [Header("Estadísticas")]
    public float moveSpeed = 3f;
    public BossHealth bossHealth;
    
    [Header("Fases")]
    public float phase2Threshold = 0.7f; // 70% vida
    
    [Header("Referencias")]
    public Transform player;
    public Animator animator;
    public BossHealthBar healthBar;
    public GameObject[] attackPrefabs;
    
    [Header("Ataques")]
    public float attackCooldown = 2f;
    public float meleeRange = 2f;
    public float castRange = 5f;
    
    private bool isAlive = true;
    // indica que el jefe está ejecutando un ataque o casteando (usa para congelar seguimiento/movimiento)
    private bool isBusyAttacking = false;
    // indica que el jefe está realizando un Dash (movimiento intencional durante el ataque)
    private bool isPerformingDash = false;
    private int currentPhase = 1;
    private float lastAttackTime;
    private Rigidbody2D rb;
    private Vector2 movement;

    // --- Anti-flicker para animaciones Move/Idle ---
    private float moveAnimChangeDelay = 0.2f; // segundos de retardo
    private float moveAnimTimer = 0f;
    private bool lastMoveState = false;
    
    // Patrones de ataque por fase
    private List<System.Action> phase1Attacks;
    private List<System.Action> phase2Attacks;

    [Header("Testing")]
    [Tooltip("Si está activo, el jefe solo ejecuta DashAttack respetando el cooldown.")]
    public bool dashTestMode = false;

    [Header("Dash")]
    [Tooltip("Daño del dash al colisionar con el jugador (similar al melee)")]
    public int dashDamage = 1;
    [Tooltip("Radio de golpe del dash para comprobar impacto con el jugador")]
    public float dashHitRadius = 1.2f;
    [Tooltip("Offset hacia adelante del centro del golpe del dash (en unidades). Permite ampliar alcance sin aumentar radio.")]
    public float dashHitForwardOffset = 1.5f;

    private Vector2 lastDashDirection = Vector2.right;

    [Header("Dash VFX")]
    [Tooltip("Prefab del efecto visual al iniciar el dash (se instancia en la posición inicial del dash)")]
    public GameObject dashVfxPrefab;

    [Header("Cast Lock")]
    [Tooltip("Tiempo extra que el jefe permanece inmóvil tras castear antes de volver a moverse.")]
    public float extraCastLockTime = 1f;

    [Header("Wave Attack (Playa)")]
    [Tooltip("Prefab de la ola que avanza desde el borde")]
    public GameObject wavePrefab;
    [Tooltip("Esquina mínima del rectángulo de la playa (X min, Y min)")]
    public Transform waveAreaMin;
    [Tooltip("Esquina máxima del rectángulo de la playa (X max, Y max)")]
    public Transform waveAreaMax;
    [Tooltip("Velocidad de avance de la ola")]
    public float waveSpeed = 6f;
    [Tooltip("Daño que aplica la ola al jugador al colisionar")]
    public int waveDamage = 1;
    [Tooltip("Grosor visual de la ola (escala perpendicular al avance)")]
    public float waveThickness = 1f;
    [Tooltip("Tiempo de vida de la ola antes de destruirse")]
    public float waveLifetime = 6f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth>();

        // Suscribirse a eventos de salud
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += OnHealthChanged;
            bossHealth.OnDeath += OnBossDeath;
            bossHealth.OnDamageTaken += OnDamageTaken;
        }

        InitializeAttackPatterns();
        StartCoroutine(BossSpawnSequence());
    }

    void InitializeAttackPatterns()
    {
        // Fase 1: Ataques básicos
        phase1Attacks = new List<System.Action>
        {
            MeleeAttack,
            ProjectileAttack
        };
        
        // Fase 2: Ataques más agresivos
        phase2Attacks = new List<System.Action>
        {
            MeleeAttack,
            ProjectileAttack,
            WaveAttack,
            DashAttack
        };
    }

    void Update()
    {
        if (!isAlive || player == null) return;
        
        CheckPhaseTransition();
        HandleMovement();
        HandleAttacks();
    }

    void CheckPhaseTransition()
    {
        float healthPercentage = bossHealth != null ? bossHealth.HealthPercentage : 1f;
        
        if (healthPercentage <= phase2Threshold && currentPhase != 2)
        {
            currentPhase = 2;
            StartCoroutine(PhaseTransition());
        }
    }

    void HandleMovement()
    {
        // Si está atacando/casteando y no es un dash, no seguir al jugador
        if (isBusyAttacking && !isPerformingDash)
        {
            movement = Vector2.zero;
            SetMoveAnimSmooth(false);
            return;
        }
        Vector2 direction = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Movimiento estratégico según la fase
        bool shouldMove = distanceToPlayer > meleeRange * 1.5f;
        movement = shouldMove ? direction : Vector2.zero;
        SetMoveAnimSmooth(shouldMove);

        // Rotar sprite hacia el jugador
        if (direction.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    // Cambia la animación de movimiento solo si la condición se mantiene estable por el retardo
    void SetMoveAnimSmooth(bool move)
    {
        if (move != lastMoveState)
        {
            moveAnimTimer += Time.deltaTime;
            if (moveAnimTimer >= moveAnimChangeDelay)
            {
                animator.SetBool("Move", move);
                lastMoveState = move;
                moveAnimTimer = 0f;
            }
        }
        else
        {
            moveAnimTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        if (isAlive)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleAttacks()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        // Modo test: fuerza solo DashAttack
        if (dashTestMode)
        {
            DashAttack();
            lastAttackTime = Time.time;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Si el jugador está fuera del rango de casteo:
        // - A partir de fase 2, forzar WaveAttack a cualquier distancia
        // - En fase 1, no forzamos ataque (comportamiento: no castea fuera de rango)
        if (distanceToPlayer > castRange)
        {
            if (currentPhase >= 2)
            {
                WaveAttack();
                lastAttackTime = Time.time;
            }
            return;
        }

        if (distanceToPlayer <= meleeRange)
        {
            ExecuteRandomAttack();
        }
        else if (distanceToPlayer <= castRange)
        {
            ExecuteRandomAttack();
        }
    }

    void ExecuteRandomAttack()
    {
        List<System.Action> currentAttacks = currentPhase switch
        {
            1 => phase1Attacks,
            2 => phase2Attacks,
            _ => phase1Attacks // Fase 3 eliminada, por defecto usar fase 1
        };
        
        if (currentAttacks.Count > 0)
        {
            int randomIndex = Random.Range(0, currentAttacks.Count);
            currentAttacks[randomIndex]?.Invoke(); // Corrected syntax error
            lastAttackTime = Time.time;
        }
    }

    // === SISTEMA DE ATAQUES ===
    
    void MeleeAttack()
    {
        // Marcar que está atacando para dejar de seguir al jugador
        isBusyAttacking = true;
        animator.SetTrigger("Attack");
        StartCoroutine(DealMeleeDamage());
    }

    IEnumerator DealMeleeDamage()
    {
        yield return new WaitForSeconds(0.3f); // Timing de la animación
        // Calcular centro del golpe (misma fórmula que al visualizar)
        Vector2 attackCenter = (Vector2)(transform.position + transform.right * meleeRange * transform.localScale.x);

        // Comprobar si el jugador sigue dentro del rango en este momento
        float distToPlayer = Vector2.Distance(attackCenter, player.position);
        if (distToPlayer <= meleeRange)
        {
            // Aplicar daño solo si sigue dentro del área
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, meleeRange);
            foreach (var hit in hits)
            {
                if (hit != null && hit.CompareTag("Player"))
                {
                    hit.GetComponent<PlayerHealth>()?.TakeDamage(1);
                }
            }
        }

        // Termina el estado de ataque
        isBusyAttacking = false;
    }

    void ProjectileAttack()
    {
        // Marcar que está casteando/atacando para que deje de seguir al jugador
        isBusyAttacking = true;
        animator.SetTrigger("Cast");
        StartCoroutine(SpawnProjectile());
    }

    IEnumerator SpawnProjectile()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (attackPrefabs.Length > 0 && attackPrefabs[0] != null)
        {
            GameObject prefab = attackPrefabs[0];

            // If the prefab contains FallingImpact, instantiate it at the player's position
            if (prefab.GetComponent<FallingImpact>() != null)
            {
                // Instanciar ligeramente por encima del jugador para que la animación quede elevada
                Vector3 spawnPos = (Vector3)player.position + Vector3.up * 1f;
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                GameObject projectile = Instantiate(
                    prefab,
                    transform.position,
                    Quaternion.identity
                );

                var rb = projectile.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = (player.position - transform.position).normalized * 8f;
            }
        }

        // Finalizar estado de casteo/ataque
        if (extraCastLockTime > 0f)
            yield return new WaitForSeconds(extraCastLockTime);
        isBusyAttacking = false;
    }

    void WaveAttack()
    {
        // Bloquear movimiento durante el cast
        isBusyAttacking = true;
        animator.SetTrigger("Cast");
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        yield return new WaitForSeconds(0.5f);

        // Validar área y prefab
        if (wavePrefab == null || waveAreaMin == null || waveAreaMax == null)
        {
            Debug.LogWarning("[Boss] WaveAttack requiere wavePrefab, waveAreaMin y waveAreaMax asignados.");
            if (extraCastLockTime > 0f) yield return new WaitForSeconds(extraCastLockTime);
            isBusyAttacking = false;
            yield break;
        }

        Vector2 min = waveAreaMin.position;
        Vector2 max = waveAreaMax.position;
        Vector2 playerPos = player != null ? (Vector2)player.position : (Vector2)transform.position;

    // Bordes: top, bottom, left, right
        Vector2 topCenter = new Vector2((min.x + max.x) * 0.5f, max.y);
        Vector2 bottomCenter = new Vector2((min.x + max.x) * 0.5f, min.y);
        Vector2 leftCenter = new Vector2(min.x, (min.y + max.y) * 0.5f);
        Vector2 rightCenter = new Vector2(max.x, (min.y + max.y) * 0.5f);

        // Distancias del jugador a cada borde
        float dTop = Mathf.Abs(playerPos.y - max.y);
        float dBottom = Mathf.Abs(playerPos.y - min.y);
        float dLeft = Mathf.Abs(playerPos.x - min.x);
        float dRight = Mathf.Abs(playerPos.x - max.x);

        // Elegir borde más cercano y dirección
        float best = dTop;
        Vector2 spawnPos = topCenter;
        Vector2 moveDir = Vector2.down; // desde arriba hacia abajo
        bool horizontal = true;

        if (dBottom < best) { best = dBottom; spawnPos = bottomCenter; moveDir = Vector2.up; horizontal = true; }
        if (dLeft   < best) { best = dLeft;   spawnPos = leftCenter;   moveDir = Vector2.right; horizontal = false; }
        if (dRight  < best) { best = dRight;  spawnPos = rightCenter;  moveDir = Vector2.left; horizontal = false; }

        // Ajustar el punto de spawn para alinear con la coordenada del jugador en el eje perpendicular
        if (horizontal)
        {
            // Ola desde arriba/abajo: ajustar X a la posición del jugador (acotado al mapa)
            float px = Mathf.Clamp(playerPos.x, min.x, max.x);
            spawnPos.x = px;
        }
        else
        {
            // Ola desde izquierda/derecha: ajustar Y a la posición del jugador (acotado al mapa)
            float py = Mathf.Clamp(playerPos.y, min.y, max.y);
            spawnPos.y = py;
        }

        // Instanciar ola (mantener tamaño del prefab)
        GameObject wave = Instantiate(wavePrefab, spawnPos, Quaternion.identity);

        // Añadir componente mover/dañar si no existe
    var mover = wave.GetComponent<WaveMover>();
        if (mover == null) mover = wave.AddComponent<WaveMover>();
        mover.Init(moveDir, waveSpeed, waveDamage, waveLifetime);

        if (extraCastLockTime > 0f) yield return new WaitForSeconds(extraCastLockTime);
        isBusyAttacking = false;
    }

    void DashAttack()
    {
        // Marcar estado de ataque y dash para que el movimiento normal se suspenda pero permita avance forzado
        isBusyAttacking = true;
        isPerformingDash = true;
        lastDashDirection = (player.position - transform.position).normalized;
        // Instanciar VFX en la posición inicial del dash
        if (dashVfxPrefab != null)
        {
            var vfx = Instantiate(dashVfxPrefab, transform.position, Quaternion.identity);
            // Orientar el VFX segun la escala (derecha/izquierda)
            float dirSign = Mathf.Sign(transform.localScale.x);
            Vector3 vfxScale = vfx.transform.localScale;
            vfxScale.x = Mathf.Abs(vfxScale.x) * dirSign;
            vfx.transform.localScale = vfxScale;
            Destroy(vfx, .5f);
        }
        animator.SetTrigger("Attack");
        StartCoroutine(PerformDash());
    }

    IEnumerator PerformDash()
    {
        Vector2 dashDirection = (player.position - transform.position).normalized;
        float originalSpeed = moveSpeed;
        
        moveSpeed = 15f;
        movement = dashDirection;
        // Aplicar daño a mitad del dash para dar oportunidad de esquivar al inicio
        yield return new WaitForSeconds(0.15f);
        DealDashDamage();
        // Esperar el resto de la duración
        yield return new WaitForSeconds(0.15f);

        moveSpeed = originalSpeed;
        movement = Vector2.zero;
        isPerformingDash = false;
        isBusyAttacking = false;
    }

    void DealDashDamage()
    {
        // Centro del impacto adelantado en la dirección del dash
        Vector3 center = transform.position + (Vector3)(lastDashDirection.normalized * dashHitForwardOffset);
        // Comprobar si el jugador está dentro del radio de impacto del dash
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, dashHitRadius);
        foreach (var hit in hits)
        {
            if (hit != null && hit.CompareTag("Player"))
            {
                hit.GetComponent<PlayerHealth>()?.TakeDamage(dashDamage);
            }
        }
    }

    // === SISTEMA DE DAÑO Y MUERTE ===
    
    public void TakeDamage(float damage)
    {
        if (!isAlive) return;

        if (bossHealth != null)
        {
            bossHealth.TakeDamage(damage);
        }
        else
        {
            // Fallback: si no hay BossHealth, avisar para evitar perder daño
            Debug.LogWarning("BossHealth component missing on boss. Damage ignored.");
        }
    }

    void Die()
    {
        isAlive = false;
        animator.SetTrigger("Death");
        healthBar.HideHealthBar();
        
        // Deshabilitar colisiones y movimiento
        GetComponent<Collider2D>().enabled = false;
        rb.linearVelocity = Vector2.zero;
        
        StartCoroutine(DeathSequence());
    }

    private void OnBossDeath()
    {
        // Called from BossHealth when boss dies
        if (!isAlive)
            return;

        Die();
    }

    private void OnHealthChanged(float percentage)
    {
        if (healthBar != null)
            healthBar.UpdateHealth(percentage);
    }

    private void OnDamageTaken()
    {
        // Reproducir un pulso visual en la barra de vida cuando el jefe recibe daño
        if (healthBar != null)
        {
            healthBar.DamagePulse();
        }
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);
        
        // Efectos de muerte, sonido, etc.
        Destroy(gameObject);
    }

    IEnumerator PhaseTransition()
    {
        // Congelar al jefe durante la transición
        float originalSpeed = moveSpeed;
        moveSpeed = 0;
        
        animator.SetTrigger("Cast");
        
        // Efectos visuales/sonoros de transición de fase
        yield return new WaitForSeconds(1.5f);
        
        moveSpeed = originalSpeed;
        
        // Ajustar cooldown según la fase (más agresivo)
        attackCooldown *= 0.7f;
    }

    IEnumerator BossSpawnSequence()
    {
        // Congelar al jefe durante el spawn
        moveSpeed = 0;
        
        // Reproducir animación de spawn
        animator.SetTrigger("Spawn");
        
        // Mostrar barra de vida con efecto
        healthBar.ShowHealthBar();
        // Actualizar valor inicial de la barra
        if (bossHealth != null)
            healthBar.UpdateHealth(bossHealth.HealthPercentage);
        
        yield return new WaitForSeconds(2f);
        
        // Reactivar movimiento
        moveSpeed = 3f;
    }

    void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= OnHealthChanged;
            bossHealth.OnDeath -= OnBossDeath;
            bossHealth.OnDamageTaken -= OnDamageTaken;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar rangos en el editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, castRange);
        // Visualizar hit del dash
        Gizmos.color = Color.cyan;
        Vector3 dashCenter = transform.position + (Vector3)((Application.isPlaying ? lastDashDirection : Vector2.right).normalized * dashHitForwardOffset);
        Gizmos.DrawWireSphere(dashCenter, dashHitRadius);
    }
}