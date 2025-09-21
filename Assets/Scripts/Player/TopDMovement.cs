using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]

public class TopDMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Obtener entrada del jugador
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Si estás usando solo 4 direcciones, elimina movimiento diagonal:
        if (moveInput.x != 0) moveInput.y = 0;

        // Actualizar Animator
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", -moveInput.y); // ← invertimos aquí
        animator.SetBool("IsMoving", moveInput != Vector2.zero);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }
}
