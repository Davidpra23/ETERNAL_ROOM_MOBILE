using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

public class RunaPathMover : MonoBehaviour
{
    [Header("Activación automática")]
    [SerializeField] private float activationDelay = 1f;

    [Header("Path Settings")]
    [SerializeField] private Transform[] pathPoints;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Rotación mientras se mueve")]
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float totalRotationDegrees = 360f;

    [Header("Componentes a activar al llegar (opcional)")]
    [SerializeField] private List<Behaviour> componentsToEnable = new List<Behaviour>();

    [Header("Light 2D Inicial (Fade In)")]
    [SerializeField] private Light2D initialLight;
    [SerializeField] private float initialIntensity = 1f;
    [SerializeField] private float fadeInDuration = 2f;

    [Header("Light 2D al llegar")]
    [SerializeField] private Light2D arrivalLight;
    [SerializeField] private float intensityIncrease = 2f;
    [SerializeField] private float lightEffectDuration = 1f;
    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowMinIntensity = 0.5f;
    [SerializeField] private float glowMaxIntensity = 1.5f;

    private Quaternion initialRotation;
    private bool isMoving = false;
    private float totalPathLength;
    private Vector3 initialScale;
    private Coroutine glowCoroutine;

    private void Start()
    {
        initialScale = transform.localScale;
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(activationDelay);

        if (initialLight != null)
        {
            yield return StartCoroutine(FadeInInitialLight());
        }

        totalPathLength = CalculateTotalPathLength();
        yield return StartCoroutine(FollowPath());
    }

    private IEnumerator FadeInInitialLight()
    {
        float t = 0f;
        float startIntensity = 0f;
        initialLight.intensity = 0f;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Lerp(startIntensity, initialIntensity, t / fadeInDuration);
            initialLight.intensity = lerp;
            yield return null;
        }

        initialLight.intensity = initialIntensity;
    }

    private float CalculateTotalPathLength()
    {
        float length = Vector3.Distance(transform.position, pathPoints[0].position);

        for (int i = 0; i < pathPoints.Length - 1; i++)
            length += Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);

        return length;
    }

    private IEnumerator FollowPath()
    {
        if (pathPoints.Length < 1)
            yield break;

        isMoving = true;
        initialRotation = transform.rotation;

        Vector3[] fullPath = new Vector3[pathPoints.Length + 1];
        fullPath[0] = transform.position;
        for (int i = 0; i < pathPoints.Length; i++)
            fullPath[i + 1] = pathPoints[i].position;

        int currentSegment = 0;
        float traveled = 0f;

        while (currentSegment < fullPath.Length - 1)
        {
            Vector3 start = fullPath[currentSegment];
            Vector3 end = fullPath[currentSegment + 1];
            float segmentLength = Vector3.Distance(start, end);
            float segmentTraveled = 0f;

            while (segmentTraveled < segmentLength)
            {
                float step = moveSpeed * Time.deltaTime;
                segmentTraveled += step;
                traveled += step;

                float t = Mathf.Clamp01(segmentTraveled / segmentLength);
                transform.position = Vector3.Lerp(start, end, t);

                if (rotateWhileMoving)
                {
                    float rotationProgress = Mathf.Clamp01(traveled / totalPathLength);
                    float rotationAngle = totalRotationDegrees * rotationProgress;
                    transform.rotation = initialRotation * Quaternion.Euler(0, 0, rotationAngle);
                }

                float scaleProgress = Mathf.Clamp01(traveled / totalPathLength);
                transform.localScale = Vector3.Lerp(initialScale, Vector3.one, scaleProgress);

                yield return null;
            }

            currentSegment++;
        }

        transform.position = fullPath[^1];
        transform.rotation = initialRotation;
        transform.localScale = Vector3.one;
        isMoving = false;

        foreach (Behaviour behaviour in componentsToEnable)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        if (arrivalLight != null)
        {
            yield return StartCoroutine(AnimateArrivalLightPulse());
            glowCoroutine = StartCoroutine(GlowPulseLoop());
        }
    }

    private IEnumerator AnimateArrivalLightPulse()
    {
        float originalIntensity = arrivalLight.intensity;
        float targetIntensity = originalIntensity + intensityIncrease;
        float halfDuration = lightEffectDuration / 2f;
        float t = 0f;

        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Lerp(originalIntensity, targetIntensity, t / halfDuration);
            arrivalLight.intensity = lerp;
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Lerp(targetIntensity, originalIntensity, t / halfDuration);
            arrivalLight.intensity = lerp;
            yield return null;
        }

        arrivalLight.intensity = originalIntensity;
    }

    private IEnumerator GlowPulseLoop()
    {
        while (true)
        {
            float t = 0f;
            while (t < Mathf.PI * 2f)
            {
                t += Time.deltaTime * glowPulseSpeed;
                float pulse = Mathf.Lerp(glowMinIntensity, glowMaxIntensity, (Mathf.Sin(t) + 1f) / 2f);
                arrivalLight.intensity = pulse;
                yield return null;
            }
        }
    }
}
