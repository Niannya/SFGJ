using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    private Vector2 moveInput;
    private bool canMove = true;

    private Vector2 facingDirection = Vector2.down;
    public Vector2 FacingDirection => facingDirection;
    public bool CanMove => canMove;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!canMove) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;
        
        if (moveInput != Vector2.zero) facingDirection = moveInput;
    }

    private void FixedUpdate()
    {
        Vector2 nextPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            moveInput = Vector2.zero;
        }
    }
}
