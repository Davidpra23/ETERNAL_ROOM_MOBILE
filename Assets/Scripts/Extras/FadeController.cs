using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f;

    public enum FadeMode { FadeIn, FadeOut }
    public FadeMode initialFade = FadeMode.FadeOut;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (initialFade == FadeMode.FadeIn)
            FadeIn();
        else
            FadeOut();
    }

    public void FadeIn()
    {
        StartCoroutine(Fade(1));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(0));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = targetAlpha == 1;
        canvasGroup.blocksRaycasts = targetAlpha == 1;
    }
}
