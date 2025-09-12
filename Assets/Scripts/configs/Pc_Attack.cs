using UnityEngine;

public class Pc_Attack : MonoBehaviour
{
    [Header("Configuración de Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private bool useMouseClick = true;
    [SerializeField] private int mouseButton = 0;
    
    [Header("Referencia al Sistema de Espada")]
    [SerializeField] private SwordDamageSystem swordDamageSystem;
    
    [Header("Opciones")]
    [SerializeField] private bool showDebug = true;

    void Awake()
    {
        // Buscar automáticamente el SwordDamageSystem si no está asignado
        if (swordDamageSystem == null)
        {
            swordDamageSystem = FindObjectOfType<SwordDamageSystem>();
            
            // Si todavía no se encuentra, buscar en hijos del player
            if (swordDamageSystem == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    swordDamageSystem = player.GetComponentInChildren<SwordDamageSystem>();
                }
            }
        }
    }

    void Update()
    {
        if (ShouldAttack())
        {
            Attack();
        }
    }

    private bool ShouldAttack()
    {
        bool keyInput = Input.GetKeyDown(attackKey);
        bool mouseInput = useMouseClick && Input.GetMouseButtonDown(mouseButton);
        return keyInput || mouseInput;
    }

    private void Attack()
    {
        if (swordDamageSystem != null)
        {
            swordDamageSystem.TryAttack();
            
            if (showDebug)
            {
                Debug.Log("Ataque ejecutado desde PC input");
            }
        }
        else if (showDebug)
        {
            Debug.LogWarning("SwordDamageSystem no encontrado");
        }
    }
    
    // Métodos públicos para cambiar el sistema de espada en tiempo de ejecución
    public void SetSwordDamageSystem(SwordDamageSystem newSystem)
    {
        swordDamageSystem = newSystem;
    }
    
    public SwordDamageSystem GetSwordDamageSystem()
    {
        return swordDamageSystem;
    }
}