using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerStatusManager : MonoBehaviour
{
    [Header("Health & Block Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    private float blockDamageSustained = 0f;
    private bool isBlockBroken = false;
    private float blockCooldownTimer = 0f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    private float currentStamina;
    private float staminaRegenRate = 1f;

    private int comboHitCount = 0;       // Menyimpan sudah berapa kali dipukul berturut-turut
    private float lastHitTime = 0f;      // Menyimpan waktu terakhir dipukul untuk reset combo
    [SerializeField] private float comboResetDelay = 2.0f;

    [Header("UI Status Bars")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("UI Cooldown Images (Radial 360)")]
    [SerializeField] private Image skillCDImage;
    [SerializeField] private Image ultimateCDImage;
    [SerializeField] private Image blockCDImage;

    [Header("Game Over / Restart UI")]
    [SerializeField] private GameObject restartPanel;

    private Animator anim;
    private bool isDead = false;
    private bool isStunned = false;
    private Coroutine currentHitStunCoroutine;
    public float CurrentStamina => currentStamina;
    public bool IsBlockBroken => isBlockBroken;
    public bool IsDead => isDead;

    // Properti baru agar PlayerTPS bisa tahu kapan harus mematikan input tombol/joystick
    public bool CanAction => !isDead && !isStunned;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();

        if (restartPanel != null)
            restartPanel.SetActive(false);

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        UpdateStatusUI();
    }

    private void Update()
    {
        if (isDead) return;
        HandleCooldowns();
    }

    public void RegenerateStamina(float customRegenRate)
    {
        if (!CanAction) return; // Tidak bisa regen kalau lagi kaku/kena hit
        if (currentStamina < maxStamina)
        {
            currentStamina += customRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            UpdateStatusUI();
        }
    }

    public void ReduceStaminaDirect(float amount)
    {
        if (isDead) return;
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        UpdateStatusUI();
    }

    public bool UseStamina(float amount)
    {
        if (!CanAction) return false; // Tombol stamina mati kalau lagi kena hit
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            UpdateStatusUI();
            return true;
        }
        return false;
    }

    // UPDATE DI PLAYERSTATUSMANAGER.CS

    public void TakeDamage(float damageAmount, bool isCurrentlyBlocking, string attackerType = "Normal", string hitDirection = "Front")
    {
        if (isDead) return;

        // Reset hit counter jika jeda pukulan musuh sudah terlalu lama
        if (Time.time - lastHitTime > comboResetDelay)
        {
            comboHitCount = 0;
        }

        lastHitTime = Time.time; // Catat waktu hit terakhir

        // 1. KONDISI KETIKA PLAYER SEDANG BLOCKING
        if (isCurrentlyBlocking && !isBlockBroken)
        {
            blockDamageSustained += damageAmount;
            Debug.Log($"Block menahan damage. Total: {blockDamageSustained}/50");

            if (attackerType == "Boss")
            {
                isBlockBroken = true;
                blockCooldownTimer = 10f;
                blockDamageSustained = 0f;
                ApplyDirectDamage(damageAmount * 0.5f);
                PlayHitReaction(4, 4.0f); // Knockdown Boss (4)
                return;
            }

            if (blockDamageSustained > 50f)
            {
                isBlockBroken = true;
                blockCooldownTimer = 10f;
                blockDamageSustained = 0f;
                ApplyDirectDamage(damageAmount);
                PlayHitReaction(7, 4.0f); // Knockdown NPC (7)
                return;
            }
        }
        // 2. KONDISI KETIKA TIDAK BLOCKING (ATAU BLOCK BROKEN)
        else
        {
            ApplyDirectDamage(damageAmount);

            // Jika setelah damage masuk Player langsung mati, tidak perlu jalankan combo stun biasa
            if (isDead) return;

            // Tambah hitungan pukulan yang masuk
            comboHitCount++;

            if (attackerType == "Boss")
            {
                if (hitDirection == "JumpAttack")
                {
                    PlayHitReaction(5, 4.0f); // StandUp Boss (5)
                }
                else if (hitDirection == "Back")
                {
                    PlayHitReaction(3, 1.0f); // Hit Back Boss (3)
                }
                else
                {
                    if (comboHitCount >= 5)
                    {
                        PlayHitReaction(4, 3.5f); // Hit ke-5 langsung Knockdown Boss (4)
                        comboHitCount = 0;        // Reset combo
                    }
                    else if (comboHitCount == 4)
                    {
                        PlayHitReaction(3, 1.2f); // Hit ke-4 terkena Hit Back Boss (3)
                    }
                    else if (comboHitCount == 3)
                    {
                        PlayHitReaction(2, 1.0f); // Hit ke-3 terkena Hit Body Boss (2)
                    }
                    else
                    {
                        int randomBossHit = (Random.value > 0.5f) ? 1 : 2;
                        PlayHitReaction(randomBossHit, 0.9f);
                    }
                }
            }
            else
            {
                // LOGIKA HIT DARI NPC BIASA (POLISI / MUTANT)
                if (hitDirection == "Back")
                {
                    PlayHitReaction(5, 1.0f); // Hit Back NPC (5)
                }
                else
                {
                    if (comboHitCount >= 5)
                    {
                        PlayHitReaction(7, 3.5f); // Hit ke-5 langsung Knockdown NPC (7)
                        comboHitCount = 0;        // Reset combo
                    }
                    else if (comboHitCount == 4)
                    {
                        PlayHitReaction(5, 1.2f); // Hit ke-4 terkena Hit Back NPC (5)
                    }
                    else if (comboHitCount == 3)
                    {
                        PlayHitReaction(3, 1.0f); // Hit ke-3 terkena Hit Body NPC (3)
                    }
                    else
                    {
                        int randomNpcHit = (Random.value > 0.5f) ? 1 : 3;
                        PlayHitReaction(randomNpcHit, 0.9f);
                    }
                }
            }
        }
    }

    private void ApplyDirectDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateStatusUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitStunRoutine(int hitTypeIndex, float duration)
    {
        isStunned = true;

        if (anim != null)
        {
            // Atur parameter isBoss di Animator
            if (hitTypeIndex == 2 || hitTypeIndex == 3 || hitTypeIndex == 4 || hitTypeIndex == 5)
            {
                if (hitTypeIndex == 3 && comboHitCount <= 3 && !anim.GetBool("isBoss"))
                    anim.SetBool("isBoss", false);
                else if (hitTypeIndex == 5 && comboHitCount == 4 && !anim.GetBool("isBoss"))
                    anim.SetBool("isBoss", false);
                else
                    anim.SetBool("isBoss", true);
            }
            else if (hitTypeIndex == 7)
            {
                anim.SetBool("isBoss", false);
            }

            // 1. Set parameter integer nilai HitType
            anim.SetInteger("HitType", hitTypeIndex);

            // 2. TRIGGER parameter "Hit" yang ada di Animator Unity kamu!
            anim.SetTrigger("Hit");

            // Jalankan manual secara paksa via anim.Play sebagai backup/override
            string stateName = "";
            switch (hitTypeIndex)
            {
                case 1: stateName = "Head Hit"; break;
                case 2: stateName = "Hit Body"; break;
                case 3: stateName = "Hit Body"; break;
                case 4: stateName = "Knockdown"; break;
                case 5:
                    stateName = anim.GetBool("isBoss") ? "StandUp" : "Hit Back";
                    break;
                case 7: stateName = "Knockdown"; break;
            }

            if (!string.IsNullOrEmpty(stateName))
            {
                anim.Play(stateName, 0, 0f);
            }
        }

        yield return new WaitForSeconds(duration);

        if (hitTypeIndex == 5 && anim.GetBool("isBoss"))
        {
            if (anim != null) anim.SetTrigger("StandUp");
            yield return new WaitForSeconds(1.5f);
        }

        isStunned = false;
    }

    // PASTIKAN DEKLARASINYA MEMAKAI INT INDEX, BUKAN STRING TRIGGERNAME
    public void PlayHitReaction(int hitTypeIndex, float lockDuration)
    {
        if (isDead) return;

        // Jika sedang stunned, hentikan coroutine stun yang lama agar bisa diganti dengan animasi hit combo berikutnya
        if (isStunned && currentHitStunCoroutine != null)
        {
            StopCoroutine(currentHitStunCoroutine);
        }

        currentHitStunCoroutine = StartCoroutine(HitStunRoutine(hitTypeIndex, lockDuration));
    }
    private void Die()
    {
        // Hentikan coroutine stun lama (jika ada)
        StopAllCoroutines();

        isDead = true;
        Debug.Log("Player Mati!");

        if (anim != null)
        {
            anim.Play("Daying");
        }

        // Mematikan kontrol pergerakan
        PlayerTPS movement = GetComponent<PlayerTPS>();
        if (movement != null) movement.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // --- GANTI DENGAN INVOKE ---
        // Memanggil fungsi OpenRestartPanel setelah jeda 3.5 detik murni
        Invoke("OpenRestartPanel", 3.5f);
    }

    // Fungsi baru untuk membuka panel tanpa coroutine
    private void OpenRestartPanel()
    {
        if (restartPanel != null)
        {
            restartPanel.SetActive(true); // Memunculkan Panel Restart ke layar
            Debug.Log("Restart Panel berhasil diaktifkan melalui Invoke!");

            // Membebas kursor mouse agar bisa diklik pada UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogError("Gagal memunculkan panel! Referensi 'restartPanel' di Inspector kosong / null.");
        }
    }

    private IEnumerator ShowRestartPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (restartPanel != null)
        {
            restartPanel.SetActive(true); // Memunculkan Panel Restart ke layar

            // Membebaskan kursor mouse agar bisa diklik pada UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResetBlockSustainedDamage()
    {
        if (!isBlockBroken) blockDamageSustained = 0f;
    }

    private void HandleCooldowns()
    {
        if (blockCooldownTimer > 0)
        {
            blockCooldownTimer -= Time.deltaTime;
            if (blockCDImage != null)
            {
                blockCDImage.fillAmount = 1f - (blockCooldownTimer / 10f);
            }
            if (blockCooldownTimer <= 0) isBlockBroken = false;
        }
        else if (blockCDImage != null)
        {
            blockCDImage.fillAmount = 0f;
        }
    }

    public void UpdateSkillCDUI(float currentCD, float maxCD, int skillType)
    {
        if (isDead) return;
        Image targetImg = (skillType == 1) ? skillCDImage : ultimateCDImage;
        if (targetImg != null)
        {
            targetImg.fillAmount = 1f - (currentCD / maxCD);
        }
    }

    private void UpdateStatusUI()
    {
        if (healthBarFill != null) healthBarFill.fillAmount = currentHealth / maxHealth;
        if (staminaBarFill != null) staminaBarFill.fillAmount = currentStamina / maxStamina;
        if (healthText != null) healthText.text = Mathf.CeilToInt(currentHealth) + "/" + maxHealth;
        if (staminaText != null) staminaText.text = Mathf.CeilToInt(currentStamina) + "/" + maxStamina;
    }
}