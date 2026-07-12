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

        // 1. KUNCI UTAMA: Jika player sedang mati atau terkena Hit (CanAction == false),
        // matikan paksa pergerakan, bersihkan input buff, dan abaikan fungsi aksi di bawahnya.
        if (statusManager != null && !statusManager.CanAction)
        {
            animator.SetBool("isWalk", false);
            animator.SetBool("isRun", false);
            isRunning = false;
            isBlocking = false;
            animator.SetBool("isBlock", false);
            combatInputBuffered = false;
            attackInputPressed = false;
            isAttackHolding = false;

            ApplyGravity(); // Gravitasi tetap berjalan agar tidak melayang saat kena hit
            return;
        }

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
        if (statusManager != null && !statusManager.CanAction)
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnRunToggle(InputAction.CallbackContext context)
    {
        if (statusManager != null && !statusManager.CanAction) return;

        if (statusManager.CurrentStamina > 0)
        {
            isRunning = !isRunning;
        }
    }

    private void OnDashPressed(InputAction.CallbackContext context)
    {
        if (statusManager != null && !statusManager.CanAction) return;
        if (IsPerformingLockedAction()) return;

        if (isRunning && moveInput.magnitude > 0.1f)
        {
            if (statusManager.UseStamina(10f))
            {
                animator.SetTrigger("Role");
            }
        }
        else
        {
            if (statusManager.UseStamina(5f))
            {
                animator.SetTrigger("Dash");
            }
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (statusManager != null && !statusManager.CanAction) return;
        if (isBlocking || IsPerformingLockedAction()) return;

        attackPressedTime = Time.time;
        attackInputPressed = true;
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        if (statusManager != null && !statusManager.CanAction) return;

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
        if (attackInputPressed && !isAttackHolding)
        {
            if (Time.time - attackPressedTime >= holdThreshold)
            {
                isAttackHolding = true;
                animator.SetBool("Attack Press", true);
                Debug.Log("Pemicu Press Attack: Menghasilkan 15 Damage!");

                DealDamageInFront(15f);

                comboStep = 0;
                combatInputBuffered = false;
            }
        }

        if (isAttackHolding)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("Attack Press") || stateInfo.IsName("Base Layer.Attack Press"))
            {
                if (stateInfo.normalizedTime >= 0.95f)
                {
                    isAttackHolding = false;
                    animator.SetBool("Attack Press", false);
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

                // --- TAMBAHKAN INI ---
                DealDamageInFront(5f);
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

                // --- TAMBAHKAN INI ---
                DealDamageInFront(8f);
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

                // --- TAMBAHKAN INI ---
                DealDamageInFront(10f);
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
        if (statusManager != null && !statusManager.CanAction) return;
        if (IsPerformingLockedAction() || isBlocking || skill1CDTimer > 0) return;

        skillPressed = true;
    }

    private void HandleSkill()
    {
        if (skillPressed)
        {
            animator.SetTrigger("Skill");
            Debug.Log("Skill 1 Aktif: Menghasilkan 25 Damage!");

            // --- TAMBAHKAN INI ---
            DealDamageInFront(25f);

            skill1CDTimer = SKILL1_MAX_CD;
        }
        skillPressed = false;
    }

    private void OnUltimate(InputAction.CallbackContext context)
    {
        if (statusManager != null && !statusManager.CanAction) return;
        if (IsPerformingLockedAction() || isBlocking || skill2CDTimer > 0) return;

        ultimatePressed = true;
    }

    private void HandleUltimate()
    {
        if (ultimatePressed)
        {
            animator.SetTrigger("Ultimate");
            Debug.Log("Skill Ultimate Aktif: Menghasilkan 50 Damage!");

            // --- TAMBAHKAN INI ---
            DealDamageInFront(50f);

            skill2CDTimer = SKILL2_MAX_CD;
        }
        ultimatePressed = false;
    }

    private void OnBlockStart(InputAction.CallbackContext context)
    {
        if (statusManager != null && !statusManager.CanAction) return;
        if (IsPerformingLockedAction() || statusManager.IsBlockBroken) return;

        statusManager.ReduceStaminaDirect(5f);
        isBlocking = true;
        blockActiveTimer = 0f;
        blockStaminaSecCounter = 0f;
    }

    private void OnBlockEnd(InputAction.CallbackContext context)
    {
        // Tetap izinkan lepas block kapan saja demi keamanan state
        isBlocking = false;
        animator.SetBool("isBlock", false);
        if (statusManager != null) statusManager.ResetBlockSustainedDamage();
    }

    private void HandleBlockStaminaConsumption()
    {
        if (isBlocking)
        {
            if (statusManager.IsBlockBroken)
            {
                isBlocking = false;
                animator.SetBool("isBlock", false);
                return;
            }

            animator.SetBool("isBlock", true);
            blockActiveTimer += Time.deltaTime;

            if (blockActiveTimer >= 3f)
            {
                blockStaminaSecCounter += Time.deltaTime;
                if (blockStaminaSecCounter >= 1f)
                {
                    statusManager.ReduceStaminaDirect(3f);
                    blockStaminaSecCounter = 0f;
                }
            }

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
            if (isRunning)
            {
                statusManager.ReduceStaminaDirect(5f * Time.deltaTime);
                if (statusManager.CurrentStamina <= 0) isRunning = false;
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

        // 2. TAMBAHKAN PENGECEKAN ANIMASI HIT DI SINI:
        // Jika sedang memutar animasi hit, sistem menganggap player sedang mengunci aksi
        return stateInfo.IsName("Skill") ||
               stateInfo.IsName("Ultimate") ||
               stateInfo.IsName("Dash") ||
               stateInfo.IsName("Role") ||
               stateInfo.IsName("Hit Body") ||
               stateInfo.IsName("Head Hit") ||
               stateInfo.IsName("Hit Back") ||
               stateInfo.IsName("Knockdown") ||
               stateInfo.IsName("StandUp");
    }

    private void DealDamageInFront(float damageAmount)
    {
        Vector3 attackCenter = transform.position + transform.forward * 1.5f;
        Collider[] hitColliders = Physics.OverlapSphere(attackCenter, 2f);

        foreach (Collider hit in hitColliders)
        {
            // 1. Cek apakah Mutant
            MutantAI mutant = hit.GetComponent<MutantAI>();
            if (mutant != null)
            {
                mutant.TakeDamageFromPlayer(damageAmount);
                return; // Keluar agar tidak memukul objek lain di frame yang sama
            }

            // 2. Cek apakah Police (TAMBAHAN BARU)
            PoliceAI police = hit.GetComponent<PoliceAI>();
            if (police != null)
            {
                police.TakeDamageFromPlayer(damageAmount);
                return;
            }
        }
    }
}