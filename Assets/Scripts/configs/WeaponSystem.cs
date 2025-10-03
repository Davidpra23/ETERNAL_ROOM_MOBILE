using UnityEngine;

public abstract class WeaponSystem : MonoBehaviour
{
    // Soporte legacy (clic único)
    public virtual void TryAttack() {}

    // Nuevo: ciclo de carga
    public virtual void OnAttackHoldStart() {}
    public virtual void OnAttackHoldRelease() {}
    public virtual void OnAttackHoldCancel() {}

    // Info opcional
    public virtual bool IsCharging => false;
}
