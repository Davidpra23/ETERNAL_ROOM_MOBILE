// Pc_Attack.cs (Modificado)
using UnityEngine;

public class Pc_Attack : MonoBehaviour
{
    [Header("Configuración de Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private bool useMouseClick = true;
    [SerializeField] private int mouseButton = 0;
    
    [Header("Opciones")]
    [SerializeField] private bool showDebug = true;

    // No necesitamos más la referencia a SwordDamageSystem aquí.

    void Update()
    {
        if (ShouldAttack())
        {
            Attack();
        }
    }

    private bool ShouldAttack()
    {
        bool keyInput = Input.GetKeyDown(attackKey);
        bool mouseInput = useMouseClick && Input.GetMouseButtonDown(mouseButton);
        return keyInput || mouseInput;
    }

    private void Attack()
    {
        // En lugar de llamar directamente a la espada, le pedimos al manager que lo haga.
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.TriggerAttack();
            
            if (showDebug)
            {
                Debug.Log("Señal de ataque enviada al EquipmentManager.");
            }
        }
        else if (showDebug)
        {
            Debug.LogWarning("EquipmentManager no encontrado en la escena.");
        }
    }
}