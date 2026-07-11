using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private float staminaRegenRate = 1f; // Isi 1 stamina tiap 1 detik jika tidak gerak

    [Header("UI Status Bars")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("UI Cooldown Images (Radial 360)")]
    [SerializeField] private Image skillCDImage;      // Pasang Image penutup Gelap Skill 1
    [SerializeField] private Image ultimateCDImage;   // Pasang Image penutup Gelap Ultimate
    [SerializeField] private Image blockCDImage;      // Pasang Image penutup Gelap Block

    // Properti untuk diakses oleh script PlayerTPS
    public float CurrentStamina => currentStamina;
    public bool IsBlockBroken => isBlockBroken;

    private void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        UpdateStatusUI();
    }

    private void Update()
    {
        HandleCooldowns();
    }

    // Dipanggil oleh PlayerTPS secara konstan dari Update jika player TIDAK bergerak
    // Diupdate agar bisa menerima angka regen yang berbeda (misal: 3 saat idle, 1 saat jalan)
    public void RegenerateStamina(float customRegenRate)
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += customRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            UpdateStatusUI();
        }
    }

    public void ReduceStaminaDirect(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        UpdateStatusUI();
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            UpdateStatusUI();
            return true;
        }
        return false;
    }

    // SYSTEM DAMAGE & BLOCK PROTECTION
    public void TakeDamage(float damageAmount, bool isCurrentlyBlocking)
    {
        if (isCurrentlyBlocking && !isBlockBroken)
        {
            blockDamageSustained += damageAmount;
            Debug.Log($"Block menahan damage. Total tertahan: {blockDamageSustained}/50");

            if (blockDamageSustained > 50f)
            {
                isBlockBroken = true;
                blockCooldownTimer = 10f; // CD 10 detik karena shield hancur
                blockDamageSustained = 0f;
                Debug.Log("Block Hancur! Player terkena break stun/hit.");

                // Sisa damage masuk ke darah player
                float pierceDamage = blockDamageSustained - 50f;
                ApplyDirectDamage(pierceDamage);
            }
        }
        else
        {
            ApplyDirectDamage(damageAmount);
        }
    }

    private void ApplyDirectDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateStatusUI();

        if (currentHealth <= 0) Debug.Log("Player Mati!");
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
                // RUMUS 0 KE 1: Nilai makin naik seiring waktu CD berjalan
                blockCDImage.fillAmount = 1f - (blockCooldownTimer / 10f);
            }
            if (blockCooldownTimer <= 0) isBlockBroken = false;
        }
        else if (blockCDImage != null)
        {
            // Saat tidak CD, direset ke 0 (atau ke 1 jika kamu ingin dia tetap penuh saat aktif)
            blockCDImage.fillAmount = 0f;
        }
    }

    // Sinkronisasi radial UI luar untuk skill dari PlayerTPS
    public void UpdateSkillCDUI(float currentCD, float maxCD, int skillType)
    {
        Image targetImg = (skillType == 1) ? skillCDImage : ultimateCDImage;
        if (targetImg != null)
        {
            // RUMUS 0 KE 1: Saat diklik langsung mulai dari 0, lalu naik ke 1
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