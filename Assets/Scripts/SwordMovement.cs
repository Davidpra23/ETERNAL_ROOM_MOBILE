using UnityEngine;

public class SwordMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float movementDistance = 1f;
    [SerializeField] private bool startMovingUp = true;

    [Header("Movement Options")]
    [SerializeField] private bool useLocalSpace = true;
    [SerializeField] private bool smoothMovement = true;
    [SerializeField] private float smoothFactor = 5f;

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool movingUp;
    private float progress;

    void Start()
    {
        // Guardar la posición inicial
        initialPosition = useLocalSpace ? transform.localPosition : transform.position;
        
        // Configurar dirección inicial
        movingUp = startMovingUp;
        
        // Calcular primera posición objetivo
        CalculateTargetPosition();
    }

    void Update()
    {
        // Actualizar progreso del movimiento
        progress += Time.deltaTime * movementSpeed;
        
        if (progress >= 1f)
        {
            // Cambiar dirección cuando se alcanza el objetivo
            movingUp = !movingUp;
            progress = 0f;
            CalculateTargetPosition();
        }

        // Calcular nueva posición
        Vector3 newPosition;
        
        if (smoothMovement)
        {
            // Movimiento suave usando Lerp
            float smoothedProgress = Mathf.SmoothStep(0f, 1f, progress);
            newPosition = Vector3.Lerp(initialPosition, targetPosition, smoothedProgress);
        }
        else
        {
            // Movimiento lineal
            newPosition = Vector3.Lerp(initialPosition, targetPosition, progress);
        }

        // Aplicar movimiento suavizado
        if (smoothMovement)
        {
            if (useLocalSpace)
                transform.localPosition = Vector3.Lerp(transform.localPosition, newPosition, smoothFactor * Time.deltaTime);
            else
                transform.position = Vector3.Lerp(transform.position, newPosition, smoothFactor * Time.deltaTime);
        }
        else
        {
            // Aplicar movimiento directamente
            if (useLocalSpace)
                transform.localPosition = newPosition;
            else
                transform.position = newPosition;
        }
    }

    private void CalculateTargetPosition()
    {
        // Calcular la posición objetivo basada en la dirección
        Vector3 direction = movingUp ? Vector3.up : Vector3.down;
        targetPosition = initialPosition + (direction * movementDistance);
    }

    // Métodos públicos para modificar valores en tiempo real
    public void SetMovementSpeed(float newSpeed)
    {
        movementSpeed = Mathf.Max(0.1f, newSpeed);
    }

    public void SetMovementDistance(float newDistance)
    {
        movementDistance = Mathf.Max(0.1f, newDistance);
        initialPosition = useLocalSpace ? transform.localPosition : transform.position;
        CalculateTargetPosition();
    }

    public void SetMovingUp(bool moveUp)
    {
        movingUp = moveUp;
        progress = 0f;
        CalculateTargetPosition();
    }

    public void ResetToInitialPosition()
    {
        if (useLocalSpace)
            transform.localPosition = initialPosition;
        else
            transform.position = initialPosition;
        
        progress = 0f;
        CalculateTargetPosition();
    }

    // Para debug visual en el editor
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(useLocalSpace ? transform.parent.TransformPoint(initialPosition) : initialPosition, 0.1f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(useLocalSpace ? transform.parent.TransformPoint(targetPosition) : targetPosition, 0.1f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(useLocalSpace ? transform.parent.TransformPoint(initialPosition) : initialPosition, 
                           useLocalSpace ? transform.parent.TransformPoint(targetPosition) : targetPosition);
        }
        else
        {
            // Mostrar rango de movimiento en el editor
            Gizmos.color = Color.cyan;
            Vector3 currentPos = transform.position;
            Gizmos.DrawWireSphere(currentPos + Vector3.up * movementDistance, 0.1f);
            Gizmos.DrawWireSphere(currentPos + Vector3.down * movementDistance, 0.1f);
            Gizmos.DrawLine(currentPos + Vector3.up * movementDistance, currentPos + Vector3.down * movementDistance);
        }
    }
}