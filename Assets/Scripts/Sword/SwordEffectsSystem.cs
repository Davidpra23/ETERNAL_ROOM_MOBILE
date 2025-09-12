using UnityEngine;
using System.Collections;

public class SwordEffectsSystem : MonoBehaviour
{
    [Header("Slash Effect")]
    [SerializeField] private ParticleSystem slashEffect;
    [SerializeField] private float effectDuration = 0.3f;
    [SerializeField] private float slashZPosition = -1f;
    [SerializeField] private bool enableSlashFollow = true;
    [SerializeField] private float slashDelay = 0f;
    
    [Header("Slash Rotations")]
    [SerializeField] private Vector3 slashRotationRight = new Vector3(0f, 0f, 90f);
    [SerializeField] private Vector3 slashRotationLeft = new Vector3(0f, 180f, 90f);
    [SerializeField] private Vector3 slashRotationUp = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 slashRotationDown = new Vector3(0f, 0f, 180f);
    
    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float hitEffectDuration = 0.5f;
    [SerializeField] private float hitEffectZPosition = -0.5f;
    [SerializeField] private float hitDirectionOffset = 0f;
    
    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SwordDamageSystem damageSystem;
    [SerializeField] private Transform parentTransform; // Referencia al padre para verificar escala
    
    void Start()
    {
        // Buscar referencias automáticamente si no están asignadas
        if (damageSystem == null)
            damageSystem = GetComponent<SwordDamageSystem>();
        
        if (playerTransform == null)
            playerTransform = transform.parent;
        
        // Buscar automáticamente el padre si no está asignado
        if (parentTransform == null)
            parentTransform = transform.parent;
    }
    
    public void ExecuteAttackEffects()
    {
        StartCoroutine(ExecuteAttackEffectsWithDelay());
    }
    
    private IEnumerator ExecuteAttackEffectsWithDelay()
    {
        if (slashDelay > 0f)
        {
            yield return new WaitForSeconds(slashDelay);
        }
        
        SpawnSlashEffect();
    }
    
    private void SpawnSlashEffect()
    {
        if (slashEffect == null || attackPoint == null) return;
        
        Vector3 slashPosition = attackPoint.position;
        slashPosition.z = slashZPosition;
        
        ParticleSystem effectInstance = Instantiate(slashEffect, slashPosition, Quaternion.identity);
        ConfigureParticleRotation(effectInstance);
        
        effectInstance.transform.position = new Vector3(
            effectInstance.transform.position.x,
            effectInstance.transform.position.y,
            slashZPosition
        );
        
        if (enableSlashFollow)
        {
            StartCoroutine(SlashFollowRoutine(effectInstance.transform));
        }
        
        Destroy(effectInstance.gameObject, effectDuration);
    }
    
    private IEnumerator SlashFollowRoutine(Transform slashTransform)
    {
        Quaternion originalRotation = slashTransform.rotation;
        Vector3 originalScale = slashTransform.localScale;
        Vector3 initialOffset = slashTransform.position - attackPoint.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < effectDuration && slashTransform != null && attackPoint != null)
        {
            elapsedTime += Time.deltaTime;
            Vector3 newPosition = attackPoint.position + initialOffset;
            newPosition.z = slashZPosition;
            slashTransform.position = newPosition;
            slashTransform.rotation = originalRotation;
            slashTransform.localScale = originalScale;
            yield return null;
        }
    }
    
    private void ConfigureParticleRotation(ParticleSystem particleSystem)
    {
        var rotationModule = particleSystem.main;
        rotationModule.startRotation3D = true;
        
        Vector3 rotation = GetSlashRotationForDirection();
        rotationModule.startRotationX = rotation.x * Mathf.Deg2Rad;
        rotationModule.startRotationY = rotation.y * Mathf.Deg2Rad;
        rotationModule.startRotationZ = rotation.z * Mathf.Deg2Rad;
    }
    
    private Vector3 GetSlashRotationForDirection()
    {
        if (damageSystem != null)
        {
            // Usar la dirección del damage system que ya apunta correctamente al enemigo
            Vector2 aimDirection = damageSystem.GetAttackDirection();
            return GetRotationFromDirection(aimDirection);
        }
        
        // Fallback: usar la rotación de la espada pero corregida por escala del padre
        return GetRotationBasedOnParentScale();
    }
    
    private Vector3 GetRotationFromDirection(Vector2 direction)
    {
        // Determinar la dirección basada en el vector de aim
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        Vector3 baseRotation;
        
        if (angle >= -45f && angle < 45f) 
            baseRotation = slashRotationRight;
        else if (angle >= 45f && angle < 135f) 
            baseRotation = slashRotationUp;
        else if (angle >= 135f || angle < -135f) 
            baseRotation = slashRotationLeft;
        else 
            baseRotation = slashRotationDown;
        
        // Aplicar corrección de escala para arriba y abajo
        return ApplyScaleCorrection(baseRotation);
    }
    
    private Vector3 GetRotationBasedOnParentScale()
    {
        // CORREGIDO: Usar parentTransform en lugar de playerTransform
        if (parentTransform == null) 
        {
            // Fallback si no hay parentTransform
            if (playerTransform != null)
            {
                return playerTransform.localScale.x > 0 ? slashRotationRight : slashRotationLeft;
            }
            return slashRotationRight;
        }
        
        // Usar la escala del PADRE para determinar dirección
        float parentScaleX = parentTransform.localScale.x;
        
        Vector3 baseRotation;
        
        if (parentScaleX > 0)
        {
            baseRotation = slashRotationRight;
        }
        else if (parentScaleX < 0)
        {
            baseRotation = slashRotationLeft;
        }
        else
        {
            baseRotation = slashRotationRight;
        }
        
        return ApplyScaleCorrection(baseRotation);
    }
    
    private Vector3 ApplyScaleCorrection(Vector3 baseRotation)
    {
        // Solo aplicar corrección si tenemos referencia al padre
        if (parentTransform == null) return baseRotation;
        
        // Verificar si el padre tiene escala negativa en X
        bool isFlipped = parentTransform.localScale.x < 0;
        
        // Aplicar corrección solo para direcciones arriba y abajo cuando está flipado
        if (isFlipped && (IsUpDirection(baseRotation) || IsDownDirection(baseRotation)))
        {
            return new Vector3(
                baseRotation.x,
                baseRotation.y + 180f, // Sumar 180 grados en Y
                baseRotation.z
            );
        }
        
        return baseRotation;
    }
    
    private bool IsUpDirection(Vector3 rotation)
    {
        // Comparar con la rotación base de arriba (con tolerancia)
        return Mathf.Approximately(rotation.z, slashRotationUp.z) &&
               Mathf.Approximately(rotation.x, slashRotationUp.x);
    }
    
    private bool IsDownDirection(Vector3 rotation)
    {
        // Comparar con la rotación base de abajo (con tolerancia)
        return Mathf.Approximately(rotation.z, slashRotationDown.z) &&
               Mathf.Approximately(rotation.x, slashRotationDown.x);
    }
    
    public void SpawnHitEffect(Vector3 hitPosition)
    {
        if (hitEffect == null) return;
        
        Vector3 effectPosition = hitPosition;
        effectPosition.z = hitEffectZPosition;
        
        Quaternion hitRotation = CalculateHitRotation(hitPosition);
        GameObject hitInstance = Instantiate(hitEffect, effectPosition, hitRotation);
        
        Destroy(hitInstance, hitEffectDuration);
    }
    
    private Quaternion CalculateHitRotation(Vector3 hitPosition)
    {
        if (playerTransform == null) return Quaternion.identity;
        
        // Usar la posición del player para cálculos consistentes
        Vector3 hitDirection = (hitPosition - playerTransform.position).normalized;
        float hitAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        float effectAngle = hitAngle + 180f + hitDirectionOffset;
        
        return Quaternion.Euler(0f, 0f, effectAngle);
    }
    
    // Método para configurar la rotación manualmente (desde otro sistema)
    public void SetSlashRotation(Vector3 rotation)
    {
        // Puedes usar este método si otro sistema quiere controlar la rotación
    }
    
    // Método público para verificar si está flipado (útil para debug)
    public bool IsParentFlipped()
    {
        return parentTransform != null && parentTransform.localScale.x < 0;
    }
}