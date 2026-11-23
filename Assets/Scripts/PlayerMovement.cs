
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 3f;

    [Header("Character Rotation")]
    public Transform characterModel;
    public float rotationSpeed = 15f;

    private Rigidbody rb;
    private Animator animator;

    private Vector3 movement;
    private Vector3 lastMoveDirection;
    private Vector3 dashDirection;

    private float dashTimer;
    private float dashCooldownTimer;

    public bool IsInvincible => dashTimer > 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        animator = GetComponentInChildren<Animator>();

        lastMoveDirection = Vector3.forward;
    }

    void Update()
    {
        // Movement input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        movement = new Vector3(moveX, 0f, moveZ).normalized;

        // Animation
        if (animator != null)
        {
            float animationSpeed = movement.magnitude;

            // Keep walking animation during dash
            if (dashTimer > 0f)
            {
                animationSpeed = 1f;
            }

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                Time.deltaTime
            );
        }

        // Remember the last movement direction
        if (movement.sqrMagnitude > 0f)
        {
            lastMoveDirection = movement;
        }

        // Dash cooldown
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Dash input
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryDash();
        }

        RotateCharacter();
    }

    void FixedUpdate()
    {
        if (dashTimer > 0f)
        {
            rb.MovePosition(
                rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime
            );

            dashTimer -= Time.fixedDeltaTime;
        }
        else
        {
            Vector3 newPosition =
                rb.position + movement * moveSpeed * Time.fixedDeltaTime;

            rb.MovePosition(newPosition);
        }
    }

    void TryDash()
    {
        if (dashCooldownTimer > 0f)
            return;

        dashDirection = movement.sqrMagnitude > 0f
            ? movement
            : lastMoveDirection;

        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        Debug.Log("Dash activated!");
    }

    void RotateCharacter()
    {
        if (characterModel == null)
            return;

        // Face the dash direction while dashing
        Vector3 direction = dashTimer > 0f
            ? dashDirection
            : movement;

        if (direction.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            characterModel.rotation = Quaternion.Slerp(
                characterModel.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}

