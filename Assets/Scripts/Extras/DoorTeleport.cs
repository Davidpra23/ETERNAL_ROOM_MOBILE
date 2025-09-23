using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class DoorTeleport : MonoBehaviour
{
    [Header("Configuración de Teleport")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private Vector2 spawnPosition = Vector2.zero;
    
    [Header("UI de Confirmación")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private CanvasGroup confirmationCanvasGroup;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private float fadeDuration = 0.3f;
    
    [Header("Configuración de Jugador")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("UI de Interacción")]
    [SerializeField] private GameObject interactionPrompt;
    
    // Referencias
    private GameObject player;
    private PlayerSceneManager playerSceneManager;
    private bool isPlayerInRange;
    private bool isPanelActive;
    private bool isTeleporting;
    private Coroutine panelCoroutine;

    private void Awake()
    {
        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.RemoveListener(OnNoClicked);
        playerSceneManager = FindObjectOfType<PlayerSceneManager>();
    }

    private void Start()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        
        if (confirmationCanvasGroup != null)
        {
            confirmationCanvasGroup.alpha = 0f;
            confirmationCanvasGroup.interactable = false;
            confirmationCanvasGroup.blocksRaycasts = false;
        }

        // ✅ REMOVIDO: No sobreescribir el texto del confirmationText
        // El texto mantendrá el valor que tenga configurado en el Inspector
        // if (confirmationText != null && !string.IsNullOrEmpty(targetSceneName))
        // {
        //     confirmationText.text = $"¿Quieres ir a {targetSceneName}?";
        // }
    }

    private void Update()
    {
        if (isPlayerInRange && !isPanelActive && !isTeleporting)
        {
            if (interactionPrompt != null && !interactionPrompt.activeInHierarchy)
            {
                interactionPrompt.SetActive(true);
            }
            
            if (!isPanelActive)
            {
                ShowConfirmationPanel();
            }
        }
        else if ((!isPlayerInRange || isTeleporting) && interactionPrompt != null && interactionPrompt.activeInHierarchy)
        {
            interactionPrompt.SetActive(false);
        }

        if (isPanelActive && Input.GetKeyDown(KeyCode.Escape))
        {
            OnNoClicked();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && other.GetComponent<Rigidbody2D>() != null)
        {
            isPlayerInRange = true;
            player = other.gameObject;
            Debug.Log("Jugador detectado en la puerta");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = false;
            player = null;
            
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            
            if (isPanelActive)
            {
                Debug.Log("Jugador se alejó, cerrando panel");
                HideConfirmationPanel();
            }
        }
    }

    private void ShowConfirmationPanel()
    {
        if (confirmationPanel == null || isPanelActive || isTeleporting) return;

        isPanelActive = true;
        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        confirmationPanel.SetActive(true);
        
        if (panelCoroutine != null)
        {
            StopCoroutine(panelCoroutine);
        }
        panelCoroutine = StartCoroutine(FadePanel(0f, 1f, true));
    }

    private void HideConfirmationPanel()
    {
        if (confirmationPanel == null || !isPanelActive || isTeleporting) return;

        if (panelCoroutine != null)
        {
            StopCoroutine(panelCoroutine);
        }
        panelCoroutine = StartCoroutine(HidePanelSequence());
    }

    private IEnumerator HidePanelSequence()
    {
        yield return StartCoroutine(FadePanel(1f, 0f, false));
        
        confirmationPanel.SetActive(false);
        isPanelActive = false;
        
        if (isPlayerInRange && interactionPrompt != null && !isTeleporting)
        {
            interactionPrompt.SetActive(true);
        }
    }

    private IEnumerator FadePanel(float from, float to, bool setInteractable)
    {
        if (confirmationCanvasGroup == null) yield break;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            confirmationCanvasGroup.alpha = Mathf.Lerp(from, to, timer / fadeDuration);
            yield return null;
        }

        confirmationCanvasGroup.alpha = to;
        confirmationCanvasGroup.interactable = setInteractable;
        confirmationCanvasGroup.blocksRaycasts = setInteractable;
    }

    private void OnYesClicked()
    {
        StartCoroutine(TeleportToScene());
    }

    private void OnNoClicked()
    {
        HideConfirmationPanel();
    }

    private IEnumerator TeleportToScene()
    {
        if (player == null || string.IsNullOrEmpty(targetSceneName) || isTeleporting) yield break;

        isTeleporting = true;
        
        // Ocultar UI
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        
        // Solo marcar el jugador para que no se destruya, PERO NO repositionarlo aún
        if (playerSceneManager != null)
        {
            playerSceneManager.PrepareForSceneChange(player, targetSceneName, spawnPosition);
        }
        else
        {
            // Fallback sin PlayerSceneManager
            DontDestroyOnLoad(player);
        }
        
        // Cargar escena asíncronamente
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;
        
        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
        
        // Esperar a que la nueva escena esté completamente cargada
        yield return new WaitForEndOfFrame();
        
        // Ahora el repositionamiento se hará a través del evento OnSceneLoaded en PlayerSceneManager
        Debug.Log("Teleport completado");
        
        // Limpiar estado
        isTeleporting = false;
        isPanelActive = false;
        isPlayerInRange = false;
    }

    private void OnDrawGizmos()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.color = isPlayerInRange ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, collider.bounds.size);
        }
    }

    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        // ✅ REMOVIDO: No cambiar el texto al cambiar la escena destino
        // if (confirmationText != null) confirmationText.text = $"¿Quieres ir a {sceneName}?";
    }

    public void SetSpawnPosition(Vector2 newPosition)
    {
        spawnPosition = newPosition;
    }

    // ✅ NUEVO MÉTODO: Para cambiar el texto manualmente si es necesario
    public void SetConfirmationText(string newText)
    {
        if (confirmationText != null)
        {
            confirmationText.text = newText;
        }
    }

    // ✅ NUEVO MÉTODO: Para restaurar el texto original del Inspector
    public void ResetConfirmationText()
    {
        // Esto no restablecerá el texto original del Inspector,
        // pero puedes guardar una referencia al texto original si lo necesitas
    }

    private void OnDestroy()
    {
        if (panelCoroutine != null) StopCoroutine(panelCoroutine);
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYesClicked);
        if (noButton != null) noButton.onClick.RemoveListener(OnNoClicked);
    }
}