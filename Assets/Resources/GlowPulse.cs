using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class GlowPulse : MonoBehaviour
{
    [Header("Luces para hacer pulsar")]
    [SerializeField] private List<Light2D> glowLights = new List<Light2D>();

    [Header("Parámetros del pulso")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 0.7f;

    [Header("Delay antes de iniciar el fade/pulso")]
    [SerializeField] private float delayBeforePulse = 0.5f;

    [Header("Primer desvanecimiento (fade-in)")]
    [SerializeField] private bool enableInitialFadeIn = true;
    [SerializeField] private float initialFadeDuration = 1f;

    private enum State { Waiting, FadingIn, Pulsing }
    private State currentState = State.Waiting;

    private float timer = 0f;

    void Start()
    {
        SetIntensity(minIntensity);
        currentState = State.Waiting;
    }

    void Update()
    {
        timer += Time.deltaTime;

        switch (currentState)
        {
            case State.Waiting:
                if (timer >= delayBeforePulse)
                {
                    timer = 0f;
                    currentState = enableInitialFadeIn ? State.FadingIn : State.Pulsing;
                }
                break;

            case State.FadingIn:
                float fadeT = Mathf.Clamp01(timer / initialFadeDuration);
                float fadeIntensity = Mathf.Lerp(minIntensity, maxIntensity, fadeT);
                SetIntensity(fadeIntensity);

                if (fadeT >= 1f)
                {
                    timer = 0f;
                    currentState = State.Pulsing;
                }
                break;

            case State.Pulsing:
                float wave = (Mathf.Sin(timer * pulseSpeed - Mathf.PI / 2f) + 1f) / 2f;
                float pulseIntensity = Mathf.Lerp(minIntensity, maxIntensity, wave);
                SetIntensity(pulseIntensity);
                break;
        }
    }

    private void SetIntensity(float value)
    {
        foreach (var light in glowLights)
        {
            if (light != null)
                light.intensity = value;
        }
    }
}
