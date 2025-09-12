using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeparation : MonoBehaviour
{
    [Header("Separation Settings")]
    [SerializeField] private float separationRadius = 0.6f; // Distancia mínima entre enemigos
    [SerializeField] private float separationForce = 1.5f;  // Fuerza de empuje

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        ApplySeparation();
    }

    private void ApplySeparation()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, separationRadius);

        Vector2 repulsion = Vector2.zero;
        int count = 0;

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Enemy")) // Asegúrate que tus enemigos tengan la tag "Enemy"
            {
                Vector2 diff = (Vector2)(transform.position - hit.transform.position);
                float distance = diff.magnitude;

                if (distance > 0)
                {
                    repulsion += diff.normalized / distance; 
                    count++;
                }
            }
        }

        if (count > 0)
        {
            repulsion /= count;
            rb.linearVelocity += repulsion * separationForce * Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
