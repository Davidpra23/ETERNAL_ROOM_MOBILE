using UnityEngine;

public class AttackDirectionSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fieldOfViewAngle = 180f;
    [SerializeField] private Transform fovReferencePoint;
    [SerializeField] private Vector3 fovForwardDirection = Vector3.right;
    
    private PlayerMovement playerMovement;
    private AutoAimSystem autoAimSystem;
    private Vector2 lastMovementDirection = Vector2.right;
    private AttackDirection currentAttackDirection = AttackDirection.Right;
    
    public enum AttackDirection { Right, Left, Up, Down }
    
    public void Initialize(PlayerMovement movement, AutoAimSystem aimSystem)
    {
        playerMovement = movement;
        autoAimSystem = aimSystem;
        
        if (fovReferencePoint == null) fovReferencePoint = transform;
    }
    
    public void UpdateAttackDirection()
    {
        if (playerMovement == null) return;
        
        Vector2 directionToUse = (autoAimSystem != null && autoAimSystem.IsAutoAimEnabled()) ? 
            autoAimSystem.GetAimDirection() : playerMovement.LastMovementDirection;
        
        if (directionToUse.magnitude > 0.1f)
        {
            Vector2 normalizedDir = directionToUse.normalized;
            float absX = Mathf.Abs(normalizedDir.x);
            float absY = Mathf.Abs(normalizedDir.y);
            
            if (absX > absY * 1.2f)
            {
                currentAttackDirection = normalizedDir.x > 0 ? AttackDirection.Right : AttackDirection.Left;
            }
            else if (absY > absX * 1.2f)
            {
                currentAttackDirection = normalizedDir.y > 0 ? AttackDirection.Up : AttackDirection.Down;
            }
        }
    }
    
    public bool IsInFieldOfView(Transform targetTransform)
    {
        if (fovReferencePoint == null) return true;
        
        Vector3 directionToTarget = (targetTransform.position - fovReferencePoint.position).normalized;
        Vector3 worldForward = GetAttackForwardDirection();
        
        float angle = Vector3.Angle(worldForward, directionToTarget);
        return angle <= fieldOfViewAngle / 2f;
    }
    
    public Vector3 GetAttackForwardDirection()
    {
        switch (currentAttackDirection)
        {
            case AttackDirection.Right: return Vector3.right;
            case AttackDirection.Left: return Vector3.left;
            case AttackDirection.Up: return Vector3.up;
            case AttackDirection.Down: return Vector3.down;
            default: return Vector3.right;
        }
    }
    
    public AttackDirection GetCurrentAttackDirection() => currentAttackDirection;
    public Vector2 GetLastMovementDirection() => lastMovementDirection;
}