using UnityEngine;

public class SonidoAlActivarPanel : MonoBehaviour
{
    [Header("CONFIGURACIÓN SONIDO")]
    public AudioClip sonidoActivacion;
    public float volumen = 1f;
    
    [Header("REFERENCIA DEL PANEL")]
    public GameObject panelOleadas;

    private bool panelEstadoAnterior = false;
    private bool yaReproducido = false;

    void Start()
    {
        Debug.Log("=== SONIDO AL ACTIVAR PANEL - INICIADO ===");
        
        if (panelOleadas == null)
        {
            Debug.LogError("❌ No hay panel asignado!");
            return;
        }

        panelEstadoAnterior = panelOleadas.activeInHierarchy;
        Debug.Log($"Estado inicial del panel: {panelEstadoAnterior}");
    }

    void Update()
    {
        if (panelOleadas == null) return;

        bool panelEstadoActual = panelOleadas.activeInHierarchy;

        // Detectar cuando el panel se ACTIVA (de false → true)
        if (!panelEstadoAnterior && panelEstadoActual && !yaReproducido)
        {
            Debug.Log("🎵 PANEL ACTIVADO - Reproduciendo sonido...");
            ReproducirSonidoUnaVez();
        }

        // Resetear el flag cuando el panel se desactiva
        if (!panelEstadoActual)
        {
            yaReproducido = false;
        }

        panelEstadoAnterior = panelEstadoActual;
    }

    void ReproducirSonidoUnaVez()
    {
        if (sonidoActivacion == null)
        {
            Debug.LogError("❌ No hay sonido asignado!");
            return;
        }

        // MÉTODO INFALIBLE: PlayClipAtPoint
        AudioSource.PlayClipAtPoint(sonidoActivacion, GetPosicionAudio(), volumen);
        yaReproducido = true;
        
        Debug.Log($"✅ SONIDO REPRODUCIDO: {sonidoActivacion.name}");
        Debug.Log($"📊 Volumen: {volumen}");
        Debug.Log($"🎯 Posición: {GetPosicionAudio()}");
    }

    Vector3 GetPosicionAudio()
    {
        // Buscar la cámara principal para la posición del audio
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform.position;
        }
        
        // Si no hay cámara principal, usar la posición del objeto
        return transform.position;
    }

    [ContextMenu("🔊 PROBAR SONIDO AHORA")]
    public void ProbarSonidoInmediato()
    {
        Debug.Log("🔊 PROBANDO SONIDO INMEDIATAMENTE...");
        
        if (sonidoActivacion != null)
        {
            AudioSource.PlayClipAtPoint(sonidoActivacion, GetPosicionAudio(), volumen);
            Debug.Log("✅ SONIDO PROBADO CON ÉXITO");
        }
        else
        {
            Debug.LogError("❌ No hay sonido asignado para probar");
        }
    }

    [ContextMenu("🔄 REINICIAR CONTADOR")]
    public void ReiniciarContador()
    {
        yaReproducido = false;
        Debug.Log("🔄 Contador de reproducción reiniciado");
    }

    [ContextMenu("📋 VER ESTADO ACTUAL")]
    public void VerEstadoActual()
    {
        Debug.Log("=== ESTADO ACTUAL ===");
        Debug.Log($"Panel activo: {panelOleadas?.activeInHierarchy ?? false}");
        Debug.Log($"Ya reproducido: {yaReproducido}");
        Debug.Log($"Sonido asignado: {sonidoActivacion != null}");
        Debug.Log($"AudioListener activo: {AudioListener.pause == false}");
        
        AudioListener listener = FindObjectOfType<AudioListener>();
        Debug.Log($"AudioListener encontrado: {listener != null}");
    }
}