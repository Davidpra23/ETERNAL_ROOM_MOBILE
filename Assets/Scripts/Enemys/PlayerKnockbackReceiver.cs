using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerKnockbackReceiver : MonoBehaviour
{
    [Header("Opcional")]
    [Tooltip("Si se asigna, se deshabilita temporalmente mientras dura el knockback.")]
    [SerializeField] private MonoBehaviour playerMovement; // tu script de movimiento
    [Tooltip("Multiplicador global por si quieres retocar desde el player.")]
    [SerializeField] private float globalForceMultiplier = 1f;
    [Tooltip("Fricción aérea durante el knockback (0 = sin drag extra).")]
    [SerializeField] private float knockbackDrag = 0f;

    private Rigidbody2D rb;
    private bool isKnockbackActive;
    private float originalDrag;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalDrag = rb.linearDamping;
    }

    /// <summary>
    /// Aplica un empuje al jugador en la dirección dada, con fuerza y duración.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(direction, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        isKnockbackActive = true;

        // Deshabilitar control opcionalmente
        if (playerMovement != null) playerMovement.enabled = false;

        // Drag temporal
        if (knockbackDrag > 0f) rb.linearDamping = knockbackDrag;

        // Reset pequeña velocidad hacia atrás para que el impulso se note
        // (no hacemos zero total para no romper física si está saltando)
        rb.linearVelocity = Vector2.zero;

        // Impulso
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        rb.AddForce(dir * force * globalForceMultiplier, ForceMode2D.Impulse);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Restaurar
        rb.linearDamping = originalDrag;
        if (playerMovement != null) playerMovement.enabled = true;
        isKnockbackActive = false;
    }
}
