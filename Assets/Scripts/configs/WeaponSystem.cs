// WeaponSystem.cs
using UnityEngine;

public abstract class WeaponSystem : MonoBehaviour
{
    // Todas las armas DEBEN tener un método TryAttack().
    // 'abstract' significa que la clase base no define cómo funciona,
    // cada arma creará su propia implementación.
    public abstract void TryAttack();
}