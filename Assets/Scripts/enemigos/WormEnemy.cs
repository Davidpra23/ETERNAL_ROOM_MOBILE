// WormEnemy.cs
using UnityEngine;

public class WormEnemy : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform shootPoint;
    public SpriteRenderer spriteRenderer; // usar flipX para batching

    [Header("Settings")]
    public float moveSpeed = 2f;
    public float meleeRange = 2f;
    public float attackRange = 6f;
    public float attackCooldown = 1.5f;
    public float thinkInterval = 0.2f;

    [Header("Burst Mitigation")]
    [Tooltip("Desincroniza el primer disparo/mirada para evitar bursts en el mismo frame.")]
    public Vector2 initialDesyncRange = new Vector2(0f, 0.35f);
    [Tooltip("Variación aleatoria añadida al cooldown de ataque para desincronizar enemigos.")]
    public float attackCooldownJitter = 0.25f;

    [Header("Behaviour Toggle")]
    [Tooltip("Script que se desactiva cuando el player está en rango melee y se reactiva al salir.")]
    public MonoBehaviour scriptToToggle;

    private float lastAttackTime;
    private float nextThinkTime;
    private Rigidbody2D rb;
    private Transform player;

    private float meleeRangeSqr;
    private float attackRangeSqr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        meleeRangeSqr = meleeRange * meleeRange;
        attackRangeSqr = attackRange * attackRange;

        // Desincroniza este enemigo respecto a los demás
        float desync = Random.Range(initialDesyncRange.x, initialDesyncRange.y);
        lastAttackTime = Time.time + desync;          // desplaza el primer disparo
        nextThinkTime = Time.time + desync;           // desplaza el primer ciclo de IA
    }

    void FixedUpdate()
    {
        if (player == null || Time.time < nextThinkTime) return;
        nextThinkTime = Time.time + thinkInterval;

        Vector2 toPlayer = (Vector2)(player.position - transform.position);
        float sqrDistance = toPlayer.sqrMagnitude;

        FaceDirection(toPlayer); // Siempre mirar al jugador

        // \ud83d\udd34 Activar/desactivar el script asignable seg\u00fan rango melee
        UpdateScriptToggle(inMelee: sqrDistance < meleeRangeSqr);

        if (sqrDistance < meleeRangeSqr)
        {
            MoveAwayFromPlayer(toPlayer);
            animator.SetBool("IsMoving", true);
        }
        else if (sqrDistance <= attackRangeSqr)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);

            // cooldown con jitter por-enemigo para evitar disparos simult\u00e1neos
            float cooldownThisShot = attackCooldown + Random.Range(-attackCooldownJitter, attackCooldownJitter);
            if (Time.time >= lastAttackTime + Mathf.Max(0.01f, cooldownThisShot))
            {
                animator.SetTrigger("ATTACK");
                Shoot(toPlayer);
                lastAttackTime = Time.time;
            }
        }
        else
        {
            MoveTowardsPlayer(toPlayer);
            animator.SetBool("IsMoving", true);
        }
    }

    void MoveAwayFromPlayer(Vector2 toPlayer)
    {
        Vector2 direction = -toPlayer.normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    void MoveTowardsPlayer(Vector2 toPlayer)
    {
        Vector2 direction = toPlayer.normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    void Shoot(Vector2 toPlayer)
    {
        Vector2 direction = toPlayer.normalized;
        var pool = ProjectilePool.Instance; // requiere que exista el manager en escena
        if (pool == null) return;

        GameObject proj = pool.GetProjectile();
        proj.transform.position = shootPoint.position;
        proj.transform.rotation = Quaternion.identity;
        var p = proj.GetComponent<Projectile>();
        if (p != null) p.Initialize(direction);
    }

    void FaceDirection(Vector2 toPlayer)
    {
        if (spriteRenderer == null) return;
        if (toPlayer.x != 0)
        {
            // Mantener materiales compartidos y batching usando flipX en lugar de escalar negativo
            spriteRenderer.flipX = (toPlayer.x < 0f);
        }
    }

    void UpdateScriptToggle(bool inMelee)
    {
        if (scriptToToggle == null) return;
        // En melee: desactivar. Fuera de melee: activar.
        bool shouldEnable = !inMelee;
        if (scriptToToggle.enabled != shouldEnable)
        {
            scriptToToggle.enabled = shouldEnable;
        }
    }
}