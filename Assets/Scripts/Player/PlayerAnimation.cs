using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform uniRoot; // Referencia a UniRoot
    
    [Header("Animation Settings")]
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float smoothDirectionChange = 5f;
    
    // Parámetros del Animator
    private const string MOVE_BOOL = "Move";
    private const string ATTACK_TRIGGER = "2_Attack";
    private const string DAMAGED_TRIGGER = "3_Damaged";
    private const string DEATH_TRIGGER = "4_Death";
    private const string DEBUFF_BOOL = "5_Debuff";
    private const string OTHER_TRIGGER = "6_Other";
    
    // Variables internas
    private Vector2 lastDirection = Vector2.right;
    private bool isMoving = false;
    private Vector3 originalScale;
    
    void Awake()
    {
        // Obtener referencias automáticamente si no están asignadas
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        // Si uniRoot no está asignado, usar el transform actual
        if (uniRoot == null)
            uniRoot = transform;
        
        // Guardar la escala original del objeto que vamos a voltear
        originalScale = uniRoot.localScale;
        
        // Suscribirse a eventos del PlayerMovement
        if (playerMovement != null)
        {
            playerMovement.OnMovementStateChanged += HandleMovementStateChange;
            playerMovement.OnMovementDirectionChanged += HandleDirectionChange;
        }
    }
    
    void Update()
    {
        // Actualizar animaciones basadas en el movimiento
        UpdateMovementAnimation();
        
        // Actualizar la orientación del sprite
        UpdateSpriteDirection();
    }
    
    private void UpdateMovementAnimation()
    {
        if (playerMovement != null)
        {
            float currentSpeed = rb != null ? rb.linearVelocity.magnitude : playerMovement.CurrentSpeed;
            isMoving = currentSpeed > movementThreshold;
            animator.SetBool(MOVE_BOOL, isMoving);
        }
    }
    
    private void UpdateSpriteDirection()
    {

    }
    
    private void HandleMovementStateChange(bool moving)
    {
        isMoving = moving;
        animator.SetBool(MOVE_BOOL, moving);
    }
    
    private void HandleDirectionChange(Vector2 direction)
    {
        lastDirection = direction;
        

    }
    
    // ========== MÉTODOS PÚBLICOS ==========
    
    public void PlayAttackAnimation() => animator.SetTrigger(ATTACK_TRIGGER);
    public void PlayDamagedAnimation() => animator.SetTrigger(DAMAGED_TRIGGER);
    public void PlayDeathAnimation() => animator.SetTrigger(DEATH_TRIGGER);
    public void SetDebuffState(bool debuffed) => animator.SetBool(DEBUFF_BOOL, debuffed);
    public void PlayOtherAnimation() => animator.SetTrigger(OTHER_TRIGGER);
    
    public void ResetAllTriggers()
    {
        animator.ResetTrigger(ATTACK_TRIGGER);
        animator.ResetTrigger(DAMAGED_TRIGGER);
        animator.ResetTrigger(DEATH_TRIGGER);
        animator.ResetTrigger(OTHER_TRIGGER);
    }
    
    void OnDestroy()
    {
        if (playerMovement != null)
        {
            playerMovement.OnMovementStateChanged -= HandleMovementStateChange;
            playerMovement.OnMovementDirectionChanged -= HandleDirectionChange;
        }
    }
}