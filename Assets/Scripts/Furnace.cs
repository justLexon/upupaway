using UnityEngine;
using UnityEngine.InputSystem;

public class Furnace : MonoBehaviour
{
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float startingHeat = 50f;
    [SerializeField] private float heatPerCoal = 20f;
    [SerializeField] private float baseBurnRate = 4f;       // heat lost per second, always
    [SerializeField] private float burnRatePerHeat = 0.04f; // extra loss per point of heat (hotter burns faster)

    public float Heat { get; private set; }
    public float MaxHeat => maxHeat;
    public float Heat01 => Heat / maxHeat;

    private void Awake()
    {
        Heat = startingHeat;
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        // TEMP (Phase 1): Space adds coal. Phase 2 replaces this with clicking the furnace.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            AddCoal();

        float burn = baseBurnRate + Heat * burnRatePerHeat;
        Heat = Mathf.Max(0f, Heat - burn * Time.deltaTime);
    }

    public void AddCoal()
    {
        if (Heat >= maxHeat) return; // don't waste coal on a full furnace
        if (!ResourceManager.Instance.TrySpendCoal()) return;
        Heat = Mathf.Min(maxHeat, Heat + heatPerCoal);
    }
}
