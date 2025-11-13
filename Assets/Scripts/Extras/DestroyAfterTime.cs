using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject objectToDestroy; // Objeto asignable
    [SerializeField] private float lifetime = 2f;       // Tiempo antes de destruir

    private void Start()
    {
        // Si no se asignó nada, destruir este mismo GameObject
        if (objectToDestroy == null)
            objectToDestroy = gameObject;

        Destroy(objectToDestroy, lifetime);
    }
}
