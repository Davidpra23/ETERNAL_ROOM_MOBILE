using UnityEngine;

/// <summary>
/// Script simple para activar o desactivar componentes (scripts) en un GameObject.
/// Útil para controlar comportamientos desde el Inspector o eventos.
/// </summary>
public class ComponentToggler : MonoBehaviour
{
    [Header("Componente a Controlar")]
    [Tooltip("El componente (script) que se activará/desactivará")]
    [SerializeField] private Behaviour targetComponent;

    [Header("Estado Inicial")]
    [Tooltip("¿Activar el componente al iniciar?")]
    [SerializeField] private bool activarAlIniciar = true;

    private void Start()
    {
        if (targetComponent != null)
            targetComponent.enabled = activarAlIniciar;
    }

    /// <summary>
    /// Activa el componente
    /// </summary>
    public void EnableComponent()
    {
        if (targetComponent != null)
            targetComponent.enabled = true;
    }

    /// <summary>
    /// Desactiva el componente
    /// </summary>
    public void DisableComponent()
    {
        if (targetComponent != null)
            targetComponent.enabled = false;
    }

    /// <summary>
    /// Alterna el estado del componente (activado ↔ desactivado)
    /// </summary>
    public void ToggleComponent()
    {
        if (targetComponent != null)
            targetComponent.enabled = !targetComponent.enabled;
    }

    /// <summary>
    /// Establece el estado del componente
    /// </summary>
    /// <param name="enabled">True para activar, False para desactivar</param>
    public void SetComponentState(bool enabled)
    {
        if (targetComponent != null)
            targetComponent.enabled = enabled;
    }
}
