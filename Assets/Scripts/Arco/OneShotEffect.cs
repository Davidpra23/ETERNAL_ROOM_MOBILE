using UnityEngine;
using System.Collections;


public class OneShotEffect : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Explosion";
    [SerializeField] private float autoDestroyDelay = 1f; // fallback

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[OneShotEffect] No Animator encontrado.");
            Destroy(gameObject);
            return;
        }

        animator.SetTrigger(triggerName);

        // Fallback: destruye luego de X segundos (útil si no tienes eventos)
        StartCoroutine(AutoDestroy(autoDestroyDelay));
    }

    private IEnumerator AutoDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // Alternativa si usas Animation Event directamente
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
