using UnityEngine;

public class PlayerSpawnController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string deathAnimName = "Death"; // la animación original de muerte
    [SerializeField] private float animDuration = 1.5f;
    [SerializeField] private PlayerMovement playerMovement;

    private void Start()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (animator != null)
        {
            animator.Play(deathAnimName, 0, 1f); // Inicia en el último frame
            animator.SetFloat("AnimSpeed", -1f); // Reproduce en reversa
        }

        Invoke(nameof(EnablePlayer), animDuration);
    }

    private void EnablePlayer()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;

        animator.SetFloat("AnimSpeed", 1f); // Restaurar velocidad normal
    }
}
