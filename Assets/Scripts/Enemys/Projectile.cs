// Projectile.cs
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    public float lifetime = 5f;
    public int damage = 1;
    private Vector2 direction;

    private Animator animator;
    private bool hasExploded = false;
    private Rigidbody2D rb;

    private float destroyTime;

    public void Initialize(Vector2 dir)
    {
        direction = dir.normalized;
        RotateTowardsDirection(direction);
        hasExploded = false;
        gameObject.SetActive(true);

        if (rb != null)
            rb.linearVelocity = direction * speed;

        destroyTime = Time.time + lifetime;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!hasExploded && Time.time >= destroyTime)
        {
            Explode();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            Explode();
        }
        else if (!other.isTrigger)
        {
            Explode();
        }
    }

    void Explode()
    {
        hasExploded = true;

        if (animator != null)
        {
            animator.SetTrigger("Explosion");
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Invoke("ReturnToPool", 0.5f);
    }

    void ReturnToPool()
    {
        ProjectilePool.Instance.ReturnProjectile(gameObject);
    }

    void RotateTowardsDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}