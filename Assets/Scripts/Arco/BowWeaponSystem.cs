using UnityEngine;
using System.Collections;

public class BowWeaponSystem : WeaponSystem
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform point1;
    [SerializeField] private Transform point2;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    [SerializeField] private string aimBoolName = "Aim";
    [SerializeField] private string shootTriggerName = "Shoot";
    [SerializeField] private string idleTriggerName = "Idle";

    [Header("Bow Base Stats")]
    [SerializeField] private int baseDamage = 8;
    [SerializeField] private float baseCooldown = 0.8f;
    [SerializeField] private float arrowSpeed = 12f;
    [SerializeField] private float arrowLifeTime = 3f;

    [Header("Auto-Aim")]
    [SerializeField] private bool enableAutoAim = true;
    [SerializeField] private float autoAimRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Charging")]
    [Tooltip("Tiempo necesario de carga para permitir disparo.")]
    [SerializeField] private float chargeTimeRequired = 0.4f;

    [Header("Charge FX")]
    [SerializeField] private GameObject chargeEffectPrefab;
    [SerializeField] private Vector3 chargeEffectOffset = Vector3.zero;

    [Header("Charged FX")]
    [SerializeField] private GameObject chargedEffectPrefab;
    [SerializeField] private Vector3 chargedEffectOffset = Vector3.zero;

    [Header("Chain Lightning (Epic)")]
    [SerializeField] private bool enableChainLightning = true;
    [SerializeField] private ChainLightningHandler chainLightningHandlerPrefab;

    public System.Action OnAimStarted;
    public System.Action OnShoot;
    public System.Action OnChargeCanceled;

    private PlayerCombatStats stats;
    private Transform currentTarget;
    private float lastShotTime = -999f;

    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private bool chargeCompleted = false;

    private GameObject ghostArrow;
    private GameObject chargeFXInstance;
    private GameObject chargedFXInstance;

    public override bool IsCharging => isCharging;

    private void Awake()
    {
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (playerTransform != null && stats == null)
            stats = playerTransform.GetComponent<PlayerCombatStats>();

        if (point1 == null) point1 = transform.Find("Point1");
        if (point2 == null) point2 = transform.Find("Point2");
    }

    private void OnEnable() => TryRegisterWeapon();

    private void Update()
    {
        if (enableAutoAim) AcquireTarget();

        if (isCharging && ghostArrow != null && point1 != null && point2 != null)
        {
            float t = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.01f, chargeTimeRequired));
            ghostArrow.transform.position = Vector3.Lerp(point1.position, point2.position, t);

            Vector2 dir = GetShootDirection();
            if (dir.sqrMagnitude > 0.0001f)
                ghostArrow.transform.right = dir;

            if (chargeFXInstance != null)
            {
                float scale = Mathf.Lerp(0.5f, 1.2f, t);
                chargeFXInstance.transform.localScale = Vector3.one * scale;
            }

            if (!chargeCompleted && t >= 1f)
            {
                chargeCompleted = true;
                TryShowChargedEffect();
            }
        }
    }

    private void TryRegisterWeapon()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.EquipWeapon(this);
        }
        else
        {
            StartCoroutine(WaitAndRegister());
        }
    }

    private IEnumerator WaitAndRegister()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForEndOfFrame();
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.EquipWeapon(this);
                yield break;
            }
        }
        Debug.LogWarning("[Bow] No se pudo registrar el arma: EquipmentManager no encontrado.");
    }

    public override void TryAttack()
    {
        OnAttackHoldStart();
        StartCoroutine(FireNextFrameIfCharged());
    }

    private IEnumerator FireNextFrameIfCharged()
    {
        yield return null;
        OnAttackHoldRelease();
    }

    public override void OnAttackHoldStart()
    {
        if (!IsShotReady() || isCharging || point1 == null || point2 == null || arrowPrefab == null)
        {
            Debug.LogWarning("[Bow] Falta Point1/Point2/arrowPrefab o ya está cargando.");
            return;
        }

        isCharging = true;
        chargeStartTime = Time.time;
        chargeCompleted = false;

        PlayIdleTrigger();
        OnAimStarted?.Invoke();
        if (animator) SafeSetBool(animator, aimBoolName, true);

        ghostArrow = Instantiate(arrowPrefab, point1.position, Quaternion.identity);
        var proj = ghostArrow.GetComponent<ArrowProjectile>();
        if (proj) proj.enabled = false;
        ForcePositiveScale(ghostArrow.transform);

        if (chargeEffectPrefab != null)
        {
            chargeFXInstance = Instantiate(chargeEffectPrefab, ghostArrow.transform);
            chargeFXInstance.transform.localPosition = chargeEffectOffset;
            chargeFXInstance.transform.localScale = Vector3.one * 0.5f;
        }

        lastShotTime = Time.time;
    }

    private void TryShowChargedEffect()
    {
        if (chargedEffectPrefab != null && ghostArrow != null)
        {
            chargedFXInstance = Instantiate(chargedEffectPrefab, ghostArrow.transform);
            chargedFXInstance.transform.localPosition = chargedEffectOffset;
        }
    }

    public override void OnAttackHoldRelease()
    {
        if (!isCharging) return;

        float elapsed = Time.time - chargeStartTime;
        bool fullyCharged = elapsed >= chargeTimeRequired;

        FireArrow(fullyCharged);
        EndChargeState();
        lastShotTime = Time.time;
    }

    public override void OnAttackHoldCancel()
    {
        if (!isCharging) return;
        PlayIdleTrigger();
        CancelChargeInternal();
    }

    private void CancelChargeInternal()
    {
        OnChargeCanceled?.Invoke();
        if (animator) SafeSetBool(animator, aimBoolName, false);

        DestroyEffects();
        Destroy(ghostArrow);
        ResetChargeState();
    }

    private void EndChargeState()
    {
        if (animator)
        {
            SafeSetBool(animator, aimBoolName, false);
            SafeSetTrigger(animator, shootTriggerName);
        }

        DestroyEffects();
        Destroy(ghostArrow);
        ResetChargeState();
    }

    private void DestroyEffects()
    {
        if (chargeFXInstance != null) Destroy(chargeFXInstance);
        if (chargedFXInstance != null) Destroy(chargedFXInstance);
        chargeFXInstance = null;
        chargedFXInstance = null;
    }

    private void ResetChargeState()
    {
        ghostArrow = null;
        isCharging = false;
        chargeCompleted = false;
    }

    private void FireArrow(bool fullyCharged)
    {
        if (point2 == null || arrowPrefab == null) return;

        Vector3 spawnPos = point2.position;
        Vector2 dir = GetShootDirection();
        if (dir == Vector2.zero) dir = Vector2.right;

        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        ForcePositiveScale(arrow.transform);

        var proj = arrow.GetComponent<ArrowProjectile>();
        if (proj != null)
        {
            float speed = EffectiveArrowSpeed();
            if (!fullyCharged)
                speed *= 0.5f;

            proj.Init(
                damage: ComputeDamage(),
                direction: dir.normalized,
                speed: speed,
                lifeTime: Mathf.Max(0.1f, arrowLifeTime),
                enemyLayer: enemyLayer,
                ignoreThese: null,
                enableChain: fullyCharged && enableChainLightning,
                chainPrefab: fullyCharged ? chainLightningHandlerPrefab : null
            );
        }

        OnShoot?.Invoke();
    }

    private bool IsShotReady() => Time.time - lastShotTime >= EffectiveCooldown();

    private float EffectiveCooldown()
    {
        if (stats == null) return baseCooldown;
        return stats.ComputeCooldown(baseCooldown);
    }

    private int ComputeDamage()
    {
        float dmg = (stats != null) ? stats.ComputeDamage(baseDamage) : baseDamage;
        if (stats != null && stats.RollCrit(out float critMult)) dmg *= critMult;
        return Mathf.RoundToInt(dmg);
    }

    private float EffectiveArrowSpeed()
    {
        if (stats == null) return arrowSpeed;
        return arrowSpeed * Mathf.Max(0.5f, stats.attackSpeedMultiplier);
    }

    private Vector2 GetShootDirection()
    {
        if (playerTransform != null && currentTarget != null)
        {
            Vector2 to = (currentTarget.position - playerTransform.position);
            if (to.sqrMagnitude > 0.0001f) return to.normalized;
        }

        if (playerTransform != null)
        {
            var pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                var moveDir = pm.GetMovementDirection();
                if (moveDir.sqrMagnitude > 0.0001f) return moveDir.normalized;
            }
        }
        return Vector2.right;
    }

    private void AcquireTarget()
    {
        if (playerTransform == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, autoAimRadius, enemyLayer);
        float best = Mathf.Infinity;
        Transform chosen = null;

        foreach (var h in hits)
        {
            if (h == null) continue;
            var eh = h.GetComponent<EnemyHealth>();
            if (eh != null && !eh.IsDead)
            {
                float d = (h.transform.position - playerTransform.position).sqrMagnitude;
                if (d < best) { best = d; chosen = h.transform; }
            }
        }
        currentTarget = chosen;
    }

    private void ForcePositiveScale(Transform t)
    {
        Vector3 ls = t.localScale;
        ls.x = Mathf.Abs(ls.x);
        ls.y = Mathf.Abs(ls.y);
        ls.z = Mathf.Abs(ls.z);
        t.localScale = ls;
    }

    private void PlayIdleTrigger()
    {
        if (animator && !string.IsNullOrEmpty(idleTriggerName))
            SafeSetTrigger(animator, idleTriggerName);
    }

    private void SafeSetBool(Animator anim, string boolName, bool value)
    {
        if (anim == null || string.IsNullOrEmpty(boolName)) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == boolName)
            { anim.SetBool(boolName, value); return; }
    }

    private void SafeSetTrigger(Animator anim, string triggerName)
    {
        if (anim == null || string.IsNullOrEmpty(triggerName)) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            { anim.SetTrigger(triggerName); return; }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Transform refT = playerTransform ? playerTransform : transform;
        Gizmos.DrawWireSphere(refT.position, autoAimRadius);

        if (point1) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(point1.position, 0.06f); }
        if (point2) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(point2.position, 0.06f); }
    }
#endif
}
