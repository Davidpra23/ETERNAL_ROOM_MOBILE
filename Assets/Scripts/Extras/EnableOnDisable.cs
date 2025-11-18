using UnityEngine;

/// <summary>
/// Activa un componente (script) asignable cuando este GameObject se desactiva.
/// Útil para encadenar comportamientos al ocultar/desactivar objetos.
/// </summary>
public class EnableOnDisable : MonoBehaviour
{
    [Header("Componente a Activar")]
    [Tooltip("El componente (script) que se activará cuando este GameObject se desactive")]
    [SerializeField] private Behaviour componentToEnable;

    private void OnDisable()
    {
        if (componentToEnable != null)
        {
            componentToEnable.enabled = true;
        }
    }
}
