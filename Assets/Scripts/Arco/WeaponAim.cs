using UnityEngine;
using System.Collections.Generic;

public class WeaponAim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform parentTransform; // Se asigna automáticamente si está vacío

    [Header("Aim Settings")]
    [SerializeField] private float aimSmoothness = 20f;
    [SerializeField] private float idleSmoothness = 8f;
    [SerializeField] private Vector2 aimOffset = Vector2.zero;

    [Header("Base Sprite Angles")]
    [Tooltip("Ángulo de la espada mirando a la derecha (en grados, tu sprite calibra aquí).")]
    [SerializeField] private float rightAngle = 90f;
    [SerializeField] private float leftAngle = -90f;
    [SerializeField] private float upAngle = 0f;
    [SerializeField] private float downAngle = 180f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private Transform currentTarget;
    private bool hasTarget = false;
    private Quaternion targetRotation;
    private List<Transform> enemiesInRange = new();

    void Awake()
    {
        AutoAssignParentTransform();
    }

    void Update()
    {
        if (!playerTransform) return;

        FindNearestEnemy();
        UpdateAim360();
        ApplyRotation();
    }

    private void FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);

        enemiesInRange.Clear();
        foreach (var hit in hits)
        {
            if (hit && hit.TryGetComponent(out EnemyHealth eh) && !eh.IsDead)
                enemiesInRange.Add(hit.transform);
        }

        float best = float.PositiveInfinity;
        Transform closest = null;

        foreach (var enemy in enemiesInRange)
        {
            float dist = (enemy.position - transform.position).sqrMagnitude;
            if (dist < best)
            {
                best = dist;
                closest = enemy;
            }
        }

        currentTarget = closest;
        hasTarget = currentTarget != null;
    }

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
            if (move.sqrMagnitude > 0.001f)
            {
                float theta = AngleFromVector360(move);
                float baseAngle = GetBaseSpriteAngleFromTheta(theta);
                targetRotation = Quaternion.Euler(0, 0, baseAngle);
            }
            // Si no se mueve, mantiene targetRotation
        }
    }

    private void ApplyRotation()
    {
        float s = hasTarget ? aimSmoothness : idleSmoothness;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, s * Time.deltaTime);
    }

    private static float AngleFromVector360(Vector2 v) =>
        (Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 360f) % 360f;

    private static float LerpAngle(float a, float b, float t) =>
        a + Mathf.DeltaAngle(a, b) * Mathf.Clamp01(t);

    private float AdjustUpDown(float angle)
    {
        if (!parentTransform) return angle;
        return Mathf.Approximately(parentTransform.localScale.x, 1f) ? -angle : angle;
    }

    private float GetBaseSpriteAngleFromTheta(float theta)
    {
        float aRight = rightAngle;
        float aLeft = leftAngle;
        float aUp = AdjustUpDown(upAngle);
        float aDown = AdjustUpDown(downAngle);

        if (theta < 90f)
        {
            float t = theta / 90f;
            return LerpAngle(aRight, aUp, t);
        }
        else if (theta < 180f)
        {
            float t = (theta - 90f) / 90f;
            return LerpAngle(aUp, aLeft, t);
        }
        else if (theta < 270f)
        {
            float t = (theta - 180f) / 90f;
            return LerpAngle(aLeft, aDown, t);
        }
        else
        {
            float t = (theta - 270f) / 90f;
            return LerpAngle(aDown, aRight, t);
        }
    }

    private void AutoAssignParentTransform()
    {
        if (parentTransform != null) return;

        Transform t = transform;
        for (int i = 0; i < 2; i++)
        {
            if (t.parent != null)
                t = t.parent;
            else
                return; // No hay suficientes niveles arriba
        }

        parentTransform = t;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}
