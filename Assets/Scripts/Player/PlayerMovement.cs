using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
   
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    private PlayerAttack playerAttack;

    // Referencias
    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Vector2 currentVelocity;

    // Modificadores de movimiento (para habilidades y mejoras)
    private float speedMultiplier = 1f;
    private bool isMovementEnabled = true;
    private bool isDashing = false;

    // Sistema de eventos para notificar cambios
    public System.Action<Vector2> OnMovementDirectionChanged;
    public System.Action<bool> OnMovementStateChanged;

    // Propiedades públicas para acceso controlado
    public float CurrentSpeed { get; private set; }
    public Vector2 LastMovementDirection { get; private set; } = Vector2.right;
    public bool IsMoving { get; private set; }

    // Nueva propiedad para la velocidad actual (necesaria para la cámara)
    public Vector2 CurrentVelocity { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetupRigidbody();
    }

    void Update()
    {
        if (!isMovementEnabled) return;

        HandleInput();
        UpdateMovementState();
        // ELIMINA esta línea: UpdateCharacterFlipping();
    }

    void FixedUpdate()
    {
        if (!isMovementEnabled) return;

        ApplyMovement();
    }
    public Vector2 GetMovementDirection()
    {
        return LastMovementDirection;
    }



    private void SetupRigidbody()
    {
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void HandleInput()
    {
        // Input para móvil - joystick virtual
        movementInput = GetMobileInput();

        // Alternativa para testing en editor
#if UNITY_EDITOR
        if (movementInput == Vector2.zero)
        {
            movementInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).normalized;
        }
#endif
    }

    private Vector2 GetMobileInput()
    {
        // Aquí integrarás con tu sistema de joystick virtual
        // Por ahora devolvemos un input simulado
        return VirtualJoystick.Instance != null ?
               VirtualJoystick.Instance.Direction :
               Vector2.zero;
    }

    private void UpdateMovementState()
    {
        bool wasMoving = IsMoving;
        IsMoving = movementInput != Vector2.zero;

        // Actualizar dirección última
        if (IsMoving)
        {
            LastMovementDirection = movementInput.normalized;
            OnMovementDirectionChanged?.Invoke(LastMovementDirection);
        }

        // Notificar cambio de estado
        if (wasMoving != IsMoving)
        {
            OnMovementStateChanged?.Invoke(IsMoving);
        }
    }

    private void ApplyMovement()
    {
        if (isDashing)
        {
            // El dash maneja su propio movimiento
            return;
        }

        Vector2 targetVelocity = movementInput * (baseMoveSpeed * speedMultiplier);

        // Suavizar aceleración y desaceleración
        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            (targetVelocity != Vector2.zero ? acceleration : deceleration) * Time.fixedDeltaTime
        );

        rb.linearVelocity = currentVelocity;
        CurrentSpeed = currentVelocity.magnitude;

        // Actualizar la propiedad de velocidad actual para la cámara
        CurrentVelocity = currentVelocity;
    }

    // ========== MÉTODOS PÚBLICOS PARA MODIFICAR EL MOVIMIENTO ==========

    public void SetMovementEnabled(bool enabled)
    {
        isMovementEnabled = enabled;

        if (!enabled)
        {
            rb.linearVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
            CurrentSpeed = 0f;
            CurrentVelocity = Vector2.zero;
        }
    }

    public void ApplySpeedMultiplier(float multiplier, float duration = 0f)
    {
        speedMultiplier = multiplier;

        if (duration > 0f)
        {
            CancelInvoke(nameof(ResetSpeedMultiplier));
            Invoke(nameof(ResetSpeedMultiplier), duration);
        }
    }

    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
    }

    public void Dash(Vector2 direction, float dashSpeed, float dashDuration)
    {
        if (isDashing) return;

        StartCoroutine(PerformDash(direction, dashSpeed, dashDuration));
    }

    private System.Collections.IEnumerator PerformDash(Vector2 direction, float dashSpeed, float dashDuration)
    {
        isDashing = true;
        Vector2 dashVelocity = direction.normalized * dashSpeed;
        rb.linearVelocity = dashVelocity;
        CurrentVelocity = dashVelocity; // Actualizar para la cámara

        // Ignorar colisiones con enemigos durante el dash
        SetEnemyCollisions(false);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        SetEnemyCollisions(true);

        // Recuperar el movimiento normal
        currentVelocity = movementInput * (baseMoveSpeed * speedMultiplier);
        rb.linearVelocity = currentVelocity;
        CurrentVelocity = currentVelocity; // Actualizar para la cámara
    }

    private void SetEnemyCollisions(bool enable)
    {
        // Aquí implementarías la lógica para ignorar colisiones con enemigos
        // durante el dash usando LayerMask o Physics2D.IgnoreCollision
    }

    public void Knockback(Vector2 direction, float force)
    {
        // Interrumpir el movimiento actual
        currentVelocity = direction.normalized * force;
        rb.linearVelocity = currentVelocity;
        CurrentVelocity = currentVelocity; // Actualizar para la cámara

        // Programar la recuperación del control
        CancelInvoke(nameof(ResetAfterKnockback));
        Invoke(nameof(ResetAfterKnockback), 0.3f);
    }

    private void ResetAfterKnockback()
    {
        // Restaurar el movimiento normal después del knockback
        currentVelocity = movementInput * (baseMoveSpeed * speedMultiplier);
        CurrentVelocity = currentVelocity; // Actualizar para la cámara
    }

    // ========== MÉTODOS PARA MEJORAS Y POWER-UPS ==========

    public void AddPermanentSpeedBonus(float bonus)
    {
        baseMoveSpeed += bonus;
    }

    public void ApplyTemporaryMovementModifier(MovementModifier modifier)
    {
        // Sistema para aplicar modificadores temporales complejos
        StartCoroutine(ApplyMovementModifierCoroutine(modifier));
    }

    private System.Collections.IEnumerator ApplyMovementModifierCoroutine(MovementModifier modifier)
    {
        float originalSpeed = baseMoveSpeed;
        baseMoveSpeed *= modifier.speedMultiplier;

        if (modifier.affectsAcceleration)
        {
            float originalAcceleration = acceleration;
            acceleration *= modifier.accelerationMultiplier;

            yield return new WaitForSeconds(modifier.duration);

            acceleration = originalAcceleration;
        }
        else
        {
            yield return new WaitForSeconds(modifier.duration);
        }

        baseMoveSpeed = originalSpeed;
    }

    // ========== MÉTODO NUEVO PARA LA CÁMARA ==========

    /// <summary>
    /// Obtiene la velocidad actual del jugador para que la cámara pueda usarla
    /// en el sistema de look ahead.
    /// </summary>
    public Vector2 GetCurrentVelocity()
    {
        return CurrentVelocity;
    }
}

// Clase para definir modificadores de movimiento complejos
[System.Serializable]
public class MovementModifier
{
    public float duration = 1f;
    public float speedMultiplier = 1f;
    public bool affectsAcceleration = false;
    public float accelerationMultiplier = 1f;
}