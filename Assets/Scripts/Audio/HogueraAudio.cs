using UnityEngine;

public class HogueraAudio : MonoBehaviour
{
    [Header("CONFIGURACIÓN AUDIO 3D")]
    [SerializeField] private AudioClip sonidoHoguera;
    [Range(0f, 1f)]
    [SerializeField] private float volumen = 0.7f;
    [SerializeField] private bool loop = true;
    
    [Header("CONFIGURACIÓN DISTANCIA 3D")]
    [SerializeField] private float minDistance = 3f;   // Distancia mínima para volumen máximo
    [SerializeField] private float maxDistance = 15f;  // Distancia máxima donde se deja de escuchar

    private AudioSource audioSource;
    private Transform player;

    void Start()
    {
        ConfigurarAudioSource();
        BuscarJugador();
        
        // Reproducir sonido
        if (sonidoHoguera != null)
        {
            audioSource.Play();
        }
    }

    void ConfigurarAudioSource()
    {
        // Obtener o crear AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configurar propiedades básicas
        audioSource.clip = sonidoHoguera;
        audioSource.volume = volumen;
        audioSource.loop = loop;
        audioSource.playOnAwake = true;

        // Configurar audio 3D
        audioSource.spatialBlend = 1f;        // 100% 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        
        // Configuraciones adicionales para sonido ambiental
        audioSource.dopplerLevel = 0f;        // Sin efecto Doppler
        audioSource.spread = 180f;            // Sonido omnidireccional
    }

    void BuscarJugador()
    {
        // Buscar al jugador por tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("No se encontró objeto con tag 'Player'. El audio 3D funcionará igual.");
        }
    }

    void Update()
    {
        // Opcional: Actualizar volumen basado en distancia al jugador
        if (player != null)
        {
            float distancia = Vector3.Distance(transform.position, player.position);
            // Unity maneja automáticamente el volumen basado en la distancia con spatialBlend = 1
        }
    }

    // Método para pausar/reanudar sonido
    public void PausarSonido()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }

    public void ReanudarSonido()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    // Método para cambiar volumen dinámicamente
    public void CambiarVolumen(float nuevoVolumen)
    {
        volumen = Mathf.Clamp01(nuevoVolumen);
        if (audioSource != null)
        {
            audioSource.volume = volumen;
        }
    }

    // Dibujar gizmos para ver las distancias en el editor
    private void OnDrawGizmosSelected()
    {
        // Gizmo para distancia mínima (verde)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        // Gizmo para distancia máxima (rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}