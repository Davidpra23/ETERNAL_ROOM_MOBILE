using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class AcidPuddle : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damagePerTick = 5;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float lifeTime = 6f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private SpriteRenderer sr;
    private float spawnTime;

    private class TickInfo
    {
        public float nextTickTime;
        public PlayerHealth health;
    }

    private Dictionary<Collider2D, TickInfo> activeTargets = new Dictionary<Collider2D, TickInfo>();

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(0f);
    }

    private void Start()
    {
        spawnTime = Time.time;
        StartCoroutine(FadeIn());

        Invoke(nameof(BeginFadeOut), lifeTime - fadeDuration);
    }

    private void BeginFadeOut()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(0f, 1f, t / fadeDuration));
            yield return null;
        }
        SetAlpha(1f);
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(1f, 0f, t / fadeDuration));
            yield return null;
        }
        Destroy(gameObject);
    }

    private void SetAlpha(float a)
    {
        if (sr)
        {
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        if (!activeTargets.ContainsKey(other))
        {
            health.TakeDamage(damagePerTick); // Primer daño inmediato
            activeTargets.Add(other, new TickInfo
            {
                health = health,
                nextTickTime = Time.time + tickInterval
            });
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!activeTargets.TryGetValue(other, out TickInfo info)) return;

        if (Time.time >= info.nextTickTime)
        {
            info.health.TakeDamage(damagePerTick);
            info.nextTickTime = Time.time + tickInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (activeTargets.ContainsKey(other))
            activeTargets.Remove(other);
    }
}
