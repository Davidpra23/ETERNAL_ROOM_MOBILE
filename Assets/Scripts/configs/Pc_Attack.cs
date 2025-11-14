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
    private bool pressed = false;
    private float pressStartTime = 0f;
    [SerializeField, Tooltip("Tiempo mínimo para considerar que es HOLD (seg)")]
    private float holdThreshold = 0.2f;

    private void Update()
    {
        var em = EquipmentManager.Instance;
        var weapon = em != null ? em.CurrentWeapon : null;
        var mode = weapon != null ? weapon.Mode : WeaponSystem.AttackInputMode.TapOnly;

        // --- INPUT DOWN ---
        if (KeyDown() || MouseDown())
        {
            pressStartTime = Time.time;
            pressed = true;

            if (mode == WeaponSystem.AttackInputMode.TapOnly)
            {
                // Ataque inmediato por tap
                if (em != null) em.TriggerAttack();
                if (showDebug) Debug.Log("[Pc_Attack] TAP attack");
            }
            else
            {
                // Iniciar hold para modos que lo usan (HoldReleaseOnly / TapAndHoldCharged)
                if (em != null)
                {
                    em.StartAttackHold();
                    holding = true;
                    if (showDebug) Debug.Log("[Pc_Attack] Hold START");
                }
            }
        }

        // --- INPUT UP ---
        if (KeyUp() || MouseUp())
        {
            float heldTime = Time.time - pressStartTime;

            if (mode == WeaponSystem.AttackInputMode.TapOnly)
            {
                // Nada extra en UP para TapOnly
            }
            else if (mode == WeaponSystem.AttackInputMode.HoldReleaseOnly)
            {
                // Siempre soltar hold -> el arma decide qué hacer en release
                if (em != null)
                {
                    em.ReleaseAttackHold();
                    if (showDebug) Debug.Log("[Pc_Attack] Hold RELEASE (HoldOnly)");
                }
                holding = false;
            }
            else // TapAndHoldCharged
            {
                if (em != null)
                {
                    // En este modo ya iniciamos hold en DOWN; el release dispara.
                    // El arma (p.ej., Bow) diferencia carga por tiempo interno.
                    em.ReleaseAttackHold();
                    if (showDebug) Debug.Log($"[Pc_Attack] Hold RELEASE (Charged), held={heldTime:0.00}s");
                }
                holding = false;
            }

            pressed = false;
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
