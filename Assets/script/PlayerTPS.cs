using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerTPS : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Gravity (Keep Grounded)")]
    [SerializeField] private float gravity = -9.81f;
    private float velocityY;

    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 0.5f;
    private int comboStep = 0;
    private bool combatInputBuffered = false;

    [Header("Hold Attack Settings")]
    [SerializeField] private float holdThreshold = 0.4f;
    private float attackPressedTime;
    private bool isAttackHolding;
    private bool attackInputPressed;

    [Header("Cooldowns Management")]
    private float skill1CDTimer = 0f;
    private float skill2CDTimer = 0f;
    private const float SKILL1_MAX_CD = 5f;
    private const float SKILL2_MAX_CD = 15f;

    [Header("Block Stamina Timers")]
    private float blockActiveTimer = 0f;
    private float blockStaminaSecCounter = 0f;

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
    private PlayerStatusManager statusManager;

    // INPUT FLAGS
    private bool ultimatePressed;
    private bool skillPressed;
    private bool isBlocking;
    public bool IsPlayerBlocking => isBlocking;
    private bool isRunning;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        statusManager = GetComponent<PlayerStatusManager>();
        inputActions = new TPS();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Dash.performed += OnDashPressed;
        inputActions.Player.Punch.performed += OnAttackPerformed;
        inputActions.Player.Punch.canceled += OnAttackCanceled;
        inputActions.Player.Skill.performed += OnSkillPressed;
        inputActions.Player.Ultimate.performed += OnUltimate;
        inputActions.Player.Block.performed += OnBlockStart;
        inputActions.Player.Block.canceled += OnBlockEnd;
        inputActions.Player.Run.performed += OnRunToggle;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Dash.performed -= OnDashPressed;
        inputActions.Player.Punch.performed -= OnAttackPerformed;
        inputActions.Player.Punch.canceled -= OnAttackCanceled;
        inputActions.Player.Skill.performed -= OnSkillPressed;
        inputActions.Player.Ultimate.performed -= OnUltimate;
        inputActions.Player.Block.performed -= OnBlockStart;
        inputActions.Player.Block.canceled -= OnBlockEnd;
        inputActions.Player.Run.performed -= OnRunToggle;
        inputActions.Disable();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        CheckGround();
        HandleMovement();
        HandleHoldAttackCheck();
        HandleComboStateCheck();
        HandleSkill();
        HandleUltimate();
        HandleBlockStaminaConsumption();
        ProcessCooldownTimers();
        ApplyGravity();

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isPunching = stateInfo.IsName("Punch") || stateInfo.IsName("Punch2") || stateInfo.IsName("Punch3");

        bool isDoingAction = isRunning || isBlocking || isPunching ||
                             stateInfo.IsName("Skill") || stateInfo.IsName("Ultimate") ||
                             stateInfo.IsName("Attack Press") || IsPerformingLockedAction();

        if (!isDoingAction)
        {
            if (moveInput.magnitude > 0.1f)
            {
                statusManager.RegenerateStamina(1f);
            }
            else
            {
                statusManager.RegenerateStamina(3f);
            }
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnRunToggle(InputAction.CallbackContext context)
    {
        // Hanya aktif lari jika punya stamina tersisa
        if (statusManager.CurrentStamina > 0)
        {
            isRunning = !isRunning;
        }
    }

    private void OnDashPressed(InputAction.CallbackContext context)
    {
        if (IsPerformingLockedAction()) return;

        if (isRunning && moveInput.magnitude > 0.1f)
        {
            if (statusManager.UseStamina(10f)) // Konsumsi Role = 10 Stamina
            {
                animator.SetTrigger("Role");
            }
        }
        else
        {
            if (statusManager.UseStamina(5f)) // Konsumsi Dash = 5 Stamina
            {
                animator.SetTrigger("Dash");
            }
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (isBlocking || IsPerformingLockedAction()) return;
        attackPressedTime = Time.time;
        attackInputPressed = true;
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        attackInputPressed = false;

        if (!isAttackHolding)
        {
            combatInputBuffered = true;
        }
        else
        {
            isAttackHolding = false;
            animator.SetBool("Attack Press", false);
        }
    }

    private void HandleHoldAttackCheck()
    {
        // 1. Pemicu awal saat tombol ditahan melewati batas waktu threshold
        if (attackInputPressed && !isAttackHolding)
        {
            if (Time.time - attackPressedTime >= holdThreshold)
            {
                isAttackHolding = true;
                animator.SetBool("Attack Press", true);
                Debug.Log("Pemicu Press Attack: Menghasilkan 15 Damage!");
                comboStep = 0;
                combatInputBuffered = false;
            }
        }

        // 2. LOGIKA BARU: Paksa balik ke Idle jika animasi Press Attack sudah selesai berjalan
        if (isAttackHolding)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Pastikan nama state di dalam tanda kutip di bawah ini SAMA PERSIS dengan nama State di Animator kamu
            if (stateInfo.IsName("Attack Press") || stateInfo.IsName("Base Layer.Attack Press"))
            {
                // Jika progres animasi sudah mencapai atau melewati 95% (hampir selesai)
                if (stateInfo.normalizedTime >= 0.95f)
                {
                    isAttackHolding = false; // Reset status holding di script
                    animator.SetBool("Attack Press", false); // Paksa Animator matikan bool agar transisi ke Idle aktif
                    Debug.Log("Animasi Press Attack selesai, memaksa kembali ke Idle.");
                }
            }
        }
    }

    private void HandleComboStateCheck()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Base Layer.Idle") || stateInfo.IsName("Idle") || stateInfo.tagHash == Animator.StringToHash("Locomotion"))
        {
            if (combatInputBuffered)
            {
                combatInputBuffered = false;
                comboStep = 1;
                animator.SetTrigger("Punch");
                Debug.Log("Pukulan Kombo 1: Menghasilkan 5 Damage!");
            }
            else
            {
                comboStep = 0;
            }
        }
        else if (stateInfo.IsName("Punch"))
        {
            if (stateInfo.normalizedTime >= 0.5f && combatInputBuffered && comboStep == 1)
            {
                combatInputBuffered = false;
                comboStep = 2;
                animator.SetTrigger("Punch2");
                Debug.Log("Pukulan Kombo 2: Menghasilkan 8 Damage!");
            }
            else if (stateInfo.normalizedTime >= 1f && !combatInputBuffered && comboStep == 1)
            {
                comboStep = 0;
            }
        }
        else if (stateInfo.IsName("Punch2"))
        {
            if (stateInfo.normalizedTime >= 0.5f && combatInputBuffered && comboStep == 2)
            {
                combatInputBuffered = false;
                comboStep = 3;
                animator.SetTrigger("Punch3");
                Debug.Log("Pukulan Kombo 3: Menghasilkan 10 Damage!");
            }
            else if (stateInfo.normalizedTime >= 1f && !combatInputBuffered && comboStep == 2)
            {
                comboStep = 0;
            }
        }
        else if (stateInfo.IsName("Punch3"))
        {
            if (stateInfo.normalizedTime >= 0.9f)
            {
                comboStep = 0;
                combatInputBuffered = false;
            }
        }
    }

    private void OnSkillPressed(InputAction.CallbackContext context)
    {
        if (IsPerformingLockedAction() || isBlocking || skill1CDTimer > 0) return;
        skillPressed = true;
    }

    private void HandleSkill()
    {
        if (skillPressed)
        {
            animator.SetTrigger("Skill");
            Debug.Log("Skill 1 Aktif: Menghasilkan 25 Damage!");
            skill1CDTimer = SKILL1_MAX_CD; // Set CD 5 detik
        }
        skillPressed = false;
    }

    private void OnUltimate(InputAction.CallbackContext context)
    {
        if (IsPerformingLockedAction() || isBlocking || skill2CDTimer > 0) return;
        ultimatePressed = true;
    }

    private void HandleUltimate()
    {
        if (ultimatePressed)
        {
            animator.SetTrigger("Ultimate");
            Debug.Log("Skill Ultimate Aktif: Menghasilkan 50 Damage!");
            skill2CDTimer = SKILL2_MAX_CD; // Set CD 15 detik
        }
        ultimatePressed = false;
    }

    // BLOCK INPUT & TIMING STAMINA MANAGEMENT
    private void OnBlockStart(InputAction.CallbackContext context)
    {
        if (IsPerformingLockedAction() || statusManager.IsBlockBroken) return;

        // Klik awal mengonsumsi 5 stamina langsung
        statusManager.ReduceStaminaDirect(5f);
        isBlocking = true;
        blockActiveTimer = 0f;
        blockStaminaSecCounter = 0f;
    }

    private void OnBlockEnd(InputAction.CallbackContext context)
    {
        isBlocking = false;
        animator.SetBool("isBlock", false);
        statusManager.ResetBlockSustainedDamage();
    }

    private void HandleBlockStaminaConsumption()
    {
        if (isBlocking)
        {
            // Jika ditengah jalan block hancur/broken akibat diserang melebihi 50 damage
            if (statusManager.IsBlockBroken)
            {
                isBlocking = false;
                animator.SetBool("isBlock", false);
                return;
            }

            animator.SetBool("isBlock", true);
            blockActiveTimer += Time.deltaTime;

            // Masuk hitungan detik setelah melewati batas threshold 3 detik awal
            if (blockActiveTimer >= 3f)
            {
                blockStaminaSecCounter += Time.deltaTime;
                if (blockStaminaSecCounter >= 1f)
                {
                    statusManager.ReduceStaminaDirect(3f); // Potong 3 stamina tiap detiknya
                    blockStaminaSecCounter = 0f;
                }
            }

            // Jika stamina habis total saat menahan block, otomatis paksa lepas block
            if (statusManager.CurrentStamina <= 0)
            {
                isBlocking = false;
                animator.SetBool("isBlock", false);
            }
        }
    }

    private void ProcessCooldownTimers()
    {
        if (skill1CDTimer > 0)
        {
            skill1CDTimer -= Time.deltaTime;
            statusManager.UpdateSkillCDUI(skill1CDTimer, SKILL1_MAX_CD, 1);
        }
        if (skill2CDTimer > 0)
        {
            skill2CDTimer -= Time.deltaTime;
            statusManager.UpdateSkillCDUI(skill2CDTimer, SKILL2_MAX_CD, 2);
        }
    }

    private void HandleMovement()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isDashing = stateInfo.IsName("Dash");
        bool isRolling = stateInfo.IsName("Role");

        if (isDashing || isRolling)
        {
            animator.SetBool("isWalk", false);
            animator.SetBool("isRun", false);
            float dashSpeed = isRolling ? 9f : 7f;
            Vector3 forwardMove = transform.forward * dashSpeed;
            controller.Move(forwardMove * Time.deltaTime);
            return;
        }

        bool isPunching = stateInfo.IsName("Punch") || stateInfo.IsName("Punch2") || stateInfo.IsName("Punch3");
        if (isBlocking || stateInfo.IsName("Skill") || stateInfo.IsName("Ultimate") || stateInfo.IsName("Attack Press") || isPunching)
        {
            animator.SetBool("isWalk", false);
            animator.SetBool("isRun", false);
            return;
        }

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        if (move.magnitude > 0.1f)
        {
            // Jika sedang berlari, kurangi stamina 5 per detik secara konstan
            if (isRunning)
            {
                statusManager.ReduceStaminaDirect(5f * Time.deltaTime);
                if (statusManager.CurrentStamina <= 0) isRunning = false; // Berhenti lari jika lelah
            }

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0; camRight.y = 0;

            Vector3 moveDirection = camForward * move.z + camRight * move.x;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            float currentSpeed = isRunning ? runSpeed : moveSpeed;
            controller.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);

            animator.SetBool("isWalk", !isRunning);
            animator.SetBool("isRun", isRunning);
        }
        else
        {
            animator.SetBool("isWalk", false);
            animator.SetBool("isRun", false);
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocityY < 0) velocityY = -2f;
        velocityY += gravity * Time.deltaTime;
        Vector3 gravityMove = new Vector3(0, velocityY, 0);
        controller.Move(gravityMove * Time.deltaTime);
    }

    private bool IsPerformingLockedAction()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Skill") || stateInfo.IsName("Ultimate") || stateInfo.IsName("Dash") || stateInfo.IsName("Role");
    }
}