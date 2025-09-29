using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton; // 👈 Botón reiniciar

    [Header("Frames del panel")]
    [SerializeField] private GameObject[] frames; // frame1, frame2, frame3
    [SerializeField] private float frameDuration = 0.5f; // segundos por frame

    [Header("Formato del score")]
    [SerializeField] private string scorePrefix = "Score: ";

    private Coroutine frameLoopCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Panel oculto al inicio
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        SetAllFramesActive(false);

        // Vincular botón
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartRoom);
    }

    /// <summary>
    /// Muestra el panel de Game Over, actualiza el texto y arranca la animación de frames.
    /// </summary>
    public void Show()
    {
        int score = TryGetScore();

        if (finalScoreText != null)
            finalScoreText.text = $"{scorePrefix}{score}";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Iniciar animación de frames
        if (frameLoopCoroutine != null) StopCoroutine(frameLoopCoroutine);
        frameLoopCoroutine = StartCoroutine(FrameLoop());
    }

    private IEnumerator FrameLoop()
    {
        int index = 0;
        while (true)
        {
            SetAllFramesActive(false);

            if (frames != null && frames.Length > 0)
            {
                frames[index].SetActive(true);
                index = (index + 1) % frames.Length; // ciclo infinito
            }

            yield return new WaitForSeconds(frameDuration);
        }
    }

    private void SetAllFramesActive(bool active)
    {
        if (frames == null) return;
        foreach (var f in frames)
            if (f != null) f.SetActive(active);
    }

    private int TryGetScore()
    {
        // Si tienes ScoreManager con Instance y CurrentScore, lo usamos.
        var type = System.Type.GetType("ScoreManager");
        if (type != null)
        {
            var instProp = type.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var scoreProp = type.GetProperty("CurrentScore", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (instProp != null && scoreProp != null)
            {
                var inst = instProp.GetValue(null);
                if (inst != null)
                {
                    object val = scoreProp.GetValue(inst);
                    if (val is int i) return i;
                }
            }
        }
        return 0;
    }

    // 👇 NUEVO: Reinicia la room
    private void RestartRoom()
    {
        // Opcional: detener animaciones del panel antes de recargar
        if (frameLoopCoroutine != null)
        {
            StopCoroutine(frameLoopCoroutine);
            frameLoopCoroutine = null;
        }

        // Recargar la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
