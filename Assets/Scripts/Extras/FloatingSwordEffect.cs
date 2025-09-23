using UnityEngine;

public class FloatingSwordEffect : MonoBehaviour
{
    [Header("Movement Settings")]
    public float floatAmplitude = 0.2f; // Altura máxima que flotará la espada
    public float floatSpeed = 1f;       // Velocidad del movimiento flotante
    
    private Vector3 startPosition;

    void Start()
    {
        // Guardar la posición inicial de la espada al activarse
        startPosition = transform.position; 
    }

    void Update()
    {
        // Aplica un movimiento senoidal para un efecto suave de flotación
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = startPosition + new Vector3(0, yOffset, 0);
    }
}