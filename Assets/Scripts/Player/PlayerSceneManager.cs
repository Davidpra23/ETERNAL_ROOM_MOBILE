using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSceneManager : MonoBehaviour
{
    private static PlayerSceneManager instance;
    
    // Datos para el teleport
    private GameObject playerToTeleport;
    private Vector2 targetPosition;
    private string targetScene;

    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Suscribirse al evento de carga de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void PrepareForSceneChange(GameObject playerObject, string sceneName, Vector2 position)
    {
        if (playerObject == null) return;
        
        // Guardar referencias para usar después de cargar la escena
        playerToTeleport = playerObject;
        targetScene = sceneName;
        targetPosition = position;
        
        // Solo marcar para que no se destruya, pero NO repositionar aún
        DontDestroyOnLoad(playerObject);
        
        Debug.Log($"Preparando teleport a {sceneName}. Repositionamiento después de carga.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Solo reposicionar si esta es la escena destino y tenemos un jugador
        if (playerToTeleport != null && scene.name == targetScene)
        {
            Debug.Log($"Repositionando jugador en nueva escena: {targetPosition}");
            
            // Repositionar el jugador en la NUEVA escena
            playerToTeleport.transform.position = targetPosition;
            
            // Limpiar referencias
            playerToTeleport = null;
            targetScene = null;
            
            Debug.Log("Repositionamiento completado");
        }
    }
}