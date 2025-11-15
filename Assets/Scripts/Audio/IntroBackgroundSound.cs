using UnityEngine;

public class IntroBackgroundSound : MonoBehaviour
{
    [Header("SONIDO DE FONDO INTRO")]
    [SerializeField] private AudioClip sonidoFondoIntro;
    [SerializeField] private float volumen = 0.5f;

    private AudioSource audioSource;

    void Start()
    {
        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = sonidoFondoIntro;
        audioSource.loop = false; // 🔥 IMPORTANTE: NO repetir
        audioSource.volume = volumen;
        audioSource.playOnAwake = false;

        // Reproducir sonido de fondo una sola vez
        if (sonidoFondoIntro != null)
        {
            audioSource.Play();
            Debug.Log("🔊 Sonido de fondo de intro iniciado (una sola vez)");
        }
    }

    // Método opcional para detener manualmente si es necesario
    public void DetenerSonidoFondo()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("🔇 Sonido de fondo de intro detenido manualmente");
        }
    }

    [ContextMenu("🔊 PROBAR SONIDO FONDO")]
    public void ProbarSonidoFondo()
    {
        if (sonidoFondoIntro != null && audioSource != null)
        {
            audioSource.Play();
        }
    }
}