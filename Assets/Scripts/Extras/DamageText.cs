using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("Movement & Lifetime")]
    public float lifetime = 1.2f;
    public Vector3 floatDirection = new Vector3(0, 2f, 0);
    public float floatSpeed = 1.5f;

    [Header("Animation Curves")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1.2f, 1, 0.8f);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private TextMeshProUGUI text;
    private float timer;
    private Color startColor;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        startColor = text.color;
    }

    public void Setup(float damage, Color color, bool isCritical = false)
    {
        text.text = damage.ToString("0");
        text.color = color;
        if (isCritical)
        {
            text.fontSize *= 1.3f;
            text.text = $"<b>{damage}</b>";
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        transform.position += floatDirection * floatSpeed * Time.deltaTime;
        transform.localScale = Vector3.one * scaleCurve.Evaluate(t);
        Color c = startColor;
        c.a = alphaCurve.Evaluate(t);
        text.color = c;

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
