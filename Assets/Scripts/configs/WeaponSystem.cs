using UnityEngine;

public abstract class WeaponSystem : MonoBehaviour
{
    public enum AttackInputMode
    {
        TapOnly,             // Un clic ejecuta TryAttack()
        HoldReleaseOnly,     // Se ataca al soltar (OnAttackHoldStart/Release)
        TapAndHoldCharged    // Mantener carga efectos/bonos; soltar dispara; tap cuenta como tiro corto
    }

    // Modo de entrada por defecto (se puede sobreescribir en armas concretas)
    public virtual AttackInputMode Mode => AttackInputMode.TapOnly;

    // Soporte legacy (clic único)
    public virtual void TryAttack() {}

    // Nuevo: ciclo de carga
    public virtual void OnAttackHoldStart() {}
    public virtual void OnAttackHoldRelease() {}
    public virtual void OnAttackHoldCancel() {}

    // Info opcional
    public virtual bool IsCharging => false;
}
