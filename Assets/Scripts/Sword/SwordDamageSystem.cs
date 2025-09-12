using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SwordDamageSystem : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float damageDelay = 0.1f; // Delay antes de aplicar daño

    [Header("Auto-Aim Settings")]
    [SerializeField] private bool enableAutoAim = true;
    [SerializeField] private float autoAimRadius = 3f;
    [SerializeField] private float attackAngle = 120f;
    [SerializeField] private bool showAttackGizmos = true;
    [SerializeField] private Color attackArcColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SwordEffectsSystem effectsSystem;
    [SerializeField] private PlayerMovement playerMovement;

    private float lastAttackTime;
    private bool isAttackEnabled = true;
    private Transform currentTarget;
    private Vector2 currentAttackDirection = Vector2.right;
    private Coroutine damageCoroutine;

    // Eventos para diferentes momentos del ataque
    public System.Action OnAttack;     // Se dispara al INICIAR el ataque
    public System.Action OnAttackHit;  // Se dispara cuando GOLPEA un enemigo

    void Start()
    {
        // Asignar referencias automáticamente
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (playerMovement == null && playerTransform != null)
            playerMovement = playerTransform.GetComponent<PlayerMovement>();

        if (effectsSystem == null)
            effectsSystem = GetComponentInChildren<SwordEffectsSystem>();
    }

    void Update()
    {
        if (enableAutoAim)
        {
            FindNearestEnemy();
            UpdateAttackDirection();
        }
    }

    /// <summary>
    /// Intenta realizar un ataque si está disponible
    /// </summary>
    public void TryAttack()
    {
        if (!isAttackEnabled || !IsAttackReady()) return;
        
        PerformAttack();
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        OnAttack?.Invoke(); // Disparar evento de inicio de ataque
        ExecuteAttackEffects();
        
        // Iniciar corrutina para aplicar daño con delay
        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);
        
        damageCoroutine = StartCoroutine(ApplyDamageWithDelay());
    }

    private IEnumerator ApplyDamageWithDelay()
    {
        // Esperar el delay configurable antes de detectar enemigos
        yield return new WaitForSeconds(damageDelay);
        DetectEnemiesInArc();
    }

    private void ExecuteAttackEffects()
    {
        effectsSystem?.ExecuteAttackEffects();
    }

    private void FindNearestEnemy()
    {
        if (playerTransform == null) return;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerTransform.position, autoAimRadius, enemyLayer);
        float closestDistance = Mathf.Infinity;
        currentTarget = null;

        foreach (Collider2D enemy in enemies)
        {
            if (enemy == null) continue;

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                float distance = Vector2.Distance(playerTransform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    currentTarget = enemy.transform;
                }
            }
        }
    }

    private void UpdateAttackDirection()
    {
        if (currentTarget != null && playerTransform != null)
        {
            currentAttackDirection = (currentTarget.position - playerTransform.position).normalized;
        }
        else
        {
            currentAttackDirection = GetFallbackAttackDirection();
        }
    }

    private Vector2 GetFallbackAttackDirection()
    {
        // Priorizar dirección del movimiento del jugador
        if (playerMovement != null)
        {
            Vector2 movementDir = playerMovement.GetMovementDirection();
            if (movementDir.magnitude > 0.1f)
                return movementDir.normalized;
        }

        // Fallback a dirección basada en escala del jugador
        if (playerTransform != null)
            return new Vector2(Mathf.Sign(playerTransform.localScale.x), 0f);

        return Vector2.right;
    }

    private void DetectEnemiesInArc()
    {
        if (playerTransform == null) return;

        Collider2D[] potentialEnemies = Physics2D.OverlapCircleAll(playerTransform.position, attackRange, enemyLayer);
        float halfAngle = attackAngle / 2f;

        foreach (Collider2D enemy in potentialEnemies)
        {
            if (enemy == null) continue;

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead && IsInAttackArc(enemy.transform, halfAngle))
            {
                enemyHealth.TakeDamage(attackDamage);
                effectsSystem?.SpawnHitEffect(enemy.transform.position);
                OnAttackHit?.Invoke(); // Disparar evento de golpe
            }
        }
    }

    private bool IsInAttackArc(Transform enemy, float halfAngle)
    {
        if (playerTransform == null) return false;

        Vector2 directionToEnemy = (enemy.position - playerTransform.position).normalized;
        float angleToEnemy = Vector2.Angle(currentAttackDirection, directionToEnemy);
        return angleToEnemy <= halfAngle;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showAttackGizmos) return;
        
        Transform referenceTransform = playerTransform != null ? playerTransform : transform;
        
        // Radio de detección de auto-aim
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(referenceTransform.position, autoAimRadius);
        
        if (attackPoint != null)
        {
            // Arco de ataque
            float halfAngle = attackAngle / 2f;
            Vector3 from = Quaternion.Euler(0, 0, -halfAngle) * currentAttackDirection;
            Handles.color = attackArcColor;
            Handles.DrawSolidArc(referenceTransform.position, Vector3.forward, from, attackAngle, attackRange);
            
            // Línea central de dirección
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(referenceTransform.position, referenceTransform.position + (Vector3)currentAttackDirection * attackRange);
            
            // Línea al objetivo actual
            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(referenceTransform.position, currentTarget.position);
            }
            
            // Punto de ataque
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, 0.1f);
        }
    }
#endif

    // Métodos públicos de acceso
    public void SetAttackEnabled(bool enabled) => isAttackEnabled = enabled;
    public bool IsAttackReady() => Time.time - lastAttackTime >= attackCooldown;
    public int GetDamage() => attackDamage;
    public Vector2 GetAttackDirection() => currentAttackDirection;
    public bool HasTarget() => currentTarget != null;
    public Vector2 GetAttackDirectionNoEnemies() => GetFallbackAttackDirection();
    
    // Cancelar el daño pendiente si es necesario
    public void CancelPendingDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }
}