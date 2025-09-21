using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraSizeTween : MonoBehaviour
{
    [Header("Tamaño de cámara")]
    [SerializeField] private float initialSize = 5f;
    [SerializeField] private float targetSize = 3f;

    [Header("Tiempos")]
    [SerializeField] private float activationDelay = 0f;
    [SerializeField] private float zoomDuration = 1f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = initialSize;

        StartCoroutine(ZoomCoroutine());
    }

    private IEnumerator ZoomCoroutine()
    {
        yield return new WaitForSeconds(activationDelay);

        float elapsed = 0f;
        float startSize = cam.orthographicSize;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomDuration);
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }

        cam.orthographicSize = targetSize;
    }
}
