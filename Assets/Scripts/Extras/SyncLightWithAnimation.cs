using UnityEngine;


public class SyncLightWithAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] lightSprites; // light map sprites
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D light2D;
    [SerializeField] private SpriteRenderer mainSpriteRenderer;

    private int lastFrame = -1;

    void Update()
    {
        if (light2D == null || mainSpriteRenderer == null || lightSprites.Length == 0) return;

        Sprite currentFrame = mainSpriteRenderer.sprite;
        int index = System.Array.IndexOf(lightSprites, currentFrame);

        if (index >= 0 && index != lastFrame)
        {
            light2D.lightCookieSprite = lightSprites[index];
            lastFrame = index;
        }
    }
}
