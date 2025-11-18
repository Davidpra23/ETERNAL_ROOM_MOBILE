using UnityEngine;

/// <summary>
/// Sistema de arma para espada con slash animado.
/// El daño se aplica mediante el proyectil de SlashProjectile, no directamente.
/// </summary>
public class SlashSwordWeapon : WeaponSystem
{
    public override AttackInputMode Mode => AttackInputMode.TapOnly;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;

    private float lastAttackTime = -999f;

    // Evento que SimpleSwordSlash escucha para ejecutar la animación
    public System.Action OnAttack;

    void Start()
    {
        // Auto-registrarse en el EquipmentManager al iniciar
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.EquipWeapon(this);
            Debug.Log($"[SlashSwordWeapon] Equipada: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[SlashSwordWeapon] EquipmentManager.Instance es null en Start.");
        }
    }

    public override void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
        {
            Debug.Log($"[SlashSwordWeapon] En cooldown ({Time.time - lastAttackTime:F2}s < {attackCooldown}s)");
            return;
        }

        lastAttackTime = Time.time;
        PerformAttack();
    }

    private void PerformAttack()
    {
        Debug.Log("[SlashSwordWeapon] Ataque ejecutado, disparando OnAttack");
        OnAttack?.Invoke();
    }

    // Métodos opcionales para otros modos de input (no se usan con TapOnly)
    public override void OnAttackHoldStart() { }
    public override void OnAttackHoldRelease() { }
    public override void OnAttackHoldCancel() { }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Opcional: dibujar el cooldown en la escena
        if (Application.isPlaying && Time.time - lastAttackTime < attackCooldown)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Cooldown: {(attackCooldown - (Time.time - lastAttackTime)):F2}s");
        }
    }
#endif
}
