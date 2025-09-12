using UnityEngine;

public class SlashMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxDistance = 2f;
    
    [Header("Combat Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask enemyLayer;
    
    private Vector3 startPosition;
    private bool hasHit = false;

    public void Initialize(Vector2 direction, float speed, float distance, int slashDamage, LayerMask enemyMask)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        maxDistance = distance;
        damage = slashDamage;
        enemyLayer = enemyMask;
        startPosition = transform.position;
        
        // Orientar el slash en la dirección de movimiento
        if (moveDirection.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void Update()
    {
        if (!hasHit)
        {
            MoveSlash();
            CheckDistance();
            DetectEnemies();
        }
    }

    private void MoveSlash()
    {
        // Mover el slash en la dirección configurada
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    private void CheckDistance()
    {
        // Verificar si ha alcanzado la distancia máxima
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void DetectEnemies()
    {
        // Detectar enemigos en el camino del slash
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 0.5f, enemyLayer);
        
        foreach (Collider2D enemy in hitEnemies)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(damage);
                hasHit = true;
                
                // Destruir el slash después de golpear
                Destroy(gameObject, 0.1f);
                break;
            }
        }
    }

    void OnDrawGizmos()
    {
        // Dibujar dirección de movimiento
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)moveDirection * 1f);
    }
}