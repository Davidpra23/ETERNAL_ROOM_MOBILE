using UnityEngine;

public class HitEffectController : MonoBehaviour
{
    [Header("Configuración de Hit Effect")]
    [SerializeField] private float lifeTime = 0.5f;
    [SerializeField] private bool autoDestroy = true;

    [Header("Component References")]
    [SerializeField] private ParticleSystem hitParticles;

    void Start()
    {
        // Inicializar componentes
        if (hitParticles == null)
            hitParticles = GetComponent<ParticleSystem>();

        // Reproducir efecto
        PlayEffect();

        // Auto-destrucción
        if (autoDestroy)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void PlayEffect()
    {
        // Reproducir partículas
        if (hitParticles != null && !hitParticles.isPlaying)
        {
            hitParticles.Play();
        }
    }

    // Método para configurar la rotación del efecto
    public void SetEffectRotation(float zRotation)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    // Método para configurar basado en dirección de golpe
    public void SetHitDirection(bool hitFromRight)
    {
        float zRotation = hitFromRight ? 90f : -90f;
        transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
        
        // Opcional: ajustar partículas si es necesario
        if (hitParticles != null)
        {
            var velocityModule = hitParticles.velocityOverLifetime;
            if (velocityModule.enabled)
            {
                // Ajustar dirección de las partículas
                float xVelocity = hitFromRight ? -1f : 1f;
                velocityModule.x = new ParticleSystem.MinMaxCurve(xVelocity * 2f);
            }
        }
    }
}