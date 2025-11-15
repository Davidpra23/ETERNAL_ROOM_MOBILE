using UnityEngine;

public class WeaponSoundIntegrator : MonoBehaviour
{
    private WeaponSoundSystem weaponSoundSystem;
    private EquipmentManager equipmentManager;
    private SwordDamageSystem swordDamageSystem;

    void Start()
    {
        // Buscar sistemas existentes
        equipmentManager = EquipmentManager.Instance;
        weaponSoundSystem = FindObjectOfType<WeaponSoundSystem>();
        swordDamageSystem = FindObjectOfType<SwordDamageSystem>();

        // Suscribirse a eventos si existen
        if (swordDamageSystem != null)
        {
            swordDamageSystem.OnAttack += OnSwordAttack;
        }

        Debug.Log("✅ WeaponSoundIntegrator inicializado");
    }

    // 🔥 Escuchar cuando SwordDamageSystem ataca
    private void OnSwordAttack()
    {
        if (weaponSoundSystem != null)
        {
            weaponSoundSystem.ReproducirSonidoAtaque();
        }
    }

    // 🔥 Escuchar cuando EquipmentManager activa ataques (para PC)
    void Update()
    {
        // Monitorear ataques por teclado a través de EquipmentManager
        if (equipmentManager != null && equipmentManager.CurrentWeapon != null)
        {
            // Esta lógica detecta cuándo se está atacando sin modificar tu código
            // Se puede expandir según sea necesario
        }
    }

    void OnDestroy()
    {
        // Limpiar suscripciones
        if (swordDamageSystem != null)
        {
            swordDamageSystem.OnAttack -= OnSwordAttack;
        }
    }
}