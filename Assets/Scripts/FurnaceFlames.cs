using UnityEngine;

// Flames inside the furnace door: grow with heat and flicker.
public class FurnaceFlames : MonoBehaviour
{
    [SerializeField] private Furnace furnace;
    [SerializeField] private Transform[] flames;
    [SerializeField] private float flickerSpeed = 12f;
    [SerializeField] private float flickerAmount = 0.2f;

    private Vector3[] baseScales;

    private void Awake()
    {
        baseScales = new Vector3[flames.Length];
        for (int i = 0; i < flames.Length; i++)
            baseScales[i] = flames[i].localScale;
    }

    private void Update()
    {
        float heat = Mathf.Max(0.05f, furnace.Heat01);
        for (int i = 0; i < flames.Length; i++)
        {
            float flicker = 1f + Mathf.Sin(Time.time * flickerSpeed + i * 1.7f) * flickerAmount;
            flames[i].localScale = baseScales[i] * heat * flicker;
        }
    }
}
