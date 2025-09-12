using UnityEngine;

public class SwordFollower : MonoBehaviour
{
    [Header("Target Following")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Vector2 positionOffset = Vector2.zero;
    [SerializeField] private float followSmoothness = 5f;
    
    [Header("Position Constraints")]
    [SerializeField] private bool useXConstraint = true;
    [SerializeField] private bool useYConstraint = true;
    [SerializeField] private float minXOffset = -1f;
    [SerializeField] private float maxXOffset = 1f;
    [SerializeField] private float minYOffset = -1f;
    [SerializeField] private float maxYOffset = 1f;
    
    [Header("Rotation Following")]
    [SerializeField] private bool followPlayerRotation = false;
    [SerializeField] private float rotationSmoothness = 5f;
    
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        if (playerTarget == null)
        {
            Debug.LogError("Player Target no asignado en SwordFollower!");
            enabled = false;
            return;
        }
        
        // Inicializar en la posición del player
        transform.position = GetTargetPosition();
        transform.rotation = GetTargetRotation();
    }

    void Update()
    {
        if (playerTarget == null) return;
        
        UpdateTargetPosition();
        UpdateTargetRotation();
        ApplyMovement();
        ApplyRotation();
    }
    
    private void UpdateTargetPosition()
    {
        targetPosition = GetTargetPosition();
    }
    
    private Vector3 GetTargetPosition()
    {
        Vector3 basePosition = playerTarget.position;
        
        // Aplicar offset configurable
        float offsetX = Mathf.Clamp(positionOffset.x, minXOffset, maxXOffset);
        float offsetY = Mathf.Clamp(positionOffset.y, minYOffset, maxYOffset);
        
        Vector3 offset = new Vector3(offsetX, offsetY, 0);
        
        return basePosition + offset;
    }
    
    private void UpdateTargetRotation()
    {
        if (followPlayerRotation)
        {
            targetRotation = GetTargetRotation();
        }
    }
    
    private Quaternion GetTargetRotation()
    {
        if (followPlayerRotation)
        {
            return playerTarget.rotation;
        }
        return transform.rotation;
    }
    
    private void ApplyMovement()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothness * Time.deltaTime);
    }
    
    private void ApplyRotation()
    {
        if (followPlayerRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
        }
    }
    
    // Métodos públicos para configuración en tiempo real
    public void SetPositionOffset(Vector2 newOffset)
    {
        positionOffset = newOffset;
    }
    
    public void SetOffsetX(float xOffset)
    {
        positionOffset.x = Mathf.Clamp(xOffset, minXOffset, maxXOffset);
    }
    
    public void SetOffsetY(float yOffset)
    {
        positionOffset.y = Mathf.Clamp(yOffset, minYOffset, maxYOffset);
    }
    
    public void SetFollowSmoothness(float smoothness)
    {
        followSmoothness = Mathf.Max(0.1f, smoothness);
    }
    
    public void SetRotationFollowing(bool enable)
    {
        followPlayerRotation = enable;
    }
    
    public Vector2 GetCurrentOffset()
    {
        return positionOffset;
    }
    
    public void TeleportToTarget()
    {
        if (playerTarget != null)
        {
            transform.position = GetTargetPosition();
            transform.rotation = GetTargetRotation();
        }
    }
    
    // Para debug visual
    void OnDrawGizmosSelected()
    {
        if (playerTarget != null)
        {
            // Dibujar línea al target
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTarget.position);
            
            // Dibujar posición objetivo
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetTargetPosition(), 0.1f);
            
            // Dibujar área de constraints
            if (useXConstraint || useYConstraint)
            {
                Gizmos.color = Color.cyan;
                Vector3 center = playerTarget.position;
                Vector3 size = new Vector3(
                    useXConstraint ? (maxXOffset - minXOffset) : 2f,
                    useYConstraint ? (maxYOffset - minYOffset) : 2f,
                    0.1f
                );
                
                Gizmos.DrawWireCube(center + new Vector3(
                    (minXOffset + maxXOffset) * 0.5f,
                    (minYOffset + maxYOffset) * 0.5f,
                    0f), size);
            }
        }
    }
}