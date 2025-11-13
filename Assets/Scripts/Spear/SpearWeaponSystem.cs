using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SpearWeaponSystem : WeaponSystem
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform uniRootTransform;   // hijo del player que se escala
    [SerializeField] private Transform spearTip;           // punta real
    [SerializeField] private LayerMask enemyLayer;

    [Header("Spear Settings")]
    [SerializeField] private float extendSpeed = 10f;
    [SerializeField] private float retractSpeed = 15f;
    [SerializeField] private float maxRange = 2.5f;
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float cooldown = 0.8f;

    [Header("Axis")]
    [Tooltip("Eje de ataque de la punta (rojo = X, verde = Y).")]
    [SerializeField] private bool useAxisX = true;

    [Header("Smoothing")]
    [Tooltip("Controla la suavidad del movimiento (1 = normal, >1 = más suave)")]
    [SerializeField] private float motionSmoothness = 1.5f;

    [Header("Hit Detection")]
    [SerializeField] private float tipHitRadius = 0.18f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerCombatStats stats;
    private WeaponAim weaponAimRef;
    private bool weaponAimPrevEnabled = false;

    private Vector3 initialLocalPos;
    private bool isAttacking = false;
    private bool isReturning = false;
    private float lastAttackTime = -999f;
    private Vector3 lockedDirLocal;
    private readonly HashSet<IHealth> hitEnemies = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.isKinematic = true;
        col.isTrigger = true;

        if (!playerTransform)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (playerTransform && stats == null)
            stats = playerTransform.GetComponent<PlayerCombatStats>();

        if (!uniRootTransform)
        {
            Transform maybeUniRoot = playerTransform?.Find("UniRoot");
            if (maybeUniRoot) uniRootTransform = maybeUniRoot;
            else Debug.LogWarning("[Spear] No se asignó UniRoot. No podrá detectar flips.");
        }

        if (!spearTip)
            Debug.LogWarning("[Spear] Asigna spearTip (punta de la lanza).");

        weaponAimRef = GetComponentInParent<WeaponAim>();
        initialLocalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.EquipWeapon(this);
    }

    // -------------------------------------------------------
    // Integración con Pc_Attack / EquipmentManager
    // -------------------------------------------------------
    public override void OnAttackHoldStart() { }

    public override void OnAttackHoldRelease()
    {
        if (isAttacking || Time.time - lastAttackTime < cooldown) return;
        StartCoroutine(SpearAttackRoutine());
    }

    public override void OnAttackHoldCancel() { }

    // -------------------------------------------------------
    // Movimiento local suavizado
    // -------------------------------------------------------
    private IEnumerator SpearAttackRoutine()
    {
        isAttacking = true;
        isReturning = false;
        hitEnemies.Clear();
        lastAttackTime = Time.time;

        // Desactivar el componente WeaponAim mientras dura el ataque (según petición)
        if (weaponAimRef != null)
        {
            weaponAimPrevEnabled = weaponAimRef.enabled;
            weaponAimRef.enabled = false;
        }

        Vector3 startLocal = transform.localPosition;

        // Dirección mundial desde la punta
        Transform tip = spearTip ? spearTip : transform;
        Vector3 dirWorld = (useAxisX ? tip.right : tip.up).normalized;

        if (uniRootTransform && uniRootTransform.lossyScale.x < 0)
            dirWorld *= -1f;

        // Convertir a dirección local
        lockedDirLocal = transform.parent.InverseTransformDirection(dirWorld).normalized;

    Vector3 endLocal = startLocal + lockedDirLocal * maxRange;
    float dist = Vector3.Distance(startLocal, endLocal);

        // AVANCE
        float t = 0f;
        while (t < 1f && !isReturning)
        {
            // Recalcular la dirección objetivo cada frame para seguir el aim actual
            Transform tipDyn = spearTip ? spearTip : transform;
            Vector3 dirWorldDyn = (useAxisX ? tipDyn.right : tipDyn.up).normalized;
            if (uniRootTransform && uniRootTransform.lossyScale.x < 0)
                dirWorldDyn *= -1f;
            Vector3 lockedDirLocalDyn = transform.parent.InverseTransformDirection(dirWorldDyn).normalized;
            endLocal = startLocal + lockedDirLocalDyn * maxRange;
            float distCurrent = Mathf.Max(0.001f, Vector3.Distance(startLocal, endLocal));

            t += (Time.deltaTime * extendSpeed / distCurrent) / motionSmoothness;

            // 🎯 Curva de aceleración/desaceleración
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(startLocal, endLocal, smoothT);

            TryHitAtTip();
            yield return null;
        }

        // RETORNO
        yield return ReturnToLocal(startLocal);
    }

    private IEnumerator ReturnToLocal(Vector3 localTarget)
    {
        isReturning = true;
        Vector3 start = transform.localPosition;
        float dist = Vector3.Distance(start, localTarget);
        float t = 0f;

        while (t < 1f)
        {
            t += (Time.deltaTime * retractSpeed / dist) / motionSmoothness;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(start, localTarget, smoothT);
            yield return null;
        }

        transform.localPosition = localTarget;
        // Restaurar estado del WeaponAim si lo desactivamos al iniciar el ataque
        if (weaponAimRef != null)
        {
            weaponAimRef.enabled = weaponAimPrevEnabled;
        }

        isAttacking = false;
        isReturning = false;
    }

    // -------------------------------------------------------
    // Daño: atraviesa múltiples enemigos
    // -------------------------------------------------------
    private void TryHitAtTip()
    {
        if (!isAttacking) return;

        Vector2 tipPos = spearTip ? (Vector2)spearTip.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(tipPos, tipHitRadius, enemyLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IHealth>(out var health) && !health.IsDead && !hitEnemies.Contains(health))
            {
                int dmg = ComputeDamage();
                // Use interface method (float) - implementations may round as needed
                health.TakeDamage(dmg);
                hitEnemies.Add(health);
            }
        }
    }

    private int ComputeDamage()
    {
        float dmg = (stats != null) ? stats.ComputeDamage(baseDamage) : baseDamage;
        if (stats != null && stats.RollCrit(out float critMult)) dmg *= critMult;
        return Mathf.RoundToInt(dmg);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform tip = spearTip ? spearTip : transform;
        Vector3 dir = (useAxisX ? tip.right : tip.up).normalized;

        if (uniRootTransform && uniRootTransform.lossyScale.x < 0)
            dir *= -1f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(tip.position, tip.position + dir * maxRange);
        Gizmos.DrawWireSphere(tip.position, 0.04f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(tip.position, tipHitRadius);
    }
#endif
}
