using UnityEngine;
using UnityEngine.UI;

public class MutantUIManager : MonoBehaviour
{
    public static MutantUIManager Instance;

    [Header("Mutant UI")]
    [SerializeField] private GameObject healthBarPanel;
    [SerializeField] private Slider hpSlider;

    private MutantAI currentActiveMutant;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (healthBarPanel != null)
            healthBarPanel.SetActive(false);
    }

    public void SetActiveMutant(MutantAI mutant)
    {
        if (mutant == null) return;

        currentActiveMutant = mutant;

        if (healthBarPanel != null)
            healthBarPanel.SetActive(true);

        if (hpSlider != null)
        {
            hpSlider.maxValue = 80f;
            hpSlider.value = mutant.GetCurrentHealth();
        }
    }

    public void UpdateActiveMutant(MutantAI mutant)
    {
        if (mutant == null) return;

        if (currentActiveMutant != mutant)
            return;

        if (hpSlider != null)
            hpSlider.value = mutant.GetCurrentHealth();

        if (mutant.IsDeadEnemy())
            ClearActiveMutant(mutant);
    }

    public void ClearActiveMutant(MutantAI mutant)
    {
        if (currentActiveMutant != mutant)
            return;

        currentActiveMutant = null;

        if (healthBarPanel != null)
            healthBarPanel.SetActive(false);
    }
}