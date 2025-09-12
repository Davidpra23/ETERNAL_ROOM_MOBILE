using UnityEngine;

public class UniRootRotationSystem : MonoBehaviour
{
    [Header("UniRoot Reference")]
    [SerializeField] private Transform uniRoot;
    
    [Header("Aim Settings")]
    [SerializeField] private float aimSmoothness = 10f;
    [SerializeField] private LayerMask enemyLayer = 1 << 8; // Layer de enemigos por defecto
    
    private Vector3 originalUniRootScale;
    private Vector2 currentAimDirection = Vector2.right;
    private AutoAimSystem autoAimSystem;
    private PlayerMovement playerMovement;
    
    void Start()
    {
        if (uniRoot != null)
        {
            originalUniRootScale = uniRoot.localScale;
        }
        
        // Buscar componentes automáticamente
        autoAimSystem = GetComponent<AutoAimSystem>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        
        // Inicializar AutoAimSystem si existe
        if (autoAimSystem != null && playerMovement != null)
        {
            autoAimSystem.Initialize(playerMovement, enemyLayer);
        }
    }
    
    void Update()
    {
        if (uniRoot == null) return;
        
        // Actualizar el auto-aim primero
        UpdateAutoAim();
        
        // Actualizar dirección de aim
        UpdateAimDirection();
        
        // Rotar based on aim
        RotateBasedOnAim();
    }
    
    private void UpdateAutoAim()
    {
        if (autoAimSystem != null)
        {
            autoAimSystem.UpdateAutoAim();
        }
    }
    
    private void UpdateAimDirection()
    {
        // Priorizar auto-aim si está detectando enemigos
        if (autoAimSystem != null && autoAimSystem.IsAimingAtEnemy())
        {
            currentAimDirection = autoAimSystem.GetAimDirection();
            return;
        }
        
        // Fallback: usar dirección de movimiento
        if (playerMovement != null)
        {
            Vector2 movementDir = playerMovement.GetMovementDirection();
            if (movementDir.magnitude > 0.1f)
            {
                currentAimDirection = movementDir;
            }
        }
    }
    
    private void RotateBasedOnAim()
    {
        if (Mathf.Abs(currentAimDirection.x) > 0.1f)
        {
            Vector3 targetScale = uniRoot.localScale;
            
            if (currentAimDirection.x > 0.1f)
            {
                targetScale.x = -Mathf.Abs(originalUniRootScale.x); // Derecha
            }
            else if (currentAimDirection.x < -0.1f)
            {
                targetScale.x = Mathf.Abs(originalUniRootScale.x); // Izquierda
            }
            
            // Suavizar la transición
            uniRoot.localScale = Vector3.Lerp(uniRoot.localScale, targetScale, aimSmoothness * Time.deltaTime);
        }
    }
    
    // Métodos de compatibilidad con PlayerAttack
    public void Initialize(PlayerAttack attack)
    {
        // Buscar componentes si no se encontraron en Start
        if (autoAimSystem == null)
            autoAimSystem = GetComponent<AutoAimSystem>();
        if (playerMovement == null && attack != null)
            playerMovement = attack.GetPlayerMovement();
            
        if (autoAimSystem != null && playerMovement != null)
        {
            autoAimSystem.Initialize(playerMovement, enemyLayer);
        }
    }
    
    public void SetIsAttacking(bool attacking)
    {
        // Mantener por compatibilidad
    }
    
    public void SetHasEnemiesInRange(bool hasEnemies)
    {
        // Mantener por compatibilidad
    }
    
    public void UpdateUniRootRotation()
    {
        // Método vacío para compatibilidad, la rotación se hace en Update()
    }
}