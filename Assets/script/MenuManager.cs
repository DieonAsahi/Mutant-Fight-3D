using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelWin; // Tambahan slot untuk menyeret Panel Win di Inspector

    private bool isPaused = false;

    private void Start()
    {
        // Pastikan saat game mulai, semua panel tertutup dan game jalan
        if (panelMenu != null)
        {
            panelMenu.SetActive(false);
        }

        if (panelWin != null)
        {
            panelWin.SetActive(false); // Memastikan Panel Win tidak muncul di awal game
        }

        Time.timeScale = 1f;
    }

    // Fungsi Tambahan: Dipanggil otomatis oleh PoliceUIManager saat semua polisi mati
    public void OpenWinPanel()
    {
        if (panelWin != null)
        {
            panelWin.SetActive(true); // Memunculkan Panel Win (Misi Selesai)
            Time.timeScale = 0f;      // Hentikan pergerakan/waktu game di background
            isPaused = true;

            // Buka kunci kursor mouse agar bisa klik tombol Next Level / Main Menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("Panel Win berhasil ditampilkan melalui MenuManager.");
        }
        else
        {
            Debug.LogError("Panel Win belum dimasukkan ke slot Inspector MenuManager!");
        }
    }

    // Fungsi untuk membuka Menu dan Pause Game biasa
    public void OpenMenu()
    {
        if (panelMenu != null)
        {
            panelMenu.SetActive(true);
            Time.timeScale = 0f;
            isPaused = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Fungsi untuk melanjutkan game (Dipakai di tombol Continue)
    public void ContinueGame()
    {
        if (panelMenu != null)
        {
            panelMenu.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}