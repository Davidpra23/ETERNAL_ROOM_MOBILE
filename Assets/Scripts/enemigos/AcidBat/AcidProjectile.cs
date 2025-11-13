using UnityEngine;
using System.Collections;

public class AcidProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float travelTime = 0.6f;
    [SerializeField] private float arcHeight = 1.5f;

    [Header("Damage")]
    [SerializeField] private int damage = 10;

    [Header("FX")]
    [SerializeField] private GameObject trailVFXPrefab;
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private GameObject acidPuddlePrefab;
    [SerializeField] private float acidPuddleDelay = 0.5f;

    private Vector2 startPos;
    private Vector2 targetPos;
    private float startTime;
    private bool initialized;
    private bool hasHitPlayer;

    public void Init(Vector2 target)
    {
        startPos = transform.position;
        targetPos = target;
        startTime = Time.time;
        initialized = true;

        if (trailVFXPrefab)
        {
            GameObject trail = Instantiate(trailVFXPrefab, transform);
            trail.transform.localPosition = Vector3.zero;
        }
    }

    private void Update()
    {
        if (!initialized) return;

        float t = (Time.time - startTime) / Mathf.Max(0.01f, travelTime);
        if (t >= 1f)
        {
            StartCoroutine(HandleImpact());
            initialized = false;
            return;
        }

        Vector2 pos = Vector2.Lerp(startPos, targetPos, t);
        float heightOffset = arcHeight * Mathf.Sin(t * Mathf.PI);
        transform.position = pos + Vector2.up * heightOffset;

        // ✅ Orientar hacia el objetivo
        Vector2 dir = (targetPos - startPos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }


    private IEnumerator HandleImpact()
    {
        // 🔻 Oculta el sprite visual
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.enabled = false;

        if (!hasHitPlayer)
        {
            Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.6f);
            if (hit != null && hit.CompareTag("Player"))
            {
                hasHitPlayer = true;
                var playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(damage);
            }
        }

        if (impactVFXPrefab)
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);

        if (acidPuddlePrefab)
        {
            yield return new WaitForSeconds(acidPuddleDelay);
            Instantiate(acidPuddlePrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

}
