using UnityEngine;
using System.Collections.Generic;

public class SlimeAudioManager : MonoBehaviour
{
    [Header("SONIDOS DE SLIME")]
    [SerializeField] private AudioClip sonidoAtaque;
    [SerializeField] private AudioClip sonidoMuerte;
    [SerializeField] private float volumen = 0.7f;
    
    [Header("CONTROL DE FRECUENCIA")]
    [SerializeField] private int slimesPorSonido = 3; // 1 sonido cada 3 slimes
    [SerializeField] private float tiempoMinEntreSonidos = 0.5f;

    private AudioSource audioSource;
    private int contadorAtaques = 0;
    private int contadorMuertes = 0;
    private float ultimoTiempoAtaque = 0f;
    private float ultimoTiempoMuerte = 0f;
    
    // Singleton para fácil acceso
    public static SlimeAudioManager Instance { get; private set; }

    void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ConfigurarAudioSource();
        Debug.Log("✅ SlimeAudioManager inicializado");
    }

    void ConfigurarAudioSource()
    {
        // Obtener o crear AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volumen;
    }

    // Asegurar que el AudioSource esté listo antes de usarlo
    private void AsegurarAudioSource()
    {
        if (audioSource == null)
        {
            ConfigurarAudioSource();
        }
    }

    // Método para que los slimes reporten ataques
    public void ReportarAtaque()
    {
        AsegurarAudioSource();
        contadorAtaques++;
        
        // Verificar frecuencia y tiempo mínimo
        if (contadorAtaques >= slimesPorSonido && 
            Time.time - ultimoTiempoAtaque >= tiempoMinEntreSonidos)
        {
            ReproducirSonidoAtaque();
            contadorAtaques = 0;
            ultimoTiempoAtaque = Time.time;
        }
    }

    // Método para que los slimes reporten muertes
    public void ReportarMuerte()
    {
        AsegurarAudioSource();
        contadorMuertes++;
        
        // Verificar frecuencia y tiempo mínimo
        if (contadorMuertes >= slimesPorSonido && 
            Time.time - ultimoTiempoMuerte >= tiempoMinEntreSonidos)
        {
            ReproducirSonidoMuerte();
            contadorMuertes = 0;
            ultimoTiempoMuerte = Time.time;
        }
    }

    private void ReproducirSonidoAtaque()
    {
        if (sonidoAtaque != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoAtaque);
            Debug.Log($"🔊 Sonido de ataque reproducido (contador: {contadorAtaques})");
        }
        else
        {
            Debug.LogWarning("❌ No se puede reproducir sonido de ataque: AudioSource o AudioClip nulo");
        }
    }

    private void ReproducirSonidoMuerte()
    {
        if (sonidoMuerte != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoMuerte);
            Debug.Log($"💀 Sonido de muerte reproducido (contador: {contadorMuertes})");
        }
        else
        {
            Debug.LogWarning("❌ No se puede reproducir sonido de muerte: AudioSource o AudioClip nulo");
        }
    }

    [ContextMenu("🔊 PROBAR SONIDO ATAQUE")]
    public void ProbarSonidoAtaque()
    {
        AsegurarAudioSource();
        Debug.Log("🔊 Probando sonido de ataque...");
        
        if (sonidoAtaque != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoAtaque);
            Debug.Log("✅ Sonido de ataque probado con éxito");
        }
        else
        {
            Debug.LogError("❌ No se puede probar sonido de ataque: " +
                          $"Sonido: {sonidoAtaque != null}, " +
                          $"AudioSource: {audioSource != null}");
        }
    }

    [ContextMenu("💀 PROBAR SONIDO MUERTE")]
    public void ProbarSonidoMuerte()
    {
        AsegurarAudioSource();
        Debug.Log("💀 Probando sonido de muerte...");
        
        if (sonidoMuerte != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoMuerte);
            Debug.Log("✅ Sonido de muerte probado con éxito");
        }
        else
        {
            Debug.LogError("❌ No se puede probar sonido de muerte: " +
                          $"Sonido: {sonidoMuerte != null}, " +
                          $"AudioSource: {audioSource != null}");
        }
    }

    [ContextMenu("🔄 REINICIAR CONTADORES")]
    public void ReiniciarContadores()
    {
        contadorAtaques = 0;
        contadorMuertes = 0;
        ultimoTiempoAtaque = 0f;
        ultimoTiempoMuerte = 0f;
        Debug.Log("🔄 Contadores reiniciados");
    }

    [ContextMenu("📊 VER ESTADO ACTUAL")]
    public void VerEstadoActual()
    {
        Debug.Log("=== ESTADO SLIME AUDIO MANAGER ===");
        Debug.Log($"AudioSource: {audioSource != null}");
        Debug.Log($"Sonido Ataque: {sonidoAtaque != null}");
        Debug.Log($"Sonido Muerte: {sonidoMuerte != null}");
        Debug.Log($"Contador Ataques: {contadorAtaques}");
        Debug.Log($"Contador Muertes: {contadorMuertes}");
        Debug.Log($"Último ataque: {ultimoTiempoAtaque}");
        Debug.Log($"Última muerte: {ultimoTiempoMuerte}");
    }
}