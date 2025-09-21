using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RunaPathBreaker : MonoBehaviour
{
    [Header("Activación automática")]
    [SerializeField] private float activationDelay = 1f;

    [Header("Path Settings")]
    [SerializeField] private Transform[] pathPoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float breakDelay = 1f;

    [Header("Rotation While Moving")]
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float totalRotationDegrees = 720f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string breakTrigger = "Break";

    [Header("Optional: Components to Disable Before Break")]
    [SerializeField] private List<Component> componentsToDisable = new List<Component>();

    private Quaternion initialRotation;
    private bool isMoving = false;
    private float totalPathLength;

    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(activationDelay);

        totalPathLength = CalculateTotalPathLength();
        yield return StartCoroutine(FollowPath());
    }

    private float CalculateTotalPathLength()
    {
        float length = Vector3.Distance(transform.position, pathPoints[0].position);

        for (int i = 0; i < pathPoints.Length - 1; i++)
            length += Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);

        return length;
    }

    IEnumerator FollowPath()
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

                yield return null;
            }

            currentSegment++;
        }

        // Final adjustments
        transform.position = fullPath[^1];
        transform.rotation = initialRotation;
        isMoving = false;

        // Disable components
        foreach (Component c in componentsToDisable)
        {
            if (c is Behaviour behaviour)
                behaviour.enabled = false;
            else if (c is Renderer renderer)
                renderer.enabled = false;
        }

        yield return new WaitForSeconds(breakDelay);

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger(breakTrigger);
            yield return StartCoroutine(WaitForAnimationToEnd());
        }

        Destroy(gameObject);
    }

    IEnumerator WaitForAnimationToEnd()
    {
        if (animator == null)
            yield break;

        // Esperar a que inicie la animación del trigger
        bool hasStarted = false;
        while (!hasStarted)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Break")) // Asegúrate de que el estado se llama exactamente "Break"
            {
                hasStarted = true;
            }
            yield return null;
        }

        // Esperar a que termine esa animación
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }
}