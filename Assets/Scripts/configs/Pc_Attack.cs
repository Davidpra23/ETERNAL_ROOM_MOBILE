// Pc_Attack.cs (Hold-to-charge compatible)
using UnityEngine;

public class Pc_Attack : MonoBehaviour
{
    [Header("Configuración de Input")]
    [SerializeField] private bool useKey = true;
    [SerializeField] private KeyCode attackKey = KeyCode.J;

    [SerializeField] private bool useMouseClick = true;
    [Tooltip("0 = Click izquierdo, 1 = Derecho, 2 = Medio")]
    [SerializeField] private int mouseButton = 0;

    [Header("Opciones")]
    [SerializeField] private bool showDebug = true;
    [Tooltip("Cancelar la carga si este objeto se desactiva o pierde el foco?")]
    [SerializeField] private bool cancelOnDisable = true;

    private bool holding = false;

    private void Update()
    {
        // --- START HOLD ---
        if (!holding && (KeyDown() || MouseDown()))
        {
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.StartAttackHold();
                holding = true;

                if (showDebug) Debug.Log("[Pc_Attack] Hold START");
            }
            else if (showDebug)
            {
                Debug.LogWarning("[Pc_Attack] EquipmentManager no encontrado.");
            }
        }

        // --- RELEASE HOLD ---
        if (holding && (KeyUp() || MouseUp()))
        {
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.ReleaseAttackHold();
                if (showDebug) Debug.Log("[Pc_Attack] Hold RELEASE");
            }
            holding = false;
        }

        // (Opcional) si quieres permitir cancelar con tecla ESC:
        // if (holding && Input.GetKeyDown(KeyCode.Escape)) CancelHold();
    }

    private bool KeyDown() => useKey && Input.GetKeyDown(attackKey);
    private bool KeyUp()   => useKey && Input.GetKeyUp(attackKey);

    private bool MouseDown() => useMouseClick && Input.GetMouseButtonDown(mouseButton);
    private bool MouseUp()   => useMouseClick && Input.GetMouseButtonUp(mouseButton);

    private void CancelHold()
    {
        if (!holding) return;
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.CancelAttackHold();
            if (showDebug) Debug.Log("[Pc_Attack] Hold CANCEL");
        }
        holding = false;
    }

    private void OnDisable()
    {
        if (cancelOnDisable) CancelHold();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && cancelOnDisable) CancelHold();
    }
}
