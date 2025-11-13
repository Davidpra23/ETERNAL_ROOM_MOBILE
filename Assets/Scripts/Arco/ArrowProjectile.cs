using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour
{
    public enum DestroyReason { None, HitEnemy, Lifetime, BecameInvisible, External }

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool destroyWhenInvisible = false;

    [Header("Runtime (set en Init)")]
    [SerializeField] private int damage;
    [SerializeField] private Vector2 dir;
    [SerializeField] private float speed;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("FX")]
    [SerializeField] private GameObject hitVFX;

    // Chain Lightning
    [SerializeField] private bool chainLightningEnabled = false;
    [SerializeField] private ChainLightningHandler chainLightningPrefab;

    private Rigidbody2D rb;
    private DestroyReason reason = DestroyReason.None;
    private bool initialized;
    private Collider2D myCollider;
    private Collider2D[] ownerColliders;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = false;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        myCollider = GetComponent<Collider2D>();
        if (myCollider == null) Debug.LogWarning("[Arrow] Falta Collider2D (usa IsTrigger).");
    }

    public void Init(int damage, Vector2 direction, float speed, float lifeTime, LayerMask enemyLayer, Collider2D[] ignoreThese = null, bool enableChain = false, ChainLightningHandler chainPrefab = null)
    {
        this.damage = damage;
        this.dir = direction.sqrMagnitude < 0.0001f ? Vector2.right : direction.normalized;
        this.speed = speed;
        this.lifeTime = Mathf.Max(0.1f, lifeTime);
        this.enemyLayer = enemyLayer;

        this.chainLightningEnabled = enableChain;
        this.chainLightningPrefab = chainPrefab;

        transform.right = this.dir;
        transform.position += (Vector3)(this.dir * 0.05f);

        rb.linearVelocity = this.dir * this.speed;

        ownerColliders = ignoreThese;
        if (ownerColliders != null && myCollider != null)
        {
            foreach (var oc in ownerColliders)
            {
                if (oc == null) continue;
                Physics2D.IgnoreCollision(myCollider, oc, true);
            }
        }

        spawnTime = Time.time;
        initialized = true;
        if (debugLogs) Debug.Log($"[Arrow] INIT dmg={damage} speed={speed} life={lifeTime}s dir={this.dir}");
    }

    private void Update()
    {
        if (!initialized) return;

        if (Time.time - spawnTime >= lifeTime)
        {
            reason = DestroyReason.Lifetime;
            if (debugLogs) Debug.Log($"[Arrow] Destroy by lifetime {lifeTime}s");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;

        if (ownerColliders != null)
        {
            foreach (var oc in ownerColliders)
            {
                if (oc == other) return; // ignora dueño
            }
        }

        if (((1 << other.gameObject.layer) & enemyLayer.value) == 0) return;

        var eh = other.GetComponent<EnemyHealth>();
        if (eh != null && !eh.IsDead)
        {
            eh.TakeDamage(damage);
            if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
            reason = DestroyReason.HitEnemy;

            // ✅ Chain Lightning
            if (chainLightningEnabled && chainLightningPrefab != null)
            {
                ChainLightningHandler chain = Instantiate(chainLightningPrefab, transform.position, Quaternion.identity);
                chain.StartChain(other.transform, enemyLayer, damage);
            }


            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        if (!initialized) return;
        if (destroyWhenInvisible)
        {
            reason = DestroyReason.BecameInvisible;
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (reason == DestroyReason.None && initialized)
        {
            reason = DestroyReason.External;
            if (debugLogs) Debug.LogWarning("[Arrow] Destroyed EXTERNALLY");
        }
    }
}
