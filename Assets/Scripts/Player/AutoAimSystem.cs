using UnityEngine;
using System;

public class AutoAimSystem : MonoBehaviour
{
    [Header("Auto-Aim Settings")]
    [SerializeField] private bool enableAutoAim = true;
    [SerializeField] private float autoAimRadius = 2.5f;
    [SerializeField] private float autoAimSmoothness = 8f;
    [SerializeField] private float returnToMovementSmoothness = 12f;
    
    private PlayerMovement playerMovement;
    private LayerMask enemyLayer;
    private Vector2 autoAimDirection = Vector2.right;
    private Vector2 smoothAimDirection = Vector2.right;
    private bool isAimingAtEnemy = false;
    private float noEnemyTimer = 0f;
    
    // Eventos para notificar cambios
    public event Action<Vector2> OnAutoAimDirectionChanged;
    public event Action<bool> OnAutoAimStateChanged;
    
    public void Initialize(PlayerMovement movement, LayerMask layerMask)
    {
        playerMovement = movement;
        enemyLayer = layerMask;
    }
    
    public void UpdateAutoAim()
    {
        if (!enableAutoAim || playerMovement == null) return;
        
        Vector2 movementDir = playerMovement.GetMovementDirection();
        Vector2 preferredAimDir = FindBestAutoAimDirection(movementDir);
        bool wasAiming = isAimingAtEnemy;
        
        if (preferredAimDir != Vector2.zero)
        {
            isAimingAtEnemy = true;
            noEnemyTimer = 0f;
            smoothAimDirection = Vector2.Lerp(smoothAimDirection, preferredAimDir, autoAimSmoothness * Time.deltaTime);
            autoAimDirection = smoothAimDirection.normalized;
            
            // Notificar cambio de dirección
            OnAutoAimDirectionChanged?.Invoke(autoAimDirection);
        }
        else
        {
            noEnemyTimer += Time.deltaTime;
            
            if (noEnemyTimer > 0.3f)
            {
                isAimingAtEnemy = false;
                Vector2 targetDir = movementDir.magnitude > 0.1f ? movementDir.normalized : autoAimDirection;
                smoothAimDirection = Vector2.Lerp(smoothAimDirection, targetDir, returnToMovementSmoothness * Time.deltaTime);
                autoAimDirection = smoothAimDirection.normalized;
            }
        }
        
        // Notificar cambio de estado
        if (wasAiming != isAimingAtEnemy)
        {
            OnAutoAimStateChanged?.Invoke(isAimingAtEnemy);
        }
    }
    
    private Vector2 FindBestAutoAimDirection(Vector2 movementDirection)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, autoAimRadius, enemyLayer);
        if (nearbyEnemies.Length == 0) return Vector2.zero;
        
        Vector2 bestDirection = Vector2.zero;
        float bestScore = -Mathf.Infinity;
        
        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy == null) continue;
            
            // Verificar si es un enemigo válido
            if (enemy.GetComponent<EnemyHealth>() != null)
            {
                Vector2 toEnemy = (enemy.transform.position - transform.position).normalized;
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                float distanceScore = 1f / (distance + 0.1f);
                float alignmentScore = Vector2.Dot(toEnemy, movementDirection) * 0.5f;
                float totalScore = distanceScore + alignmentScore;
                
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestDirection = toEnemy;
                }
            }
        }
        
        return bestDirection;
    }
    
    public Vector2 GetAimDirection() => autoAimDirection;
    public bool IsAutoAimEnabled() => enableAutoAim;
    public bool IsAimingAtEnemy() => isAimingAtEnemy;
    
    public void SetAutoAim(bool enabled) => enableAutoAim = enabled;
    public void SetAutoAimRadius(float radius) => autoAimRadius = Mathf.Max(1f, radius);
}