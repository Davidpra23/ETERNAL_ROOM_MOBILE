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
/// Controla la orientación y animación de slash de la espada
/// </summary>
public class SwordAimAndSlash : MonoBehaviour
{
    // Nuevas variables para control de cambio de objetivo
    [SerializeField] private float targetLockDuration = 1f; // Duración mínima antes de cambiar de objetivo
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

    [Header("Default Angles")]
    [SerializeField] private float rightAngle = 90f;
    [SerializeField] private float leftAngle = -90f;
    [SerializeField] private float upAngle = 0f;
    [SerializeField] private float downAngle = 180f;

    [Header("Default Slash Angles")]
    [SerializeField] private DefaultSlashAngles defaultSlashAngles = new DefaultSlashAngles();

    // Estados privados
    private Transform currentTarget;
    private bool isSlashing = false;
    private bool hasTarget = false;
    private Vector3 targetPosition;
    private List<Transform> enemiesInRange = new List<Transform>();
    private Vector3 swordForwardDirection = Vector3.up;

    // Rotaciones
    private Quaternion targetRotation;
    private Quaternion defaultRotation;
    private float currentDefaultAngle = 90f;

    #region Unity Methods

    void Awake()
    {
        AutoAssignReferences();
    }

    void Start()
    {
        InitializeComponents();
        SetupDetection();
        SubscribeToEvents();

        swordForwardDirection = (swordTip.position - transform.position).normalized;
        UpdateDefaultRotation();
        targetRotation = defaultRotation;
        transform.rotation = targetRotation;

        UpdateTargetPosition();
        transform.position = targetPosition;
    }

    void Update()
    {
        timeSinceLastTargetChange += Time.deltaTime;

        if (playerTransform == null) return;

        UpdateTargetPosition();
        FollowPlayerPosition();

        if (isSlashing) return;

        FindNearestEnemy();
        UpdateAim();
        ApplyRotation();
        UpdateSwordForwardDirection();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            var enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead && !enemiesInRange.Contains(other.transform))
                enemiesInRange.Add(other.transform);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
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

    #region Auto Assignment

    /// <summary>
    /// Asigna automáticamente todas las referencias necesarias
    /// </summary>
    private void AutoAssignReferences()
    {
        AssignPlayerTransform();
        AssignSwordDamageSystem();
        AssignPlayerMovement();
        AssignSwordTip();
        AssignParentTransform();
    }

    /// <summary>
    /// Busca y asigna el transform del jugador automáticamente
    /// </summary>
    private void AssignPlayerTransform()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (playerTransform == null)
            {
                Debug.LogWarning("Player Transform no encontrado. Buscando en objetos con nombre 'Player'...");
                GameObject playerObj = GameObject.Find("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            if (playerTransform != null)
                Debug.Log("Player Transform asignado automáticamente: " + playerTransform.name);
        }
    }

    /// <summary>
    /// Busca y asigna el sistema de daño de la espada automáticamente
    /// </summary>
    private void AssignSwordDamageSystem()
    {
        if (swordDamageSystem == null)
        {
            // Primero buscar en el mismo GameObject
            swordDamageSystem = GetComponent<SwordDamageSystem>();

            // Si no está, buscar en hijos
            if (swordDamageSystem == null)
                swordDamageSystem = GetComponentInChildren<SwordDamageSystem>();

            // Si todavía no está, buscar en el player
            if (swordDamageSystem == null && playerTransform != null)
            {
                swordDamageSystem = playerTransform.GetComponentInChildren<SwordDamageSystem>();

                // Último intento: buscar en toda la escena
                if (swordDamageSystem == null)
                {
                    swordDamageSystem = FindObjectOfType<SwordDamageSystem>();
                    if (swordDamageSystem != null)
                        Debug.LogWarning("SwordDamageSystem encontrado en la escena pero no en la jerarquía esperada.");
                }
            }

            if (swordDamageSystem != null)
                Debug.Log("SwordDamageSystem asignado automáticamente: " + swordDamageSystem.name);
        }
    }

    /// <summary>
    /// Busca y asigna el componente de movimiento del jugador automáticamente
    /// </summary>
    private void AssignPlayerMovement()
    {
        if (playerMovement == null)
        {
            if (playerTransform != null)
            {
                playerMovement = playerTransform.GetComponent<PlayerMovement>();

                if (playerMovement == null)
                {
                    playerMovement = playerTransform.GetComponentInChildren<PlayerMovement>();

                    if (playerMovement == null)
                    {
                        playerMovement = FindObjectOfType<PlayerMovement>();
                        if (playerMovement != null)
                            Debug.LogWarning("PlayerMovement encontrado en la escena pero no en el jugador.");
                    }
                }
            }
            else
            {
                playerMovement = FindObjectOfType<PlayerMovement>();
            }

            if (playerMovement != null)
                Debug.Log("PlayerMovement asignado automáticamente: " + playerMovement.name);
        }
    }

    /// <summary>
    /// Busca y asigna la punta de la espada automáticamente
    /// </summary>
    private void AssignSwordTip()
    {
        if (swordTip == null)
        {
            // Buscar por nombres comunes
            swordTip = transform.Find("Tip");
            if (swordTip == null) swordTip = transform.Find("SwordTip");
            if (swordTip == null) swordTip = transform.Find("tip");
            if (swordTip == null) swordTip = transform.Find("sword_tip");

            // Buscar por tag
            if (swordTip == null)
            {
                GameObject tipObj = GameObject.FindGameObjectWithTag("SwordTip");
                if (tipObj != null) swordTip = tipObj.transform;
            }

            // Si no se encuentra, usar el propio transform
            if (swordTip == null)
            {
                swordTip = transform;
                Debug.LogWarning("No se encontró SwordTip. Usando el transform actual.");
            }
            else
            {
                Debug.Log("SwordTip asignado automáticamente: " + swordTip.name);
            }
        }
    }

    /// <summary>
    /// Asigna el parent transform automáticamente
    /// </summary>
    private void AssignParentTransform()
    {
        if (parentTransform == null)
        {
            parentTransform = transform.parent;

            if (parentTransform == null)
            {
                Debug.LogWarning("No se encontró parent transform. Usando el transform actual.");
                parentTransform = transform;
            }
            else
            {
                Debug.Log("Parent Transform asignado automáticamente: " + parentTransform.name);
            }
        }
    }

    /// <summary>
    /// Inicializa los componentes después de asignar las referencias
    /// </summary>
    private void InitializeComponents()
    {
        // Verificar que todas las referencias esenciales estén asignadas
        if (playerTransform == null)
        {
            Debug.LogError("No se pudo asignar Player Transform automáticamente!");
            enabled = false;
            return;
        }

        if (swordDamageSystem == null)
        {
            Debug.LogError("No se pudo asignar SwordDamageSystem automáticamente!");
            enabled = false;
            return;
        }

        if (playerMovement == null)
        {
            Debug.LogWarning("PlayerMovement no asignado. Algunas funcionalidades estarán limitadas.");
        }

        if (swordTip == null)
        {
            swordTip = transform;
            Debug.LogWarning("SwordTip no asignado. Usando transform actual.");
        }

        if (parentTransform == null)
        {
            parentTransform = transform;
            Debug.LogWarning("ParentTransform no asignado. Usando transform actual.");
        }
    }

    #endregion

    #region Position & Rotation

    /// <summary>
    /// Actualiza la posición objetivo basada en el jugador
    /// </summary>
    private void UpdateTargetPosition()
    {
        if (playerTransform == null) return;
        targetPosition = playerTransform.position + new Vector3(positionOffset.x, positionOffset.y, 0f);
    }

    /// <summary>
    /// Suavemente sigue la posición del jugador
    /// </summary>
    private void FollowPlayerPosition()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothness * Time.deltaTime);
    }

    /// <summary>
    /// Actualiza la dirección frontal de la espada
    /// </summary>
    private void UpdateSwordForwardDirection()
    {
        if (swordTip != null)
            swordForwardDirection = (swordTip.position - transform.position).normalized;
    }

    /// <summary>
    /// Actualiza la rotación por defecto basada en el movimiento del jugador
    /// </summary>
    private void UpdateDefaultRotation()
    {
        if (playerMovement == null) return;

        Vector2 movementDir = playerMovement.GetMovementDirection();
        if (movementDir.magnitude <= 0.1f) return;

        float absX = Mathf.Abs(movementDir.x);
        float absY = Mathf.Abs(movementDir.y);

        if (absX > absY * 1.2f)
            currentDefaultAngle = movementDir.x > 0 ? rightAngle : leftAngle;
        else if (absY > absX * 1.2f)
            currentDefaultAngle = movementDir.y > 0 ? upAngle : downAngle;

        defaultRotation = Quaternion.Euler(0f, 0f, currentDefaultAngle);
    }

    /// <summary>
    /// Actualiza la dirección de apuntado
    /// </summary>
    private void UpdateAim()
    {
        UpdateDefaultRotation();
        targetRotation = hasTarget && currentTarget != null ? GetAimRotation() : defaultRotation;
    }

    /// <summary>
    /// Calcula la rotación de apuntado hacia el objetivo
    /// </summary>
    private Quaternion GetAimRotation()
    {
        Vector2 directionToTarget = ((Vector2)(currentTarget.position - swordTip.position) + aimOffset).normalized;
        float targetAngle = CalculateAngleFromForward(directionToTarget);
        return Quaternion.Euler(0f, 0f, targetAngle);
    }

    /// <summary>
    /// Calcula el ángulo desde la dirección frontal
    /// </summary>
    private float CalculateAngleFromForward(Vector2 targetDirection)
    {
        float angle = Vector2.SignedAngle(swordForwardDirection, targetDirection);
        return transform.rotation.eulerAngles.z + angle;
    }

    /// <summary>
    /// Aplica suavemente la rotación a la espada
    /// </summary>
    private void ApplyRotation()
    {
        float smoothness = hasTarget ? aimSmoothness : idleSmoothness;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothness * Time.deltaTime);
    }

    #endregion

    #region Enemy Detection

    /// <summary>
    /// Encuentra el enemigo más cercano en el rango de detección
    /// </summary>
    private void FindNearestEnemy()
    {
        if (enemiesInRange.Count == 0)
        {
            currentTarget = null;
            hasTarget = false;
            return;
        }

        enemiesInRange.RemoveAll(enemy => enemy == null || enemy.GetComponent<EnemyHealth>()?.IsDead == true);

        // Si ya tenemos un objetivo válido y no ha pasado suficiente tiempo, mantenlo
        if (currentTarget != null && enemiesInRange.Contains(currentTarget) && timeSinceLastTargetChange < targetLockDuration)
        {
            hasTarget = true;
            return;
        }

        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform enemy in enemiesInRange)
        {
            float distance = Vector3.Distance(transform.position, enemy.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        // Si hay cambio de objetivo, reiniciamos el temporizador
        if (closestEnemy != currentTarget)
        {
            timeSinceLastTargetChange = 0f;
        }

        currentTarget = closestEnemy;
        hasTarget = currentTarget != null;
    }


    #endregion

    #region Slash System

    /// <summary>
    /// Configura el collider de detección de enemigos
    /// </summary>
    private void SetupDetection()
    {
        var detectionCollider = GetComponent<CircleCollider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
        float scaledRadius = detectionRadius / Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        detectionCollider.radius = scaledRadius;
        detectionCollider.isTrigger = true;
    }

    /// <summary>
    /// Suscribe a los eventos del sistema de daño
    /// </summary>
    private void SubscribeToEvents()
    {
        if (swordDamageSystem != null)
            swordDamageSystem.OnAttack += OnPlayerAttack;
    }

    /// <summary>
    /// Evento llamado cuando el jugador realiza un ataque
    /// </summary>
    private void OnPlayerAttack()
    {
        if (!isSlashing)
            StartCoroutine(PerformSlash());
    }

    /// <summary>
    /// Corrutina principal que ejecuta la animación de slash
    /// </summary>
    private IEnumerator PerformSlash()
    {
        isSlashing = true;
        SlashDirectionConfig config = GetSlashDirectionConfig();
        yield return ExecuteSlashPhases(config);
        isSlashing = false;
    }

    /// <summary>
    /// Ejecuta las tres fases del slash: wind-up, slash y return
    /// </summary>
    private IEnumerator ExecuteSlashPhases(SlashDirectionConfig config)
    {
        Quaternion startRotation = transform.rotation;
        float windUpDur = windUpDuration / slashSpeedMultiplier;
        float slashDur = slashDuration / slashSpeedMultiplier;
        float returnDur = returnDuration / slashSpeedMultiplier;

        // Wind-up phase
        Quaternion windUpRotation = startRotation * Quaternion.Euler(0f, 0f, config.windUpAngle);
        yield return RotateTo(windUpRotation, windUpDur, windUpCurve);

        // Slash phase
        Quaternion slashRotation = windUpRotation * Quaternion.Euler(0f, 0f, config.slashAngle);
        yield return RotateTo(slashRotation, slashDur, slashCurve);

        // Return phase
        yield return RotateTo(targetRotation, returnDur, returnCurve);
    }

    /// <summary>
    /// Rotación suavizada hacia un objetivo
    /// </summary>
    private IEnumerator RotateTo(Quaternion targetRot, float duration, AnimationCurve curve)
    {
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.rotation = targetRot;
    }

    /// <summary>
    /// Determina la configuración de slash basada en la dirección
    /// </summary>
    private SlashDirectionConfig GetSlashDirectionConfig()
    {
        if (hasTarget && currentTarget != null)
        {
            return GetSlashConfigFromTarget();
        }
        else
        {
            return GetSlashConfigFromMovement();
        }
    }

    /// <summary>
    /// Obtiene configuración de slash basada en la posición del objetivo
    /// </summary>
    private SlashDirectionConfig GetSlashConfigFromTarget()
    {
        Vector2 relativePos = (currentTarget.position - playerTransform.position).normalized;

        if (Mathf.Abs(relativePos.x) > Mathf.Abs(relativePos.y))
        {
            return relativePos.x > 0 ? CreateRightSlashConfig() : CreateLeftSlashConfig();
        }
        else
        {
            return relativePos.y > 0 ? CreateUpSlashConfig() : CreateDownSlashConfig();
        }
    }

    /// <summary>
    /// Obtiene configuración de slash basada en el movimiento del jugador
    /// </summary>
    private SlashDirectionConfig GetSlashConfigFromMovement()
    {
        Vector2 movementDir = playerMovement != null ? playerMovement.GetMovementDirection() : Vector2.zero;

        if (movementDir.magnitude <= 0.1f)
        {
            return parentTransform != null && parentTransform.localScale.x < 0 ?
                CreateLeftSlashConfig() : CreateRightSlashConfig();
        }

        if (Mathf.Abs(movementDir.x) > Mathf.Abs(movementDir.y))
        {
            return movementDir.x > 0 ? CreateRightSlashConfig() : CreateLeftSlashConfig();
        }
        else
        {
            return movementDir.y > 0 ? CreateUpSlashConfig() : CreateDownSlashConfig();
        }
    }

    /// <summary>
    /// Crea configuración para slash derecho
    /// </summary>
    private SlashDirectionConfig CreateRightSlashConfig()
    {
        return new SlashDirectionConfig
        {
            windUpAngle = defaultSlashAngles.rightWindUpAngle,
            slashAngle = defaultSlashAngles.rightSlashAngle
        };
    }

    /// <summary>
    /// Crea configuración para slash izquierdo
    /// </summary>
    private SlashDirectionConfig CreateLeftSlashConfig()
    {
        return new SlashDirectionConfig
        {
            windUpAngle = defaultSlashAngles.leftWindUpAngle,
            slashAngle = defaultSlashAngles.leftSlashAngle
        };
    }

    /// <summary>
    /// Crea configuración para slash arriba
    /// </summary>
    private SlashDirectionConfig CreateUpSlashConfig()
    {
        return new SlashDirectionConfig
        {
            windUpAngle = GetAdjustedUpWindUpAngle(),
            slashAngle = GetAdjustedUpSlashAngle()
        };
    }

    /// <summary>
    /// Crea configuración para slash abajo
    /// </summary>
    private SlashDirectionConfig CreateDownSlashConfig()
    {
        return new SlashDirectionConfig
        {
            windUpAngle = GetAdjustedDownWindUpAngle(),
            slashAngle = GetAdjustedDownSlashAngle()
        };
    }

    #endregion

    #region Angle Adjustment

    /// <summary>
    /// Ajusta el ángulo de wind-up para arriba basado en la escala del padre
    /// </summary>
    private float GetAdjustedUpWindUpAngle()
    {
        float angle = defaultSlashAngles.upWindUpAngle;
        return parentTransform != null && Mathf.Approximately(parentTransform.localScale.x, 1f) ? -angle : angle;
    }

    /// <summary>
    /// Ajusta el ángulo de slash para arriba basado en la escala del padre
    /// </summary>
    private float GetAdjustedUpSlashAngle()
    {
        float angle = defaultSlashAngles.upSlashAngle;
        return parentTransform != null && Mathf.Approximately(parentTransform.localScale.x, 1f) ? -angle : angle;
    }

    /// <summary>
    /// Ajusta el ángulo de wind-up para abajo basado en la escala del padre
    /// </summary>
    private float GetAdjustedDownWindUpAngle()
    {
        float angle = defaultSlashAngles.downWindUpAngle;
        return parentTransform != null && Mathf.Approximately(parentTransform.localScale.x, 1f) ? -angle : angle;
    }

    /// <summary>
    /// Ajusta el ángulo de slash para abajo basado en la escala del padre
    /// </summary>
    private float GetAdjustedDownSlashAngle()
    {
        float angle = defaultSlashAngles.downSlashAngle;
        return parentTransform != null && Mathf.Approximately(parentTransform.localScale.x, 1f) ? -angle : angle;
    }

    #endregion
}

/// <summary>
/// Configuración para la animación de slash
/// </summary>
public class SlashDirectionConfig
{
    public float windUpAngle;
    public float slashAngle;
}