using UnityEngine;
using UnityEngine.UI;

public class PlatformDetector : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject mobileControls;
    [SerializeField] private GameObject pcControlsHint;

    void Start()
    {
        bool isMobile = Application.isMobilePlatform;
        
        // Activar/desactivar controles móviles
        if (mobileControls != null)
        {
            mobileControls.SetActive(isMobile);
        }
        
        // Mostrar/ocultar hint para PC
        if (pcControlsHint != null)
        {
            pcControlsHint.SetActive(!isMobile);
        }

        Debug.Log($"Plataforma detectada: {(isMobile ? "Móvil" : "PC")}");
    }

    // Método para forzar plataforma (útil para testing)
    public void SetForceMobile(bool forceMobile)
    {
        if (mobileControls != null)
        {
            mobileControls.SetActive(forceMobile);
        }
        
        PlayerAttack attack = FindObjectOfType<PlayerAttack>();
        if (attack != null)
        {
            // Necesitarías añadir un método en PlayerAttack para forzar móvil
        }
    }
}