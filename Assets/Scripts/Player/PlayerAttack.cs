using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAnimation playerAnim;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Systems")]
    [SerializeField] private AttackDirectionSystem directionSystem;
    [SerializeField] private AutoAimSystem autoAimSystem;
    [SerializeField] private AttackEffectsSystem effectsSystem;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private float lastAttackTime = 0f;
    private bool isAttackEnabled = true;
    
    public System.Action OnAttack;
    public System.Action OnAttackHit;
    
    void Awake()
    {
        if (playerAnim == null) playerAnim = GetComponent<PlayerAnimation>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        
        InitializeSystems();
    }
    
    void Update()
    {
        if (autoAimSystem != null) autoAimSystem.UpdateAutoAim();
        if (directionSystem != null) directionSystem.UpdateAttackDirection();
    }
    
    private void InitializeSystems()
    {
        if (directionSystem == null) directionSystem = GetComponent<AttackDirectionSystem>();
        if (autoAimSystem == null) autoAimSystem = GetComponent<AutoAimSystem>();
        if (effectsSystem == null) effectsSystem = GetComponent<AttackEffectsSystem>();
        
        // Inyectar dependencias
        if (directionSystem != null) directionSystem.Initialize(playerMovement, autoAimSystem);
        if (autoAimSystem != null) autoAimSystem.Initialize(playerMovement, enemyLayer);
        if (effectsSystem != null) effectsSystem.Initialize(this, enemyLayer);
    }
    
    public void TryAttack()
    {
        if (!isAttackEnabled) return;
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        PerformAttack();
    }
    
    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        
        if (playerAnim != null) playerAnim.PlayAttackAnimation();
        
        OnAttack?.Invoke();
        
        if (effectsSystem != null) effectsSystem.ExecuteAttackEffects();
    }
    
    public int GetDamage() => attackDamage;

    // Métodos públicos
    public void SetAttackEnabled(bool enabled) => isAttackEnabled = enabled;
    public bool IsAttackReady() => Time.time - lastAttackTime >= attackCooldown;
    
    // Getters para otros sistemas
    public PlayerMovement GetPlayerMovement() => playerMovement;
    public AttackDirectionSystem GetDirectionSystem() => directionSystem;
    public AutoAimSystem GetAutoAimSystem() => autoAimSystem;
}