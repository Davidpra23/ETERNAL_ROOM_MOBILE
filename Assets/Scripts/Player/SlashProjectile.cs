using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Proyectil de slash que atraviesa enemigos, aplicando daño una sola vez por enemigo.
/// Se mueve en línea recta y se autodestruye tras cierto tiempo o distancia.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SlashProjectile : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad del proyectil")]
    [SerializeField] private float speed = 15f;
    [Tooltip("El sprite del prefab mira a la izquierda por defecto? (true = izquierda/180°, false = derecha/0°)")]
    [SerializeField] private bool spriteDefaultFacesLeft = true;
    
    [Header("Daño")]
    [Tooltip("Daño base del proyectil")]
    [SerializeField] private float baseDamage = 10f;
    [Tooltip("Capa de enemigos")]
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Vida del Proyectil")]
    [Tooltip("Tiempo máximo de vida (segundos)")]
    [SerializeField] private float lifetime = 2f;
    [Tooltip("Distancia máxima recorrida (0 = infinito)")]
    [SerializeField] private float maxDistance = 0f;
    
    [Header("VFX")]
    [Tooltip("Efecto al impactar enemigo (opcional)")]
    [SerializeField] private GameObject hitVfxPrefab;
    [Tooltip("Destruir el proyectil tras golpear X enemigos (0 = nunca se destruye por hits)")]
    [SerializeField] private int destroyAfterHits = 0;

    private Rigidbody2D rb;
    private Vector2 direction;
    private Vector3 startPosition;
    private readonly HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
    private int totalHits = 0;
    private bool initialized = false;
    private Transform ownerRoot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>
    /// Inicializa el proyectil con una dirección específica.
    /// </summary>
    public void Initialize(Vector2 dir)
    {
        Initialize(dir, null);
    }

    /// <summary>
    /// Inicializa el proyectil con una dirección y el root del dueño para ignorar auto-colisión.
    /// </summary>
    public void Initialize(Vector2 dir, Transform owner)
    {
        direction = dir.normalized;
        initialized = true;
        startPosition = transform.position;
        ownerRoot = owner;
        
        // Rotar el proyectil para que mire en la dirección de movimiento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Si el sprite mira a la izquierda por defecto, sumar 180°
        if (spriteDefaultFacesLeft)
            angle += 180f;
        
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        
        Debug.Log($"[SlashProjectile] Dir: {direction}, Angle: {angle}°, Pos: {transform.position}");
        
        // Aplicar velocidad
        if (rb != null)
            rb.linearVelocity = direction * speed;
        
        // Autodestrucción por tiempo
        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        // Si no se inicializó externamente, usar la dirección del transform
        if (!initialized)
        {
            direction = transform.right; // o transform.up según orientación
            Initialize(direction);
        }
    }

    private void Update()
    {
        if (!initialized) return;

        // Verificar distancia máxima
        if (maxDistance > 0f)
        {
            float dist = Vector3.Distance(startPosition, transform.position);
            if (dist >= maxDistance)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignorar colisiones con el dueño del proyectil
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // Evitar golpear al mismo objetivo dos veces
        if (hitEnemies.Contains(other.gameObject)) return;

        // Si hay un filtro de capas configurado, respétalo
        if (enemyLayer.value != 0)
        {
            if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        }

        // Aceptar cualquier entidad que implemente IHealth (enemigos y jefe)
        var targetHealth = other.GetComponent<IHealth>();
        if (targetHealth == null || targetHealth.IsDead) return;

        // Aplicar daño
        targetHealth.TakeDamage(baseDamage);
        hitEnemies.Add(other.gameObject);
        totalHits++;

        // Spawn VFX de impacto
        if (hitVfxPrefab != null)
        {
            Vector3 hitPos = other.ClosestPoint(transform.position);
            Instantiate(hitVfxPrefab, hitPos, Quaternion.identity);
        }

        // Destruir si alcanzó el límite de hits
        if (destroyAfterHits > 0 && totalHits >= destroyAfterHits)
            Destroy(gameObject);
    }

    // Opcional: destruir al salir de cámara
    private void OnBecameInvisible()
    {
        // Pequeño delay para evitar destrucción prematura al spawn
        Invoke(nameof(DestroyIfOffScreen), 0.5f);
    }

    private void DestroyIfOffScreen()
    {
        if (!GetComponent<Renderer>().isVisible)
            Destroy(gameObject);
    }
}
