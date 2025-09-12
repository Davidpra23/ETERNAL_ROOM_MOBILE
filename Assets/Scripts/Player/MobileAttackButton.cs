using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias Visuales")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.8f);
    
    [Header("Referencia al SwordDamageSystem")]
    [SerializeField] private SwordDamageSystem swordDamageSystem;

    [Header("Configuración")]
    [SerializeField] private bool showDebug = true;

    void Awake()
    {
        // Buscar SwordDamageSystem automáticamente si no está asignado
        if (swordDamageSystem == null)
        {
            swordDamageSystem = FindObjectOfType<SwordDamageSystem>();
            if (swordDamageSystem != null && showDebug)
            {
                Debug.Log("SwordDamageSystem encontrado para botón móvil");
            }
        }
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
        if (swordDamageSystem != null)
        {
            swordDamageSystem.TryAttack();
            if (showDebug) Debug.Log("Botón móvil presionado - Ataque ejecutado");
        }
        else
        {
            if (showDebug) Debug.LogWarning("SwordDamageSystem no asignado al botón");
        }
        
        // Cambiar color visual
        if (buttonImage != null)
            buttonImage.color = pressedColor;
    }

    // Cuando se suelta el botón táctil
    public void OnPointerUp(PointerEventData eventData)
    {
        // Restaurar color visual
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }

    // Método público para forzar ataque desde otros scripts
    public void ForceAttack()
    {
        if (swordDamageSystem != null)
        {
            swordDamageSystem.TryAttack();
            if (showDebug) Debug.Log("Ataque forzado desde código");
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