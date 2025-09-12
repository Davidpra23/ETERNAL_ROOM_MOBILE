using UnityEngine;
using System.Collections;

public class AttackEffectsSystem : MonoBehaviour
{
    [Header("Attack Point Reference")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.5f;
    
    [Header("Slash Effect")]
    [SerializeField] private ParticleSystem slashEffect;
    [SerializeField] private float effectDuration = 0.3f;
    [SerializeField] private float slashZPosition = -1f;
    [SerializeField] private bool enableSlashFollow = true;
    [SerializeField] private float slashDelay = 0f; // Nuevo: Delay configurable
    
    [Header("Slash Rotations")]
    [SerializeField] private Vector3 slashRotationRight = new Vector3(0f, 0f, 90f);
    [SerializeField] private Vector3 slashRotationLeft = new Vector3(0f, 180f, 90f);
    [SerializeField] private Vector3 slashRotationUp = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 slashRotationDown = new Vector3(0f, 0f, 180f);
    
    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float hitEffectDuration = 0.5f;
    [SerializeField] private float hitEffectZPosition = -0.5f;
    [SerializeField] private bool useRelativeDirection = true;
    [SerializeField] private float hitDirectionOffset = 0f;
    
    private PlayerAttack playerAttack;
    private LayerMask enemyLayer;
    private AttackDirectionSystem directionSystem;
    
    public void Initialize(PlayerAttack attack, LayerMask layerMask)
    {
        playerAttack = attack;
        enemyLayer = layerMask;
        directionSystem = attack.GetDirectionSystem();
    }
    
    public void ExecuteAttackEffects()
    {
        StartCoroutine(ExecuteAttackEffectsWithDelay());
    }
    
    private IEnumerator ExecuteAttackEffectsWithDelay()
    {
        // Aplicar delay antes de spawnear el slash
        if (slashDelay > 0f)
        {
            yield return new WaitForSeconds(slashDelay);
        }
        
        SpawnSlashEffect();
        DetectEnemies();
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
        if (directionSystem == null) return slashRotationRight;
        
        Vector3 baseRotation;
        
        switch (directionSystem.GetCurrentAttackDirection())
        {
            case AttackDirectionSystem.AttackDirection.Right: 
                baseRotation = slashRotationRight;
                break;
            case AttackDirectionSystem.AttackDirection.Left: 
                baseRotation = slashRotationLeft;
                break;
            case AttackDirectionSystem.AttackDirection.Up: 
                baseRotation = slashRotationUp;
                // Si el player mira a la derecha, sumar -180 a la Y
                if (IsPlayerFacingRight())
                {
                    baseRotation.y -= 180f;
                }
                break;
            case AttackDirectionSystem.AttackDirection.Down: 
                baseRotation = slashRotationDown;
                // Si el player mira a la derecha, sumar -180 a la Y
                if (IsPlayerFacingRight())
                {
                    baseRotation.y -= 180f;
                }
                break;
            default: 
                baseRotation = slashRotationRight;
                break;
        }
        
        return baseRotation;
    }
    
    private bool IsPlayerFacingRight()
    {
        // Buscar el hijo UniRoot que maneja las direcciones
        Transform directionHandler = transform.Find("kk");
        
        if (directionHandler != null)
        {
            // Escala -1 = derecha, escala 1 = izquierda
            return directionHandler.localScale.x < 0;
        }
        
        // Fallback: asumir que mira a la derecha
        return true;
    }
    
    private void DetectEnemies()
    {
        if (attackPoint == null || directionSystem == null) return;
        
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        bool hitAnyEnemy = false;
        
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead && directionSystem.IsInFieldOfView(enemy.transform))
            {
                enemyHealth.TakeDamage(playerAttack.GetDamage());
                hitAnyEnemy = true;
                SpawnHitEffect(enemy.transform.position, enemy.transform);
            }
        }
        
        if (hitAnyEnemy)
        {
            playerAttack.OnAttackHit?.Invoke();
        }
    }
    
    private void SpawnHitEffect(Vector3 hitPosition, Transform enemyTransform)
    {
        if (hitEffect == null) return;
        
        Vector3 effectPosition = hitPosition;
        effectPosition.z = hitEffectZPosition;
        
        Quaternion hitRotation = CalculateHitRotation(enemyTransform);
        GameObject hitInstance = Instantiate(hitEffect, effectPosition, hitRotation);
        
        Destroy(hitInstance, hitEffectDuration);
    }
    
    private Quaternion CalculateHitRotation(Transform enemyTransform)
    {
        if (enemyTransform == null) return hitEffect.transform.rotation;
        
        Vector3 hitDirection = (enemyTransform.position - transform.position).normalized;
        float hitAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        float effectAngle = hitAngle + 180f + hitDirectionOffset;
        
        return Quaternion.Euler(0f, 0f, effectAngle);
    }
    
    public int GetDamage() => playerAttack != null ? playerAttack.GetDamage() : 10;
}