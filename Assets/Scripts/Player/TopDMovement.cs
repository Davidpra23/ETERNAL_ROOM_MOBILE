using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 1f;           // Distancia del ataque
    [SerializeField] private int attackDamage = 2;             // Daño del ataque
    [SerializeField] private float attackCooldown = 0.5f;      // Tiempo entre ataques

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastMoveDir;
    private bool wasMoving;
    private bool isAttacking;
    private float lastAttackTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isAttacking) return; // No moverse mientras ataca

        // Entrada de movimiento
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Solo 4 direcciones
        if (moveInput.x != 0) moveInput.y = 0;

        // Normalizar dirección (solo -1, 0, 1)
        moveInput.x = Mathf.Clamp(moveInput.x, -1f, 1f);
        moveInput.y = Mathf.Clamp(moveInput.y, -1f, 1f);

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Actualizar Animator solo si cambia
        if (isMoving != wasMoving)
        {
            animator.SetBool("IsMoving", isMoving);
            wasMoving = isMoving;
        }

        // Actualizar dirección solo si cambia
        if (isMoving && moveInput != lastMoveDir)
        {
            animator.SetFloat("MoveX", moveInput.x);
            animator.SetFloat("MoveY", moveInput.y);
            lastMoveDir = moveInput;
        }

        // Ataque con clic izquierdo del ratón
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = isAttacking ? Vector2.zero : moveInput.normalized * moveSpeed;
    }

    // ==========================
    //      SISTEMA DE ATAQUE
    // ==========================

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");

        // Esperar un momento antes de aplicar el daño (sincronización con animación)
        yield return new WaitForSeconds(0.15f);

        // Detectar objetos cercanos
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            (Vector2)transform.position + lastMoveDir.normalized * attackRange * 0.5f,
            attackRange
        );

        foreach (Collider2D hit in hits)
        {
            // Verificar por tag "Enemy"
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(attackDamage);
                }
            }
        }

        // Esperar un poco antes de poder volver a atacar
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    // Visualizar rango de ataque en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 center = Application.isPlaying
            ? (Vector2)transform.position + lastMoveDir.normalized * attackRange * 0.5f
            : (Vector2)transform.position;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}
