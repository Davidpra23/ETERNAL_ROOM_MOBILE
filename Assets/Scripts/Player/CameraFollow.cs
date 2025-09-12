using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    
    [Header("Configuración de seguimiento")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Límites de la cámara")]
    [SerializeField] private Transform boundaryMin; // Esquina inferior izquierda del área
    [SerializeField] private Transform boundaryMax; // Esquina superior derecha del área
    
    [Header("Configuración avanzada")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = true;
    [SerializeField] private float lookAheadDistance = 0f;
    [SerializeField] private bool useLookAhead = false;
    
    private Vector3 desiredPosition;
    private Camera cam;
    private Vector2 cameraHalfSize;
    
    private void Start()
    {
        // Buscar automáticamente el player si no está asignado
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(targetTag);
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"CameraFollow: Objetivo encontrado automáticamente: {target.name}");
            }
            else
            {
                Debug.LogWarning($"CameraFollow: No se encontró ningún objeto con tag '{targetTag}'. Asigna manualmente el objetivo.");
            }
        }
        
        // Obtener referencia a la cámara y calcular su tamaño
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            float cameraHeight = cam.orthographicSize * 2f;
            float cameraWidth = cameraHeight * cam.aspect;
            cameraHalfSize = new Vector2(cameraWidth / 2f, cameraHeight / 2f);
        }
        else
        {
            Debug.LogWarning("CameraFollow: No se encontró componente Camera. Los límites podrían no funcionar correctamente.");
            cameraHalfSize = Vector2.one;
        }
        
        // Configurar la posición inicial de la cámara
        if (target != null)
        {
            transform.position = CalculateTargetPosition();
        }
        
        Debug.Log("CameraFollow: Seguimiento directo activado - sin suavizado");
    }
    
    private void Update()
    {
        if (target == null) return;
        
        // Calcular la posición objetivo
        desiredPosition = CalculateTargetPosition();
        
        // Aplicar límites a la cámara
        desiredPosition = ApplyCameraBounds(desiredPosition);
        
        // Seguimiento directo sin suavizado
        transform.position = desiredPosition;
    }
    
    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPos = target.position;
        
        // Aplicar look ahead si está habilitado
        if (useLookAhead && lookAheadDistance > 0)
        {
            // Obtener la dirección del movimiento del player
            PlayerMovement playerMovement = target.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                Vector2 velocity = playerMovement.GetCurrentVelocity();
                if (velocity.magnitude > 0.1f)
                {
                    Vector3 lookAheadOffset = new Vector3(velocity.x, velocity.y, 0).normalized * lookAheadDistance;
                    targetPos += lookAheadOffset;
                }
            }
        }
        
        // Aplicar offset
        Vector3 finalPosition = targetPos + offset;
        
        // Aplicar restricciones de ejes
        if (!followX) finalPosition.x = transform.position.x;
        if (!followY) finalPosition.y = transform.position.y;
        if (!followZ) finalPosition.z = transform.position.z;
        
        return finalPosition;
    }
    
    private Vector3 ApplyCameraBounds(Vector3 targetPosition)
    {
        // Si no hay límites definidos, retornar la posición sin cambios
        if (boundaryMin == null || boundaryMax == null)
            return targetPosition;
        
        // Calcular los límites del mundo basados en los objetos de referencia
        float minX = boundaryMin.position.x + cameraHalfSize.x;
        float maxX = boundaryMax.position.x - cameraHalfSize.x;
        float minY = boundaryMin.position.y + cameraHalfSize.y;
        float maxY = boundaryMax.position.y - cameraHalfSize.y;
        
        // Aplicar límites a la posición de la cámara
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        
        return targetPosition;
    }
    
    // Métodos públicos para control externo
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    public void SetBoundaries(Transform minBoundary, Transform maxBoundary)
    {
        boundaryMin = minBoundary;
        boundaryMax = maxBoundary;
    }
    
    public void EnableLookAhead(float distance)
    {
        useLookAhead = true;
        lookAheadDistance = distance;
    }
    
    public void DisableLookAhead()
    {
        useLookAhead = false;
    }
    
    // Método para hacer que la cámara vaya inmediatamente a la posición del objetivo
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = CalculateTargetPosition();
        }
    }
    
    // Método para hacer shake de cámara
    public void ShakeCamera(float intensity, float duration)
    {
        StartCoroutine(CameraShakeCoroutine(intensity, duration));
    }
    
    private System.Collections.IEnumerator CameraShakeCoroutine(float intensity, float duration)
    {
        Vector3 originalPosition = transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            transform.position = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
    }
    
    // Método para dibujar los límites en el editor
    private void OnDrawGizmosSelected()
    {
        // Dibujar límites de la cámara si están asignados
        if (boundaryMin != null && boundaryMax != null)
        {
            Gizmos.color = Color.green;
            Vector3 center = (boundaryMin.position + boundaryMax.position) / 2f;
            Vector3 size = boundaryMax.position - boundaryMin.position;
            Gizmos.DrawWireCube(center, size);
            
            // Dibujar iconos en los puntos de límite
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(boundaryMin.position, 0.3f);
            Gizmos.DrawSphere(boundaryMax.position, 0.3f);
        }
        
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.5f);
            
            if (useLookAhead)
            {
                Gizmos.color = Color.blue;
                Vector3 lookAheadPos = CalculateTargetPosition();
                Gizmos.DrawLine(target.position, lookAheadPos);
                Gizmos.DrawWireSphere(lookAheadPos, 0.3f);
            }
        }
    }
}