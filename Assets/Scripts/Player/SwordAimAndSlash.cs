using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DefaultSlashAngles
{
    [Header("Right Direction")]
    public float rightWindUpAngle = 30f;
    public float rightSlashAngle = -90f;

    [Header("Left Direction")]
    public float leftWindUpAngle = -30f;
    public float leftSlashAngle = 90f;

    [Header("Up Direction")]
    public float upWindUpAngle = -30f;
    public float upSlashAngle = 90f;

    [Header("Down Direction")]
    public float downWindUpAngle = 30f;
    public float downSlashAngle = -90f;
}

/// <summary>
/// Controla la orientación y animación de slash de la espada.
/// - Sin enemigos: aim 360° según movimiento del jugador (interpolando ángulos base).
/// - Con enemigos: aim hacia el target.
/// - El slash se adapta a cualquier ángulo mezclando tus presets (Right/Up/Left/Down).
/// </summary>
public class SwordAimAndSlash : MonoBehaviour
{
    [SerializeField] private float targetLockDuration = 1f;
    private float timeSinceLastTargetChange = 0f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SwordDamageSystem swordDamageSystem;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform swordTip;
    [SerializeField] private Transform parentTransform;

    [Header("Aim Settings")]
    [SerializeField] private float aimSmoothness = 20f;
    [SerializeField] private float idleSmoothness = 8f;
    [SerializeField] private Vector2 aimOffset = Vector2.zero;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Position")]
    [SerializeField] private Vector2 positionOffset = Vector2.zero;
    [SerializeField] private float followSmoothness = 12f;

    [Header("Slash Settings")]
    [SerializeField] private float windUpDuration = 0.1f;
    [SerializeField] private float slashDuration = 0.2f;
    [SerializeField] private float returnDuration = 0.15f;
    [SerializeField] private float slashSpeedMultiplier = 1f;
    [SerializeField] private AnimationCurve windUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Default Angles (base sprite)")]
    [Tooltip("Ángulo de la espada mirando a la derecha (en grados, tu sprite calibra aquí).")]
    [SerializeField] private float rightAngle = 90f;
    [SerializeField] private float leftAngle = -90f;
    [SerializeField] private float upAngle = 0f;
    [SerializeField] private float downAngle = 180f;

    [Header("Default Slash Angles (delta por dirección)")]
    [SerializeField] private DefaultSlashAngles defaultSlashAngles = new DefaultSlashAngles();

    // Estado
    private Transform currentTarget;
    private bool isSlashing = false;
    private bool hasTarget = false;
    private Vector3 targetPosition;
    private readonly List<Transform> enemiesInRange = new();
    private Vector3 swordForwardDirection = Vector3.up;

    private Quaternion targetRotation;
    private Quaternion defaultRotation;

    #region Unity

    void Awake() => AutoAssignReferences();

    void Start()
    {
        InitializeComponents();
        SetupDetection();
        SubscribeToEvents();

        swordForwardDirection = (swordTip.position - transform.position).normalized;
        targetRotation = defaultRotation = transform.rotation;

        UpdateTargetPosition();
        transform.position = targetPosition;
    }

    void Update()
    {
        timeSinceLastTargetChange += Time.deltaTime;
        if (!playerTransform) return;

        UpdateTargetPosition();
        FollowPlayerPosition();

        if (isSlashing) return;

        FindNearestEnemy();
        UpdateAim360();
        ApplyRotation();
        UpdateSwordForwardDirection();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        var eh = other.GetComponent<EnemyHealth>();
        if (eh != null && !eh.IsDead && !enemiesInRange.Contains(other.transform))
            enemiesInRange.Add(other.transform);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        enemiesInRange.Remove(other.transform);
    }

    void OnDestroy()
    {
        if (swordDamageSystem != null)
            swordDamageSystem.OnAttack -= OnPlayerAttack;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (swordTip != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, swordForwardDirection * 2f);
            Gizmos.DrawWireSphere(swordTip.position, 0.1f);
        }

        if (hasTarget && currentTarget != null && swordTip != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(swordTip.position, currentTarget.position);
        }
    }
#endif

    #endregion

    #region Auto-assign & Init

    private void AutoAssignReferences()
    {
        if (!playerTransform)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform ?? GameObject.Find("Player")?.transform;

        if (!swordDamageSystem)
            swordDamageSystem = GetComponent<SwordDamageSystem>() ?? GetComponentInChildren<SwordDamageSystem>() ??
                                (playerTransform ? playerTransform.GetComponentInChildren<SwordDamageSystem>() : null) ??
                                FindObjectOfType<SwordDamageSystem>();

        if (!playerMovement)
            playerMovement = playerTransform ? playerTransform.GetComponentInChildren<PlayerMovement>() : FindObjectOfType<PlayerMovement>();

        if (!swordTip)
        {
            swordTip = transform.Find("Tip") ?? transform.Find("SwordTip") ?? transform.Find("tip") ?? transform.Find("sword_tip");
            if (!swordTip) swordTip = transform;
        }

        if (!parentTransform) parentTransform = transform.parent ? transform.parent : transform;
    }

    private void InitializeComponents()
    {
        if (!playerTransform || !swordDamageSystem)
        {
            Debug.LogError("[SwordAimAndSlash] Faltan referencias críticas. Deshabilitando.");
            enabled = false;
            return;
        }
    }

    #endregion

    #region Posición/Rotación

    private void UpdateTargetPosition()
    {
        if (!playerTransform) return;
        targetPosition = playerTransform.position + (Vector3)positionOffset;
    }

    private void FollowPlayerPosition()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothness * Time.deltaTime);
    }

    private void UpdateSwordForwardDirection()
    {
        if (swordTip)
            swordForwardDirection = (swordTip.position - transform.position).normalized;
    }

    /// <summary>
    /// Aiming 360°:
    /// - Con target: mira al enemigo usando la mezcla de ángulos base.
    /// - Sin target: mira según vector de movimiento (si no se mueve, mantiene la última).
    /// </summary>
    private void UpdateAim360()
    {
        if (hasTarget && currentTarget != null)
        {
            Vector2 dir = (currentTarget.position - playerTransform.position);
            float theta = AngleFromVector360(dir + aimOffset);
            float baseAngle = GetBaseSpriteAngleFromTheta(theta);
            targetRotation = Quaternion.Euler(0, 0, baseAngle);
        }
        else
        {
            Vector2 move = playerMovement ? playerMovement.GetMovementDirection() : Vector2.zero;
            if (move.sqrMagnitude > 0.0001f)
            {
                float theta = AngleFromVector360(move);
                float baseAngle = GetBaseSpriteAngleFromTheta(theta);
                targetRotation = Quaternion.Euler(0, 0, baseAngle);
            }
            // si no se mueve, se mantiene el targetRotation actual (idle suave).
        }
    }

    private void ApplyRotation()
    {
        float s = hasTarget ? aimSmoothness : idleSmoothness;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, s * Time.deltaTime);
    }

    #endregion

    #region Enemy Detection

    private void FindNearestEnemy()
    {
        if (enemiesInRange.Count == 0)
        {
            currentTarget = null;
            hasTarget = false;
            return;
        }

        enemiesInRange.RemoveAll(e => !e || e.GetComponent<EnemyHealth>()?.IsDead == true);

        if (currentTarget != null && enemiesInRange.Contains(currentTarget) && timeSinceLastTargetChange < targetLockDuration)
        {
            hasTarget = true;
            return;
        }

        Transform closest = null;
        float best = float.PositiveInfinity;
        Vector3 p = transform.position;

        for (int i = 0; i < enemiesInRange.Count; i++)
        {
            var t = enemiesInRange[i];
            if (!t) continue;
            float d = (t.position - p).sqrMagnitude; // sqr para evitar sqrt
            if (d < best) { best = d; closest = t; }
        }

        if (closest != currentTarget) timeSinceLastTargetChange = 0f;
        currentTarget = closest;
        hasTarget = currentTarget != null;
    }

    #endregion

    #region Slash System

    private void SetupDetection()
    {
        var col = GetComponent<CircleCollider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
        float s = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        col.radius = detectionRadius / (s <= 0 ? 1f : s);
        col.isTrigger = true;
    }

    private void SubscribeToEvents()
    {
        if (swordDamageSystem != null)
            swordDamageSystem.OnAttack += OnPlayerAttack;
    }

    private void OnPlayerAttack()
    {
        if (!isSlashing)
            StartCoroutine(PerformSlash());
    }

    private IEnumerator PerformSlash()
    {
        isSlashing = true;

        // Ángulo actual de aim para construir un slash coherente con esa dirección
        float theta = GetCurrentAimTheta();
        SlashDirectionConfig cfg = GetBlendedSlashConfig(theta);

        // Fases
        Quaternion startRot = transform.rotation;
        Quaternion windUpRot = startRot * Quaternion.Euler(0, 0, cfg.windUpAngle);
        Quaternion slashRot = windUpRot * Quaternion.Euler(0, 0, cfg.slashAngle);

        float w = windUpDuration / Mathf.Max(0.0001f, slashSpeedMultiplier);
        float s = slashDuration / Mathf.Max(0.0001f, slashSpeedMultiplier);
        float r = returnDuration / Mathf.Max(0.0001f, slashSpeedMultiplier);

        yield return RotateTo(windUpRot, w, windUpCurve);
        yield return RotateTo(slashRot, s, slashCurve);
        yield return RotateTo(targetRotation, r, returnCurve);

        isSlashing = false;
    }

    private IEnumerator RotateTo(Quaternion targetRot, float duration, AnimationCurve curve)
    {
        Quaternion start = transform.rotation;
        float t = 0f;
        if (duration <= 0f)
        {
            transform.rotation = targetRot;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.rotation = Quaternion.Slerp(start, targetRot, curve.Evaluate(k));
            yield return null;
        }
        transform.rotation = targetRot;
    }

    #endregion

    #region 360° Mapping (ángulos base + mezcla de slash)

    // 0..360, 0 = +X (derecha), sentido anti-horario
    private static float AngleFromVector360(Vector2 v) =>
        (Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 360f) % 360f;

    // Lerp angular seguro (maneja wrap-around).
    private static float LerpAngle(float a, float b, float t) =>
        a + Mathf.DeltaAngle(a, b) * Mathf.Clamp01(t);

    // Retorna el ángulo de sprite (en grados) mezclando Right/Up/Left/Down según theta (0=Right, 90=Up, 180=Left, 270=Down).
    private float GetBaseSpriteAngleFromTheta(float theta)
    {
        // Determina el sector y la mezcla
        if (theta < 90f)
        {
            float t = theta / 90f;
            return LerpAngle(rightAngle, upAngle, t);
        }
        else if (theta < 180f)
        {
            float t = (theta - 90f) / 90f;
            return LerpAngle(upAngle, leftAngle, t);
        }
        else if (theta < 270f)
        {
            float t = (theta - 180f) / 90f;
            return LerpAngle(leftAngle, downAngle, t);
        }
        else
        {
            float t = (theta - 270f) / 90f;
            return LerpAngle(downAngle, rightAngle, t);
        }
    }

    // Configuración de slash interpolada desde presets (Right/Up/Left/Down) según theta.
    private SlashDirectionConfig GetBlendedSlashConfig(float theta)
    {
        // Crea 4 configs base
        var rightCfg = new SlashDirectionConfig
        {
            windUpAngle = defaultSlashAngles.rightWindUpAngle,
            slashAngle = defaultSlashAngles.rightSlashAngle
        };

        var leftCfg = new SlashDirectionConfig
        {
            windUpAngle = defaultSlashAngles.leftWindUpAngle,
            slashAngle = defaultSlashAngles.leftSlashAngle
        };

        // Up/Down ajustan signo si tu parent está “flippeado” como en tu lógica original
        var upCfg = new SlashDirectionConfig
        {
            windUpAngle = AdjustUpDown(defaultSlashAngles.upWindUpAngle),
            slashAngle = AdjustUpDown(defaultSlashAngles.upSlashAngle)
        };

        var downCfg = new SlashDirectionConfig
        {
            windUpAngle = AdjustUpDown(defaultSlashAngles.downWindUpAngle),
            slashAngle = AdjustUpDown(defaultSlashAngles.downSlashAngle)
        };

        // Mezcla por sector
        if (theta < 90f)
        {
            float t = theta / 90f;
            return LerpSlashConfig(rightCfg, upCfg, t);
        }
        else if (theta < 180f)
        {
            float t = (theta - 90f) / 90f;
            return LerpSlashConfig(upCfg, leftCfg, t);
        }
        else if (theta < 270f)
        {
            float t = (theta - 180f) / 90f;
            return LerpSlashConfig(leftCfg, downCfg, t);
        }
        else
        {
            float t = (theta - 270f) / 90f;
            return LerpSlashConfig(downCfg, rightCfg, t);
        }
    }

    private SlashDirectionConfig LerpSlashConfig(SlashDirectionConfig a, SlashDirectionConfig b, float t)
    {
        return new SlashDirectionConfig
        {
            windUpAngle = LerpAngle(a.windUpAngle, b.windUpAngle, t),
            slashAngle = LerpAngle(a.slashAngle, b.slashAngle, t)
        };
    }

    // Conserva tu comportamiento de invertir Up/Down según flip X del parent (por compatibilidad visual)
    private float AdjustUpDown(float angle)
    {
        if (!parentTransform) return angle;
        // Si tu sprite “mirando a la derecha” implica invertir vertical, copia tu condición:
        return Mathf.Approximately(parentTransform.localScale.x, 1f) ? -angle : angle;
    }

    private float GetCurrentAimTheta()
    {
        if (hasTarget && currentTarget)
            return AngleFromVector360((Vector2)(currentTarget.position - playerTransform.position) + aimOffset);

        Vector2 move = playerMovement ? playerMovement.GetMovementDirection() : Vector2.zero;
        if (move.sqrMagnitude < 0.0001f)
        {
            // Si está quieto, usa el ángulo actual del targetRotation para coherencia
            return (targetRotation.eulerAngles.z - rightAngle + 360f) % 360f;
        }
        return AngleFromVector360(move);
    }

    #endregion
}

/// <summary> Configuración para la animación de slash (delta de rotación). </summary>
public class SlashDirectionConfig
{
    public float windUpAngle;
    public float slashAngle;
}
