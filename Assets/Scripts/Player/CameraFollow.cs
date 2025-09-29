using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    
    [Header("Configuración de seguimiento")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Límites de la cámara")]
    [SerializeField] private Transform boundaryMin;
    [SerializeField] private Transform boundaryMax;
    
    [Header("Configuración avanzada")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = true;
    [SerializeField] private float lookAheadDistance = 0f;
    [SerializeField] private bool useLookAhead = false;

    private Vector3 desiredPosition;
    private Camera cam;
    private Vector2 cameraHalfSize;
    private float retryTimer = 0f; // para controlar la búsqueda

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            float cameraHeight = cam.orthographicSize * 2f;
            float cameraWidth = cameraHeight * cam.aspect;
            cameraHalfSize = new Vector2(cameraWidth / 2f, cameraHeight / 2f);
        }
        else
        {
            cameraHalfSize = Vector2.one;
        }

        TryFindPlayer();
    }

    private void Update()
    {
        if (target == null)
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0f)
            {
                TryFindPlayer();
                retryTimer = 1f; // reintentar cada segundo
            }
            return;
        }

        desiredPosition = CalculateTargetPosition();
        desiredPosition = ApplyCameraBounds(desiredPosition);
        transform.position = desiredPosition;
    }

    private void TryFindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            target = player.transform;
            Debug.Log($"CameraFollow: Player encontrado de nuevo: {target.name}");
        }
    }

    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPos = target.position;

        if (useLookAhead && lookAheadDistance > 0)
        {
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

        Vector3 finalPosition = targetPos + offset;

        if (!followX) finalPosition.x = transform.position.x;
        if (!followY) finalPosition.y = transform.position.y;
        if (!followZ) finalPosition.z = transform.position.z;

        return finalPosition;
    }

    private Vector3 ApplyCameraBounds(Vector3 targetPosition)
    {
        if (boundaryMin == null || boundaryMax == null)
            return targetPosition;

        float minX = boundaryMin.position.x + cameraHalfSize.x;
        float maxX = boundaryMax.position.x - cameraHalfSize.x;
        float minY = boundaryMin.position.y + cameraHalfSize.y;
        float maxY = boundaryMax.position.y - cameraHalfSize.y;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        return targetPosition;
    }
}
