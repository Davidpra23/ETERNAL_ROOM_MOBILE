using UnityEngine;
using System.Collections;

/// <summary>
/// Mecanica simple de slash: retrocede 45° y luego avanza 90° (doble),
/// regresando al ángulo base al final. Pensado para adjuntarlo al objeto de la espada.
/// </summary>
public class SimpleSwordSlash : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform que rota durante el slash. Si está vacío usa este Transform.")]
    [SerializeField] private Transform slashPivot;
    [Tooltip("Transform para determinar la direccion (usa su scale.x). Si está vacío, usa el parent/root.")]
    [SerializeField] private Transform facingReference;
    [Tooltip("Sistema de arma de espada con slash. Si está vacío se auto-busca.")]
    [SerializeField] private SlashSwordWeapon slashSwordWeapon;

    [Header("VFX de Slash")]
    [Tooltip("Prefab del efecto visual de slash (se instancia al iniciar el swing)")]
    [SerializeField] private GameObject slashVfxPrefab;
    [Tooltip("Punto de spawn del VFX (por ejemplo, la punta de la espada). Si está vacío se usa el pivot")]
    [SerializeField] private Transform vfxSpawnPoint;
    [Tooltip("Parentear el VFX al pivot para que siga el movimiento del swing")]
    [SerializeField] private bool parentVfxToPivot = true;
    [Tooltip("El prefab por defecto mira a la derecha? Si es falso, asumimos que mira a la izquierda")]
    [SerializeField] private bool vfxDefaultFacesRight = false;
    [Tooltip("Offset angular extra para alinear el VFX con tu arte (grados)")]
    [SerializeField] private float vfxAngleOffset = 0f;
    [Tooltip("Autodestruir el VFX tras X segundos (0 = lo maneja el prefab)")]
    [SerializeField] private float vfxAutoDestroyAfter = 0f;

    [Header("Proyectil de Slash")]
    [Tooltip("Prefab del proyectil que se lanza al hacer slash")]
    [SerializeField] private GameObject slashProjectilePrefab;
    [Tooltip("Punto de spawn del proyectil (ej. punta de espada). Si está vacío usa el pivot")]
    [SerializeField] private Transform projectileSpawnPoint;
    [Tooltip("Offset de posición del spawn del proyectil (en espacio local del spawn point)")]
    [SerializeField] private Vector2 projectileSpawnOffset = Vector2.zero;

    [Header("Ángulos (grados)")]
    [Tooltip("Ángulo de retroceso (negativo = hacia atrás del ángulo base)")]
    [SerializeField] private float windUpDegrees = -45f;
    [Tooltip("Ángulo de avance desde el ángulo base (doble de 45 = 90)")]
    [SerializeField] private float slashDegrees = 90f;

    [Tooltip("Invierte los ángulos cuando el personaje mira a la derecha (scale.x >= 0).")]
    [SerializeField] private bool invertWhenFacingRight = true;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float windUpDuration = 0.08f;
    [SerializeField] private float slashDuration = 0.15f;
    [SerializeField] private float returnDuration = 0.10f;

    [Header("Curvas de Animación")]
    [SerializeField] private AnimationCurve windUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isSlashing = false;
    private Quaternion baseRotation;

    private void Awake()
    {
        if (slashPivot == null) slashPivot = transform;
    }

    private void Start()
    {
        baseRotation = slashPivot.rotation;
        
        if (facingReference == null)
            facingReference = transform.parent != null ? transform.parent : transform.root;
        
        // Auto-buscar SlashSwordWeapon si no está asignado
        if (slashSwordWeapon == null)
            slashSwordWeapon = GetComponentInParent<SlashSwordWeapon>() ?? GetComponent<SlashSwordWeapon>();
        
        // Suscribirse al evento OnAttack
        if (slashSwordWeapon != null)
        {
            slashSwordWeapon.OnAttack += Slash;
            Debug.Log("[SimpleSwordSlash] Suscrito a SlashSwordWeapon.OnAttack");
        }
        else
        {
            Debug.LogWarning("[SimpleSwordSlash] No se encontró SlashSwordWeapon. El slash no se ejecutará automáticamente.");
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento para evitar memory leaks
        if (slashSwordWeapon != null)
            slashSwordWeapon.OnAttack -= Slash;
    }

    /// <summary>
    /// Dispara la animación de slash si no hay una en curso.
    /// </summary>
    public void Slash()
    {
        if (!isSlashing && gameObject.activeInHierarchy)
            StartCoroutine(SlashRoutine());
    }

    private IEnumerator SlashRoutine()
    {
        isSlashing = true;

        // Guardar base actual por si la empuñadura apunta dinámicamente
        baseRotation = slashPivot.rotation;

        // Determinar direccion segun scale.x y ajustar signos de ángulos si procede
        Transform refT = facingReference != null ? facingReference : transform;
        bool facingRight = (refT.lossyScale.x >= 0f);
        float wu = windUpDegrees;
        float sl = slashDegrees;
        if (invertWhenFacingRight && facingRight)
        {
            wu = -wu;
            sl = -sl;
        }

        // 1) Wind-up: retrocede windUpDegrees desde base
        Quaternion windUpRot = baseRotation * Quaternion.Euler(0f, 0f, wu);
        yield return RotateTo(slashPivot, windUpRot, windUpDuration, windUpCurve);

        // Instanciar VFX al inicio del swing hacia adelante
        SpawnSlashVfx(facingRight);
        
        // Instanciar proyectil de slash
        SpawnSlashProjectile(facingRight);

        // 2) Slash: avanza slashDegrees desde la base (doble de 45 = 90)
        Quaternion slashRot = baseRotation * Quaternion.Euler(0f, 0f, sl);
        yield return RotateTo(slashPivot, slashRot, slashDuration, slashCurve);

        // 3) Vuelta al ángulo base
        yield return RotateTo(slashPivot, baseRotation, returnDuration, returnCurve);

        isSlashing = false;
    }

    private void SpawnSlashVfx(bool facingRight)
    {
        if (slashVfxPrefab == null || slashPivot == null) return;

        Vector3 pos = vfxSpawnPoint != null ? vfxSpawnPoint.position : slashPivot.position;
        Quaternion rot = slashPivot.rotation * Quaternion.Euler(0f, 0f, vfxAngleOffset);

        Transform parent = parentVfxToPivot ? slashPivot : null;
        var go = Object.Instantiate(slashVfxPrefab, pos, rot, parent);

        if (go != null)
        {
            var t = go.transform;
            Vector3 ls = t.localScale;
            float desiredSign = (facingRight == vfxDefaultFacesRight) ? 1f : -1f;
            ls.x = Mathf.Abs(ls.x) * desiredSign;
            t.localScale = ls;

            if (vfxAutoDestroyAfter > 0f)
                Object.Destroy(go, vfxAutoDestroyAfter);
        }
    }

    private void SpawnSlashProjectile(bool facingRight)
    {
        if (slashProjectilePrefab == null || slashPivot == null) return;

        Transform spawnT = projectileSpawnPoint != null ? projectileSpawnPoint : slashPivot;
        Vector3 pos = spawnT.position + spawnT.TransformDirection(projectileSpawnOffset);
        
        // Calcular dirección usando el ángulo del pivot
        float angleRad = slashPivot.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        
        // Si mira a la izquierda (scale.x negativo), invertir la dirección
        if (!facingRight)
        {
            direction = -direction;
        }
        
        Debug.Log($"[SimpleSwordSlash] Pivot angle: {slashPivot.eulerAngles.z}°, Direction: {direction}, FacingRight: {facingRight}");
        
        var go = Object.Instantiate(slashProjectilePrefab, pos, Quaternion.identity);
        
        if (go != null)
        {
            var proj = go.GetComponent<SlashProjectile>();
            if (proj != null)
            {
                // Pasar root del dueño para evitar auto-daño
                Transform ownerRoot = transform.root;
                proj.Initialize(direction, ownerRoot);
            }
        }
    }

    private static IEnumerator RotateTo(Transform t, Quaternion targetRot, float duration, AnimationCurve curve)
    {
        if (t == null) yield break;
        if (duration <= 0f)
        {
            t.rotation = targetRot;
            yield break;
        }

        Quaternion startRot = t.rotation;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / duration);
            float f = curve != null ? curve.Evaluate(k) : k;
            t.rotation = Quaternion.Slerp(startRot, targetRot, f);
            yield return null;
        }
        t.rotation = targetRot;
    }
}
