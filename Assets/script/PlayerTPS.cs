using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerTPS : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    private float velocityY;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    [SerializeField] private LayerMask groundMask;

    private bool isGrounded;

    private CharacterController controller;
    private TPS inputActions;

    private Vector2 moveInput;

    // INPUT FLAGS
    private bool jumpPressed;

    private bool isPunching;
    private bool isPunching2;

    private bool ultimatePressed;
    private bool jumpAttPressed;

    private bool isEmoting;

    private bool isBlocking;
    private bool isRunning;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new TPS();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // MOVE
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        // JUMP
        inputActions.Player.Jump.performed += OnJump;

        // PUNCH
        inputActions.Player.Punch.performed += OnPunch;
        inputActions.Player.Punch.canceled += OnPunch;

        // PUNCH 2
        inputActions.Player.Punch2.performed += OnPunch2;
        inputActions.Player.Punch2.canceled += OnPunch2;

        // ULTIMATE
        inputActions.Player.Ultimate.performed += OnUltimate;

        // JUMP ATTACK
        inputActions.Player.JumpAtt.performed += OnJumpAtt;

        // EMOT
        inputActions.Player.Emot.performed += OnEmot;

        // BLOCK
        inputActions.Player.Block.performed += OnBlockStart;
        inputActions.Player.Block.canceled += OnBlockEnd;

        // RUN
        inputActions.Player.Run.performed += OnRunStart;
        inputActions.Player.Run.canceled += OnRunEnd;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        inputActions.Player.Jump.performed -= OnJump;

        inputActions.Player.Punch.performed -= OnPunch;
        inputActions.Player.Punch.canceled -= OnPunch;

        inputActions.Player.Punch2.performed -= OnPunch2;
        inputActions.Player.Punch2.canceled -= OnPunch2;

        inputActions.Player.Ultimate.performed -= OnUltimate;

        inputActions.Player.JumpAtt.performed -= OnJumpAtt;

        inputActions.Player.Emot.performed -= OnEmot;

        inputActions.Player.Block.performed -= OnBlockStart;
        inputActions.Player.Block.canceled -= OnBlockEnd;

        inputActions.Player.Run.performed -= OnRunStart;
        inputActions.Player.Run.canceled -= OnRunEnd;

        inputActions.Disable();
    }

    private void Update()
    {
        CheckGround();

        HandleMovement();

        HandleJump();

        HandlePunch();

        HandlePunch2();

        HandleUltimate();

        HandleJumpAttack();

        HandleEmot();

        HandleBlock();

        ApplyGravity();

        UpdateAnimator();
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );
    }

    // =========================
    // INPUT CALLBACKS
    // =========================

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    private void OnPunch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isPunching = true;
        }

        if (context.canceled)
        {
            isPunching = false;
        }
    }

    private void OnPunch2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isPunching2 = true;
        }

        if (context.canceled)
        {
            isPunching2 = false;
        }
    }

    private void OnUltimate(InputAction.CallbackContext context)
    {
        ultimatePressed = true;
    }

    private void OnJumpAtt(InputAction.CallbackContext context)
    {
        jumpAttPressed = true;
    }

    private void OnEmot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isEmoting = true;
        }
    }

    private void OnBlockStart(InputAction.CallbackContext context)
    {
        isBlocking = true;
    }

    private void OnBlockEnd(InputAction.CallbackContext context)
    {
        isBlocking = false;
    }

    private void OnRunStart(InputAction.CallbackContext context)
    {
        isRunning = true;
    }

    private void OnRunEnd(InputAction.CallbackContext context)
    {
        isRunning = false;
    }

    // =========================
    // MOVEMENT
    // =========================

    private void HandleMovement()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        if (move.magnitude > 0.1f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0;
            camRight.y = 0;

            Vector3 moveDirection =
                camForward * move.z +
                camRight * move.x;

            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            float currentSpeed =
                isRunning ? runSpeed : moveSpeed;

            controller.Move(
                moveDirection.normalized *
                currentSpeed *
                Time.deltaTime
            );

            animator.SetBool("isWalk", true);
            animator.SetBool("isRun", isRunning);

            // BATALKAN EMOTE SAAT GERAK
            if (isEmoting)
            {
                isEmoting = false;
                animator.SetBool("isEmoting", false);
            }
        }
        else
        {
            animator.SetBool("isWalk", false);
            animator.SetBool("isRun", false);
        }
    }

    // =========================
    // JUMP
    // =========================

    private void HandleJump()
    {
        if (jumpPressed && isGrounded)
        {
            velocityY =
                Mathf.Sqrt(jumpForce * -2f * gravity);

            animator.SetTrigger("jump");
        }

        jumpPressed = false;
    }

    // =========================
    // PUNCH
    // =========================

    private void HandlePunch()
    {
        animator.SetBool("isPunch", isPunching);
    }

    // =========================
    // PUNCH 2
    // =========================

    private void HandlePunch2()
    {
        animator.SetBool("isPunch2", isPunching2);
    }

    // =========================
    // ULTIMATE
    // =========================

    private void HandleUltimate()
    {
        if (ultimatePressed)
        {
            animator.SetTrigger("ultimate");
        }

        ultimatePressed = false;
    }

    // =========================
    // JUMP ATTACK
    // =========================

    private void HandleJumpAttack()
    {
        if (jumpAttPressed)
        {
            animator.SetTrigger("jumpatt");
        }

        jumpAttPressed = false;
    }

    // =========================
    // EMOT
    // =========================

    private void HandleEmot()
    {
        animator.SetBool("isEmoting", isEmoting);
    }

    // =========================
    // BLOCK
    // =========================

    private void HandleBlock()
    {
        animator.SetBool("isBlock", isBlocking);
    }

    // =========================
    // GRAVITY
    // =========================

    private void ApplyGravity()
    {
        if (isGrounded && velocityY < 0)
        {
            velocityY = -2f;
        }

        velocityY += gravity * Time.deltaTime;

        Vector3 gravityMove =
            new Vector3(0, velocityY, 0);

        controller.Move(gravityMove * Time.deltaTime);
    }

    // =========================
    // ANIMATOR
    // =========================

    private void UpdateAnimator()
    {
        animator.SetBool("isGrounded", isGrounded);
    }
}