using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject panelMenu;

    private bool isPaused = false;

    private void Start()
    {
        // Pastikan saat game mulai, panel menu tertutup dan game jalan
        if (panelMenu != null)
        {
            panelMenu.SetActive(false);
        }
        Time.timeScale = 1f;
    }

    // Fungsi untuk membuka Menu dan Pause Game
    public void OpenMenu()
    {
        if (panelMenu != null)
        {
            panelMenu.SetActive(true);
            Time.timeScale = 0f; // Membekukan waktu game (NPC, animasi, pergerakan berhenti)
            isPaused = true;

            // Opsional: Buka kunci kursor mouse agar bisa klik UI jika game kamu tipe TPS lock cursor
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
            Time.timeScale = 1f; // Mengembalikan waktu game ke normal
            isPaused = false;

            // Opsional: Kunci kembali kursor mouse jika memakai mode TPS
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}