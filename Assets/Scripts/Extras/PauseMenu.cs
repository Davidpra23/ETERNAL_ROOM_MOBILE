using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;

    private void Start()
    {
        // Asegurar que el menú esté oculto al inicio
        pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);

        // Asignar listeners
        if (continueButton) continueButton.onClick.AddListener(ResumeGame);
        if (optionsButton) optionsButton.onClick.AddListener(OpenOptions);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        // Presionar Escape para pausar/despausar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Detiene la simulación
        pausePanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Reanuda el juego
        pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        AudioListener.pause = false;
    }

    private void OpenOptions()
    {
        if (!optionsPanel) return;
        optionsPanel.SetActive(true);
        pausePanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("[PauseMenu] Cerrando el juego...");
        Application.Quit();
    }
}
