using UnityEngine;
using System.Collections;

/// <summary>
/// Script que controla el ataque de impacto telegráfico del jefe.
/// El prefab debe instanciarse sobre la posición del jugador (BossController lo hace).
/// Comportamiento:
///  - Muestra opcionalmente una telegraph en la posición objetivo durante `telegraphDuration`.
///  - Dispara opcionalmente el trigger "Impact" en el Animator del prefab para reproducir la animación.
///  - Tras un pequeño delay (`impactDelayAfterTrigger`) aplica daño en un radio comprobando `IHealth` o `PlayerHealth`.
///  - Reproduce un VFX de impacto en una altura configurable `impactVfxYOffset`.
/// </summary>
public class FallingImpact : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Tiempo que la telegraph permanece visible antes del impacto (permite que el jugador esquive)")]
    public float telegraphDuration = 0.8f;
    [Tooltip("Pequeña espera tras disparar la animación de impacto antes de aplicar daño")]
    public float impactDelayAfterTrigger = 0.12f;

    [Header("Impact")]
    [Tooltip("Radio del área afectada" )]
    public float radius = 1.5f;
    [Tooltip("Daño aplicado a cada objetivo dentro del radio")]
    public float damage = 20f;
    [Tooltip("Layers a afectar por el impacto")]
    public LayerMask damageLayer = ~0; // por defecto: todo

    [Header("Visuals")]
    [Tooltip("Prefab opcional que dibuja la telegraph (ej. circulo en el suelo)")]
    public GameObject telegraphPrefab;
    [Tooltip("Prefab opcional del VFX que se reproducirá en el impacto")]
    public GameObject impactVfxPrefab;
    [Tooltip("Offset en X a aplicar al VFX de impacto respecto a la posición objetivo")]
    public float impactVfxXOffset = 0f;
    [Tooltip("Offset en Y a aplicar al VFX de impacto respecto a la posición objetivo")]
    public float impactVfxYOffset = 0f;

    // Posición objetivo donde se aplica el impacto (se asume que el prefab fue instanciado en esa posición)
    private Vector3 targetPosition;
    // Posición exacta del VFX/impacto (targetPosition + impactVfxYOffset)
    private Vector3 impactPosition;
    private GameObject telegraphInstance;

    IEnumerator Start()
    {
        // Guardar la posición objetivo (el prefab debe instanciarse en la posición del jugador o ligeramente arriba)
        targetPosition = transform.position;

        // Calcular la posición del impacto (target + offset en X e Y)
        impactPosition = new Vector3(targetPosition.x + impactVfxXOffset, targetPosition.y + impactVfxYOffset, targetPosition.z);

            // Instanciar la telegraph en la posición del VFX si está asignada
            if (telegraphPrefab != null)
            {
                telegraphInstance = Instantiate(telegraphPrefab, impactPosition, Quaternion.identity);

                // Intentar ajustar la escala en función del tamaño del sprite (unidades del sprite)
                // Esto corrige discrepancias cuando el sprite tiene PPU distinto (ej. 256 px, PPU 100 -> 2.56 units)
                var sr = telegraphInstance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float spriteWorldWidth = sr.sprite.bounds.size.x; // ancho en unidades del sprite
                    if (spriteWorldWidth > 0f)
                    {
                        // escala necesaria para que el diámetro (radius*2) coincida con el ancho del sprite en unidades
                        float neededScale = (radius * 2f) / spriteWorldWidth;
                        telegraphInstance.transform.localScale = Vector3.one * neededScale;
                    }
                    else
                    {
                        telegraphInstance.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
                    }
                }
                else
                {
                    // Fallback: escala uniforme basada en unidades
                    telegraphInstance.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
                }
            }

        // Esperar el tiempo de telegraph para dar oportunidad al jugador de esquivar
        yield return new WaitForSeconds(telegraphDuration);

        // Si el prefab tiene Animator, activar el trigger "Impact" para reproducir la animación
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Impact");
        }

        // Esperar un pequeño delay para sincronizar animación y aplicación de daño
        if (impactDelayAfterTrigger > 0f)
            yield return new WaitForSeconds(impactDelayAfterTrigger);

    // Aplicar daño en el radio comprobando IHealth o PlayerHealth (usar impactPosition como origen)
    Collider2D[] hits = Physics2D.OverlapCircleAll(impactPosition, radius, damageLayer);
        foreach (var c in hits)
        {
            if (c == null) continue;

            // Intentar IHealth (genérico para enemigos y jefe)
            if (c.TryGetComponent<IHealth>(out var ih))
            {
                if (!ih.IsDead)
                    ih.TakeDamage(damage);
            }
            else
            {
                // Fallback: PlayerHealth
                var ph = c.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(Mathf.RoundToInt(damage));
                }
            }
        }

        // Reproducir VFX de impacto en impactPosition
        if (impactVfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(impactVfxPrefab, impactPosition, Quaternion.identity);
            // Destruir el VFX después de 2 segundos
            Destroy(vfxInstance, 2f);
        }

        // Limpiar la telegraph
        if (telegraphInstance != null)
            Destroy(telegraphInstance);

        // Destruir este objeto inmediatamente ya que el VFX se destruirá por separado
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 gp = Application.isPlaying ? impactPosition : (transform.position + new Vector3(impactVfxXOffset, impactVfxYOffset, 0));
        Gizmos.DrawWireSphere(gp, radius);
    }
}
