using UnityEngine;
using UnityEngine.UI;

public class PoliceUIManager : MonoBehaviour
{
    public static PoliceUIManager Instance;

    [Header("Police UI")]
    [SerializeField] private GameObject healthBarPanel;
    [SerializeField] private Slider hpSlider;

    [Header("Win System Settings")]
    [SerializeField] private MenuManager menuManager; // Tambahan: Masukkan object MenuManager ke sini di Inspector
    private bool levelCleared = false;
    private float checkDelay = 0.5f; // Cek setiap 0.5 detik sekali
    private float timer = 0f;

    private PoliceAI currentActivePolice;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (healthBarPanel != null)
            healthBarPanel.SetActive(false);

        // Otomatis mencari MenuManager di scene jika lupa dipasang di Inspector
        if (menuManager == null)
        {
            menuManager = Object.FindFirstObjectByType<MenuManager>();
        }
    }

    private void Update()
    {
        // Tambahan: Logika monitoring jumlah polisi di scene secara berkala
        if (!levelCleared)
        {
            timer += Time.deltaTime;
            if (timer >= checkDelay)
            {
                timer = 0f;
                CheckRemainingPolice();
            }
        }
    }

    public void SetActivePolice(PoliceAI police)
    {
        if (police == null) return;

        currentActivePolice = police;

        if (healthBarPanel != null)
            healthBarPanel.SetActive(true);

        if (hpSlider != null)
        {
            hpSlider.maxValue = police.MaxHealth;
            hpSlider.value = police.CurrentHealth;
        }
    }

    public void UpdateActivePolice(PoliceAI police)
    {
        if (police == null) return;

        if (currentActivePolice != police)
            return;

        if (hpSlider != null)
            hpSlider.value = police.CurrentHealth;

        if (police.CurrentHealth <= 0)
            ClearActivePolice(police);
    }

    public void ClearActivePolice(PoliceAI police)
    {
        if (currentActivePolice != police)
            return;

        currentActivePolice = null;

        if (healthBarPanel != null)
            healthBarPanel.SetActive(false);
    }

    // ================== FUNGSI TAMBAHAN UNTUK WIN SYSTEM ==================
    // Wajib ada kata 'public' di paling depan agar bisa diakses script lain!
    public void CheckRemainingPolice()
    {
        PoliceAI[] allPolice = Object.FindObjectsByType<PoliceAI>(FindObjectsSortMode.None);

        int activePoliceCount = 0;

        foreach (PoliceAI police in allPolice)
        {
            // Pengecekan berlapis: Pastikan objek ada, script aktif, dan darahnya masih di atas 0
            if (police != null && police.enabled && police.CurrentHealth > 0)
            {
                activePoliceCount++;
            }
        }

        Debug.Log($"[DIAGNOSIS] Jumlah polisi aktif terdeteksi: {activePoliceCount}");

        if (activePoliceCount == 0)
        {
            levelCleared = true;
            TriggerWinScreen(); // Panel Win akan terbuka sekarang!
        }
    }

    private void TriggerWinScreen()
    {
        Debug.Log("Semua polisi telah dikalahkan! Misi Selesai.");

        if (menuManager != null)
        {
            // Panggil fungsi untuk memunculkan Panel Win dari MenuManager.
            // CATATAN: Silakan ganti "OpenWinPanel()" di bawah ini dengan nama fungsi 
            // yang ada di dalam script MenuManager-mu (misal: ShowWinPanel, WinGame, dll.)
            menuManager.OpenWinPanel();
        }
        else
        {
            Debug.LogError("MenuManager tidak ditemukan! Tarik object Menu ke slot di Inspector.");
        }
    }
}