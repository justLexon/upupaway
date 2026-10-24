using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceHUD : MonoBehaviour
{
    [SerializeField] private Furnace furnace;
    [SerializeField] private TMP_Text coalText;
    [SerializeField] private RectTransform heatFill;
    [SerializeField] private Image heatFillImage;
    [SerializeField] private int lowCoal = 3;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color lowTextColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color coldColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color hotColor = new Color(1f, 0.55f, 0.1f);

    private void Update()
    {
        int coal = ResourceManager.Instance.Coal;
        coalText.text = $"Coal: {coal}  Coins: {ResourceManager.Instance.Coins}";
        coalText.color = coal <= lowCoal ? lowTextColor : normalTextColor;

        heatFill.anchorMax = new Vector2(furnace.Heat01, 1f);
        heatFillImage.color = Color.Lerp(coldColor, hotColor, furnace.Heat01);
    }
}
