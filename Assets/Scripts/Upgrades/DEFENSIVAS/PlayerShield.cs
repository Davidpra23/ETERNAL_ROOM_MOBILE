using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    private bool shieldActive = false;
    private PlayerHealth health;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.OnDamageTaken.AddListener(OnDamageTaken);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamageTaken.RemoveListener(OnDamageTaken);
        }
    }

    public void ActivateShield()
    {
        shieldActive = true;
        // Aquí puedes agregar animación, FX o sonido de activación de escudo
    }

    private void OnDamageTaken()
    {
        if (shieldActive)
        {
            // Cancela el daño y consume el escudo
            shieldActive = false;
            health.Heal(1); // Revertir el 1 de daño recibido (hack limpio)
        }
    }

    public bool IsShieldActive() => shieldActive;
}
