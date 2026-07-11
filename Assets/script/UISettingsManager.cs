using UnityEngine;
using UnityEngine.UI;

public class UISettingsManager : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private GameObject panelSetting; // Tempat menarik objek 'PanelSetting'

    [Header("Sound Effect UI")]
    [SerializeField] private Image soundButtonImage; // Objek Image untuk Sound Effect
    [SerializeField] private Sprite soundOnSprite;    // Sprite Sound On (Hijau)
    [SerializeField] private Sprite soundOffSprite;   // Sprite Sound Off (Merah)

    [Header("Music UI")]
    [SerializeField] private Image musicButtonImage; // Objek Image untuk Music
    [SerializeField] private Sprite musicOnSprite;    // Sprite Music On (Hijau)
    [SerializeField] private Sprite musicOffSprite;   // Sprite Music Off (Merah)

    private bool isSoundOn = true;
    private bool isMusicOn = true;

    private void Start()
    {
        // Jalankan fungsi saat game dimulai agar gambar awal kedua tombol langsung terpasang
        UpdateSoundUI();
        UpdateMusicUI();
    }

    // ==========================================
    // LOGIKA UNTUK SOUND EFFECT
    // ==========================================
    public void ToggleSoundEffect()
    {
        isSoundOn = !isSoundOn;
        UpdateSoundUI();

        if (isSoundOn) Debug.Log("Sound FX: ON");
        else Debug.Log("Sound FX: OFF");
    }

    private void UpdateSoundUI()
    {
        if (soundButtonImage != null && soundOnSprite != null && soundOffSprite != null)
        {
            soundButtonImage.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
        }
    }

    // ==========================================
    // LOGIKA UNTUK MUSIC
    // ==========================================
    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        UpdateMusicUI();

        if (isMusicOn) Debug.Log("Music: ON");
        else Debug.Log("Music: OFF");
    }

    private void UpdateMusicUI()
    {
        if (musicButtonImage != null && musicOnSprite != null && musicOffSprite != null)
        {
            musicButtonImage.sprite = isMusicOn ? musicOnSprite : musicOffSprite;
        }
    }

    // ==========================================
    // LOGIKA UNTUK CLOSE
    // ==========================================
    public void ClosePanelSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);
        }
    }
}