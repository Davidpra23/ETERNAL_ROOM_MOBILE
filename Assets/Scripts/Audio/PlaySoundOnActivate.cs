using UnityEngine;

public class SoundOnActivate : MonoBehaviour
{
    [Header("SONIDO AL ACTIVARSE")]
    public AudioClip sonido;
    [Range(0, 1)]
    public float volumen = 1f;
    public bool reproducirAlActivar = true;
    public bool reproducirAlInicio = false;

    void Start()
    {
        if (reproducirAlInicio && sonido != null)
        {
            ReproducirSonido();
        }
    }

    void OnEnable()
    {
        if (reproducirAlActivar && sonido != null)
        {
            ReproducirSonido();
        }
    }

    public void ReproducirSonido()
    {
        // Crear GameObject temporal para el sonido
        GameObject sonidoTemp = new GameObject("Sonido Temporal");
        AudioSource audioSource = sonidoTemp.AddComponent<AudioSource>();
        audioSource.clip = sonido;
        audioSource.volume = volumen;
        audioSource.Play();

        // Destruir después de reproducir
        Destroy(sonidoTemp, sonido.length + 0.1f);
    }
}