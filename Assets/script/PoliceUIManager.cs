using UnityEngine;
using UnityEngine.UI;

public class PoliceUIManager : MonoBehaviour
{
    public static PoliceUIManager Instance;

    [Header("Police UI")]
    [SerializeField] private GameObject healthBarPanel;
    [SerializeField] private Slider hpSlider;

    private PoliceAI currentActivePolice;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (healthBarPanel != null)
            healthBarPanel.SetActive(false);
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
}