using System.Collections;
using UnityEngine;

public class Furnace : MonoBehaviour, IClickable
{
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float startingHeat = 50f;
    [SerializeField] private float heatPerCoal = 20f;
    [SerializeField] private float baseBurnRate = 4f;       // heat lost per second, always
    [SerializeField] private float burnRatePerHeat = 0.04f; // extra loss per point of heat (hotter burns faster)
    [SerializeField] private Color coldColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color hotColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private SpriteRenderer spriteRenderer;

    public float Heat { get; private set; }
    public float MaxHeat => maxHeat;
    public float Heat01 => Heat / maxHeat;
    public event System.Action OnCoalAdded;

    private Vector3 baseScale;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        baseScale = transform.localScale;
        Heat = startingHeat;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        float burn = baseBurnRate + Heat * burnRatePerHeat;
        Heat = Mathf.Max(0f, Heat - burn * Time.deltaTime);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(coldColor, hotColor, Heat01);
    }

    public void OnPress()
    {
        AddCoal();
    }

    public void OnRelease()
    {
    }

    public void AddCoal()
    {
        if (Heat >= maxHeat)
            return;

        if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpendCoal())
            return;

        Heat = Mathf.Min(maxHeat, Heat + heatPerCoal);
        OnCoalAdded?.Invoke();

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(Pulse());
    }

    public void SetSpriteRendererForSceneBuilder(SpriteRenderer renderer)
    {
        spriteRenderer = renderer;
    }

    private IEnumerator Pulse()
    {
        transform.localScale = baseScale * 1.08f;
        yield return new WaitForSeconds(0.07f);
        transform.localScale = baseScale;
        pulseRoutine = null;
    }
}
