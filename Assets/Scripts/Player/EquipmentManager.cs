// EquipmentManager.cs (Modificado)
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    // MODIFICADO: Ahora guarda cualquier tipo de WeaponSystem.
    private WeaponSystem currentWeapon;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    // MODIFICADO: El parámetro ahora es del tipo base.
    public void EquipWeapon(WeaponSystem newWeapon)
    {
        currentWeapon = newWeapon;
        Debug.Log($"Arma equipada: {newWeapon.gameObject.name}");
    }

    public void TriggerAttack()
    {
        if (currentWeapon != null)
        {
            currentWeapon.TryAttack();
        }
        else
        {
            Debug.LogWarning("Intento de ataque sin arma equipada.");
        }
    }
}