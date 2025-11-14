using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public int CurrentScore => currentScore;

    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText; // Texto principal del score
    [SerializeField] private TMP_Text shopScoreText; // Texto del score en la tienda
    private int currentScore = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateScoreText();
    }

    public void AddScore(int value)
    {
        currentScore += value;
        if (currentScore < 0) currentScore = 0;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = currentScore.ToString();
        
        if (shopScoreText != null)
            shopScoreText.text = currentScore.ToString();
    }
}
