using UnityEngine;

public class SonidoPanelSimple : MonoBehaviour
{
    public AudioClip sonidoAlActivar;
    public GameObject panelOleadas;
    
    private AudioSource audioSrc;
    private bool estabaActivo = false;

    void Start()
    {
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
            audioSrc = gameObject.AddComponent<AudioSource>();
        
        if (panelOleadas != null)
            estabaActivo = panelOleadas.activeSelf;
    }

    void Update()
    {
        if (panelOleadas == null) return;
        
        bool estaActivo = panelOleadas.activeSelf;
        
        // Solo reproducir cuando se ACTIVA (no cuando ya está activo)
        if (!estabaActivo && estaActivo)
        {
            if (sonidoAlActivar != null)
                audioSrc.PlayOneShot(sonidoAlActivar);
        }
        
        estabaActivo = estaActivo;
    }
}