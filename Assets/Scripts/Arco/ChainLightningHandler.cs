using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightningHandler : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int maxChainTargets = 4;
    [SerializeField] private float chainRange = 6f;
    [SerializeField] private float delayPerUnit = 0.02f;

    [Header("FX")]
    [SerializeField] private GameObject lightningVFXPrefab;

    [Header("Debug")]
    [SerializeField] private bool debug = false;

    private int damagePerHit;
    private LayerMask enemyLayer;

    public void StartChain(Transform startTarget, LayerMask enemyLayer, int damage)
    {
        this.damagePerHit = damage;
        this.enemyLayer = enemyLayer;

        if (startTarget == null || lightningVFXPrefab == null) return;
        StartCoroutine(ChainRoutine(startTarget));
    }

    private IEnumerator ChainRoutine(Transform startTarget)
    {
        List<Transform> hitEnemies = new List<Transform> { startTarget };
        Transform current = startTarget;

        // ✅ Aplica daño al primer objetivo (ya lo hizo la flecha, opcional)
        // EnemyHealth ehFirst = current.GetComponent<EnemyHealth>();
        // if (ehFirst != null && !ehFirst.IsDead) ehFirst.TakeDamage(damagePerHit);

        for (int i = 1; i < maxChainTargets; i++)
        {
            Transform next = FindNextTarget(current, hitEnemies);
            if (next == null) break;

            // ✅ Aplica daño al siguiente objetivo
            var eh = next.GetComponent<EnemyHealth>();
            if (eh != null && !eh.IsDead)
                eh.TakeDamage(damagePerHit);

            hitEnemies.Add(next);
            SpawnLightning(current.position, next.position);

            float delay = Vector2.Distance(current.position, next.position) * delayPerUnit;
            if (debug) Debug.Log($"[Chain] Jump {i}: {current.name} → {next.name} | Delay: {delay:F2}s");
            current = next;
            yield return new WaitForSeconds(delay);
        }

        // ✅ destruir este handler tras acabar
        Destroy(gameObject);
    }

    private Transform FindNextTarget(Transform from, List<Transform> exclude)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(from.position, chainRange, enemyLayer);
        float best = Mathf.Infinity;
        Transform chosen = null;

        foreach (var h in hits)
        {
            if (h == null || exclude.Contains(h.transform)) continue;

            var eh = h.GetComponent<EnemyHealth>();
            if (eh != null && !eh.IsDead)
            {
                float d = (h.transform.position - from.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    chosen = h.transform;
                }
            }
        }
        return chosen;
    }

    private void SpawnLightning(Vector2 from, Vector2 to)
    {
        GameObject vfx = Instantiate(lightningVFXPrefab, from, Quaternion.identity);
        Vector2 dir = to - from;

        vfx.transform.right = dir.normalized;
        vfx.transform.position = (from + to) * 0.5f;
        vfx.transform.localScale = new Vector3(dir.magnitude, 1f, 1f);
    }
}
