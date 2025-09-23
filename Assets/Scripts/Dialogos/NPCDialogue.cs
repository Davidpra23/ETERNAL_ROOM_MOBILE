using UnityEngine;
using TMPro;
using System.Collections;

public class NPCDialogue : MonoBehaviour
{
    [Header("Configuración de UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    
    [Header("Configuración de Diálogo")]
    [SerializeField] private string[] dialogueLines;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Header("Secuencia de Obtención de Objeto")]
    [SerializeField] private int specialDialogueLine = 5;
    [SerializeField] private GameObject itemObtainedPanel;
    [SerializeField] private CanvasGroup itemCanvasGroup;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform weaponParentTransform;
    
    [Header("Control Externo")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private MonoBehaviour scriptToEnable;

    // Estados
    private PlayerMovement playerMovement;
    private bool isPlayerInRange;
    private bool isDialogueActive;
    private bool hasDialogueCompleted;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private bool inSpecialSequence;

    // Cache de componentes TMP
    private TextMeshProUGUI interactionPromptTMP;
    private CanvasGroup cachedDialogueCanvasGroup;
    private CanvasGroup cachedItemCanvasGroup;
    
    // Pre-inicialización de TMP
    private bool isTMPPrepared;

    private void Awake()
    {
        // Cachear componentes
        cachedDialogueCanvasGroup = dialogueCanvasGroup;
        cachedItemCanvasGroup = itemCanvasGroup;
        
        // Pre-inicializar TMP del interaction prompt
        if (interactionPrompt != null)
        {
            interactionPromptTMP = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>();
            if (interactionPromptTMP != null)
            {
                // Pre-generar fuentes TMP mientras está desactivado
                interactionPromptTMP.ForceMeshUpdate();
                interactionPromptTMP.gameObject.SetActive(false);
            }
        }
        
        // Pre-inicializar TMP del diálogo
        if (dialogueText != null)
        {
            dialogueText.ForceMeshUpdate();
            dialogueText.gameObject.SetActive(false);
        }

        ResetDialogueState();
        isTMPPrepared = true;
    }

    private void ResetDialogueState()
    {
        isPlayerInRange = false;
        isDialogueActive = false;
        hasDialogueCompleted = false;
        currentLineIndex = 0;
        isTyping = false;
        inSpecialSequence = false;
    }

    private void Start()
    {
        // Configuración inicial mínima
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (itemObtainedPanel != null) itemObtainedPanel.SetActive(false);
        
        if (cachedDialogueCanvasGroup != null)
        {
            cachedDialogueCanvasGroup.alpha = 0f;
            cachedDialogueCanvasGroup.interactable = false;
            cachedDialogueCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        // Input processing optimizado
        if ((isPlayerInRange && !isDialogueActive && !inSpecialSequence) || 
            (isDialogueActive && !inSpecialSequence))
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        if (!Input.GetKeyDown(KeyCode.E) && !Input.GetMouseButtonDown(0)) return;

        if (isPlayerInRange && !isDialogueActive && !inSpecialSequence)
        {
            StartDialogue();
        }
        else if (isDialogueActive && !inSpecialSequence)
        {
            if (isTyping)
            {
                CompleteCurrentLine();
            }
            else
            {
                NextDialogueLine();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = true;
        
        if (playerMovement == null)
        {
            playerMovement = other.GetComponent<PlayerMovement>();
        }
        
        ShowInteractionPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = false;
        HideInteractionPrompt();
        
        if (isDialogueActive || inSpecialSequence)
        {
            EndDialogue();
        }
    }

    private void ShowInteractionPrompt()
    {
        if (interactionPrompt == null || !isTMPPrepared) return;
        
        // Activar solo si es necesario y pre-cargar TMP
        if (!interactionPrompt.activeInHierarchy)
        {
            if (interactionPromptTMP != null)
            {
                interactionPromptTMP.gameObject.SetActive(true);
            }
            interactionPrompt.SetActive(true);
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null && interactionPrompt.activeInHierarchy)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void StartDialogue()
    {
        isDialogueActive = true;
        currentLineIndex = hasDialogueCompleted && dialogueLines.Length > 0 ? dialogueLines.Length - 1 : 0;
        
        HideInteractionPrompt();
        
        if (hudCanvas != null) hudCanvas.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;
        
        StartCoroutine(StartDialogueSequence());
    }

    private IEnumerator StartDialogueSequence()
    {
        // Activar panel antes del fade
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            
            // Activar TMP del diálogo
            if (dialogueText != null && !dialogueText.gameObject.activeInHierarchy)
            {
                dialogueText.gameObject.SetActive(true);
            }
            
            // Fade in
            if (cachedDialogueCanvasGroup != null)
            {
                float timer = 0f;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    cachedDialogueCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                    yield return null;
                }
                cachedDialogueCanvasGroup.alpha = 1f;
                cachedDialogueCanvasGroup.interactable = true;
                cachedDialogueCanvasGroup.blocksRaycasts = true;
            }
        }
        
        DisplayDialogueLine(dialogueLines[currentLineIndex]);
    }

    private void DisplayDialogueLine(string line)
    {
        if (dialogueText == null) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        dialogueText.text = "";
        typingCoroutine = StartCoroutine(TypeTextOptimized(line));
    }

    private IEnumerator TypeTextOptimized(string line)
    {
        isTyping = true;
        
        // Usar StringBuilder interno de TMP para mejor rendimiento
        dialogueText.text = "";
        dialogueText.ForceMeshUpdate();
        
        int visibleChars = 0;
        int totalChars = line.Length;
        
        while (visibleChars < totalChars)
        {
            visibleChars++;
            dialogueText.maxVisibleCharacters = visibleChars;
            dialogueText.text = line;
            dialogueText.ForceMeshUpdate();
            
            yield return new WaitForSeconds(typingSpeed);
        }
        
        dialogueText.maxVisibleCharacters = totalChars;
        isTyping = false;
        
        if (!inSpecialSequence && !hasDialogueCompleted && currentLineIndex == specialDialogueLine - 1)
        {
            yield return StartCoroutine(SpecialItemSequence());
        }
    }

    private void CompleteCurrentLine()
    {
        if (!isTyping) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        if (dialogueText != null && currentLineIndex < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLineIndex];
            dialogueText.maxVisibleCharacters = dialogueLines[currentLineIndex].Length;
            isTyping = false;
            
            if (!inSpecialSequence && !hasDialogueCompleted && currentLineIndex == specialDialogueLine - 1)
            {
                StartCoroutine(SpecialItemSequence());
            }
        }
    }

    private void NextDialogueLine()
    {
        if (inSpecialSequence) return;

        currentLineIndex++;
        
        if (currentLineIndex >= dialogueLines.Length)
        {
            CompleteDialogue();
            return;
        }
        
        if (hasDialogueCompleted)
        {
            EndDialogue();
        }
        else
        {
            DisplayDialogueLine(dialogueLines[currentLineIndex]);
        }
    }

    private void CompleteDialogue()
    {
        if (!hasDialogueCompleted)
        {
            hasDialogueCompleted = true;
            if (scriptToEnable != null) scriptToEnable.enabled = true;
        }
        EndDialogue();
    }

    private IEnumerator SpecialItemSequence()
    {
        inSpecialSequence = true;
        isDialogueActive = false;
        
        if (itemObtainedPanel != null)
        {
            itemObtainedPanel.SetActive(true);
            yield return FadeCanvasGroup(cachedItemCanvasGroup, 0f, 1f, true);
        }
        
        yield return WaitForClick();
        
        if (itemObtainedPanel != null)
        {
            yield return FadeCanvasGroup(cachedItemCanvasGroup, 1f, 0f, false);
            itemObtainedPanel.SetActive(false);
        }
        
        if (weaponPrefab != null && weaponParentTransform != null)
        {
            Instantiate(weaponPrefab, weaponParentTransform);
        }
        
        inSpecialSequence = false;
        isDialogueActive = true;
        currentLineIndex++;
        
        if (currentLineIndex < dialogueLines.Length)
        {
            DisplayDialogueLine(dialogueLines[currentLineIndex]);
        }
        else
        {
            CompleteDialogue();
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, bool setInteractable)
    {
        if (group == null) yield break;
        
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, timer / fadeDuration);
            yield return null;
        }
        
        group.alpha = to;
        group.interactable = setInteractable;
        group.blocksRaycasts = setInteractable;
    }

    private IEnumerator WaitForClick()
    {
        while (Input.GetMouseButton(0)) yield return null;
        while (!Input.GetMouseButtonDown(0)) yield return null;
    }

    private void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        StartCoroutine(EndDialogueSequence());
    }

    private IEnumerator EndDialogueSequence()
    {
        // Fade out del diálogo
        if (cachedDialogueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(cachedDialogueCanvasGroup, 1f, 0f, false);
        }
        
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (hudCanvas != null) hudCanvas.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        
        isDialogueActive = false;
        inSpecialSequence = false;
        
        if (isPlayerInRange) ShowInteractionPrompt();
    }
}