using UnityEngine;

// Mueve una "ola" en línea recta y daña al jugador al colisionar
[RequireComponent(typeof(Collider2D))]
public class WaveMover : MonoBehaviour
{
    private Vector2 direction = Vector2.down;
    private float speed = 5f;
    private int damage = 1;
    private float lifeTime = 5f;

    private Collider2D col;
    private bool hasDamagedPlayer = false;

    public void Init(Vector2 moveDir, float moveSpeed, int dmg, float lifetime)
    {
        direction = moveDir.sqrMagnitude > 0.0001f ? moveDir.normalized : Vector2.down;
        speed = Mathf.Max(0f, moveSpeed);
        damage = Mathf.Max(0, dmg);
        lifeTime = Mathf.Max(0.01f, lifetime);

        // Orientar visualmente la ola según la dirección (el arte apunta por su eje Up)
        transform.up = direction;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // En caso de que el jugador ya estuviera dentro del volumen al instanciar la ola
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (hasDamagedPlayer) return;
        if (other != null && other.CompareTag("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                hasDamagedPlayer = true;
            }
        }
    }
}
