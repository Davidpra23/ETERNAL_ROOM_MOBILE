using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public CanvasGroup fadePanel;
    public float fadeDuration = 1f;

    public void ChangeSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // Load scene
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        float time = 0f;
        fadePanel.blocksRaycasts = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, time / fadeDuration);
            fadePanel.alpha = alpha;
            yield return null;
        }

        fadePanel.alpha = to;
    }

    private void Start()
    {
        // Optional: Fade in when entering the scene
        if (fadePanel != null)
        {
            fadePanel.alpha = 1f;
            StartCoroutine(Fade(1f, 0f));
        }
    }
}
