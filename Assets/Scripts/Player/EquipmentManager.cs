using UnityEngine;

[DefaultExecutionOrder(-200)]
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }
    private WeaponSystem currentWeapon;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[EquipMgr] Awake listo");
    }

    public void EquipWeapon(WeaponSystem newWeapon)
    {
        currentWeapon = newWeapon;
        Debug.Log($"[EquipMgr] Arma equipada: {newWeapon.gameObject.name}");
    }

    // Legacy (por si quieres probar con un click)
    public void TriggerAttack()
    {
        if (currentWeapon != null) currentWeapon.TryAttack();
        else Debug.LogWarning("[EquipMgr] Intento de ataque sin arma equipada.");
    }

    // NUEVO: mantener para cargar
    public void StartAttackHold()
    {
        if (currentWeapon != null) currentWeapon.OnAttackHoldStart();
        else Debug.LogWarning("[EquipMgr] HoldStart sin arma.");
    }

    public void ReleaseAttackHold()
    {
        if (currentWeapon != null) currentWeapon.OnAttackHoldRelease();
        else Debug.LogWarning("[EquipMgr] HoldRelease sin arma.");
    }

    // Útil si necesitas cancelar externamente (stun, etc.)
    public void CancelAttackHold()
    {
        if (currentWeapon != null) currentWeapon.OnAttackHoldCancel();
        else Debug.LogWarning("[EquipMgr] HoldCancel sin arma.");
    }

    public WeaponSystem CurrentWeapon => currentWeapon;
}
