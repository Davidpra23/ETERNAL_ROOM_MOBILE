using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias Visuales")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("Configuración")]
    [SerializeField] private bool showDebug = true;

    void Awake()
    {
        // Nada que auto-asignar: se usará EquipmentManager en tiempo de ejecución
    }

    void Start()
    {
        if (showDebug) 
        {
            Debug.Log($"Botón móvil inicializado. Plataforma: {Application.platform}");
            Debug.Log($"Botón activo: {gameObject.activeSelf}");
        }
    }

    // Cuando se presiona el botón táctil
    public void OnPointerDown(PointerEventData eventData)
    {
        var em = EquipmentManager.Instance;
        if (em == null)
        {
            if (showDebug) Debug.LogWarning("[MobileAttackButton] EquipmentManager.Instance es null");
        }
        else if (em.CurrentWeapon == null)
        {
            if (showDebug) Debug.LogWarning("[MobileAttackButton] No hay arma equipada");
        }
        else
        {
            var mode = em.CurrentWeapon.Mode;
            if (mode == WeaponSystem.AttackInputMode.TapOnly)
            {
                em.TriggerAttack();
                if (showDebug) Debug.Log("[MobileAttackButton] TAP attack");
            }
            else
            {
                em.StartAttackHold();
                if (showDebug) Debug.Log("[MobileAttackButton] Hold START");
            }
        }
        
        // Cambiar color visual
        if (buttonImage != null)
            buttonImage.color = pressedColor;
    }

    // Cuando se suelta el botón táctil
    public void OnPointerUp(PointerEventData eventData)
    {
        var em = EquipmentManager.Instance;
        if (em != null && em.CurrentWeapon != null)
        {
            var mode = em.CurrentWeapon.Mode;
            if (mode == WeaponSystem.AttackInputMode.HoldReleaseOnly ||
                mode == WeaponSystem.AttackInputMode.TapAndHoldCharged)
            {
                em.ReleaseAttackHold();
                if (showDebug) Debug.Log("[MobileAttackButton] Hold RELEASE");
            }
        }

        // Restaurar color visual
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }

    // Método público para forzar ataque desde otros scripts
    public void ForceAttack()
    {
        var em = EquipmentManager.Instance;
        if (em != null)
        {
            em.TriggerAttack();
            if (showDebug) Debug.Log("[MobileAttackButton] Ataque forzado");
        }
    }

    // Método para asegurar que el botón está siempre activo
    public void EnsureButtonActive()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            if (showDebug) Debug.Log("Botón reactivado");
        }
    }

    // Para testing en editor
    void Update()
    {
        // Testing con tecla T en editor
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T) && showDebug)
        {
            Debug.Log("Testing: Tecla T presionada");
            ForceAttack();
        }
        #endif
    }
}