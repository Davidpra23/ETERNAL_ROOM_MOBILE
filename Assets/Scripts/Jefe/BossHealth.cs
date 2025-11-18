using UnityEngine;
using System;
using System.Collections;

public class BossHealth : MonoBehaviour, IHealth
{
    [Header("Damage Text Settings")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Transform damageTextSpawnPoint;

    [Header("Health Settings")]
    public float maxHealth = 1000f;
    public float currentHealth;
    public bool isDead = false;

    [Header("References (optional)")]
    public Animator animator;

    // porcentaje (0..1)
    public Action<float> OnHealthChanged;
    public Action OnDeath;
    public Action OnDamageTaken;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(HealthPercentage);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);

        OnDamageTaken?.Invoke();
        OnHealthChanged?.Invoke(HealthPercentage);

        if (animator != null)
            animator.SetTrigger("Damaged");

        ShowDamageText(Mathf.RoundToInt(amount));

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(HealthPercentage);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();

        if (animator != null)
            animator.SetTrigger("Death");
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void ShowDamageText(int damageAmount)
    {
        if (damageTextPrefab == null) return;

        Vector3 spawnPos = damageTextSpawnPoint != null ?
            damageTextSpawnPoint.position : transform.position + Vector3.up * 1f;

        GameObject go = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        DamageText dmg = go.GetComponent<DamageText>();

        Color color = Color.white;
        bool isCritical = false;
        if (damageAmount > 20)
        {
            color = Color.yellow;
            isCritical = true;
        }

        dmg.Setup(damageAmount, color, isCritical);
    }
}
