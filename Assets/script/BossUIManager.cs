using UnityEngine;
using UnityEngine.UI;

public class BossUIManager : MonoBehaviour
{
    public static BossUIManager Instance { get; private set; }

    [Header("Boss UI")]
    [SerializeField] private GameObject healthBarPanel;
    [SerializeField] private Slider hpSlider;

    [Header("Win System Settings")]
    [SerializeField] private MenuManager menuManager;

    private BossAI currentActiveBoss;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Pastikan saat game mulai, panel darah bos belum muncul (nanti muncul saat terdeteksi)
        if (healthBarPanel != null)
            healthBarPanel.SetActive(false);

        if (menuManager == null)
        {
            menuManager = Object.FindFirstObjectByType<MenuManager>();
        }
    }

    public void SetActiveBoss(BossAI boss)
    {
        if (boss == null) return;

        currentActiveBoss = boss;

        if (healthBarPanel != null)
            healthBarPanel.SetActive(true);

        if (hpSlider != null)
        {
            hpSlider.maxValue = boss.maxHealth;
            hpSlider.value = boss.currentHealth;
        }
    }

    public void UpdateActiveBoss(BossAI boss)
    {
        if (boss == null || currentActiveBoss != boss) return;

        if (hpSlider != null)
            hpSlider.value = boss.currentHealth;

        // Jika darah bos habis
        if (boss.currentHealth <= 0)
        {
            // 1. Pemicu layar kemenangan terlebih dahulu
            TriggerWinScreen();

            // 2. Bersihkan status aktif bos dan sembunyikan Health Bar-nya
            ClearActiveBoss(boss);
        }
    }
    public void ClearActiveBoss(BossAI boss)
    {
        if (currentActiveBoss != boss) return;

        currentActiveBoss = null;

        if (healthBarPanel != null)
            healthBarPanel.SetActive(false);
    }

    private void TriggerWinScreen()
    {
        Debug.Log("Bos Doctore telah dikalahkan! Memunculkan Panel Win...");
        if (menuManager != null)
        {
            menuManager.OpenWinPanel();
        }
        else
        {
            Debug.LogError("MenuManager belum dipasang pada BossUIManager!");
        }
    }
}