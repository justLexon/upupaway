using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BuilderUtils;

// Builds the altitude meter (left) and resource HUD (top right). Used by the Phase 4/5/6 builders and Build Art Style.
// Both rebuild from scratch: any existing AltitudeMeter / ResourceHUD under the canvas is replaced.
// Styled with the brass/glass sprites from ArtStyleAssets and colors from ArtPalette.
public static class HudBuilder
{
    private static Sprite frame;
    private static Sprite tube;
    private static Sprite tubeFill;
    private static Sprite knob;
    private static TMP_FontAsset displayFont;
    private static TMP_FontAsset bodyFont;

    private static void LoadArt()
    {
        frame = ArtStyleAssets.BrassFrame;
        tube = ArtStyleAssets.Tube;
        tubeFill = ArtStyleAssets.TubeFill;
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        displayFont = ArtStyleAssets.DisplayFont;
        bodyFont = ArtStyleAssets.BodyFont;
    }

    // ---------- Altitude meter ----------

    public static AltitudeMeter BuildAltitudeMeter(Transform canvas, ShipController ship, CloudSea cloudSea, PitstopManager pitstops)
    {
        LoadArt();
        DestroyChild(canvas, "AltitudeMeter");

        RectTransform panel = Sliced("AltitudeMeter", canvas, frame, Color.white);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 0.5f);
        panel.anchoredPosition = new Vector2(24f, -10f);
        panel.sizeDelta = new Vector2(170f, 720f);

        // Glass tube on the left side of the panel. Top leaves room for the pitstop flag + label,
        // bottom for the cloud puffs.
        RectTransform track = Sliced("Track", panel, tube, Color.white);
        track.anchorMin = new Vector2(0f, 0f);
        track.anchorMax = new Vector2(0f, 1f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.offsetMin = new Vector2(30f, 44f);
        track.offsetMax = new Vector2(58f, -84f);

        RectTransform fill = Sliced("Fill", track, tubeFill, ArtPalette.Moss);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(1f, 0.5f);
        fill.offsetMin = new Vector2(3f, 3f);
        fill.offsetMax = new Vector2(-3f, -3f);

        // Pitstop pennant at the top of the tube.
        RectTransform flag = Container("PitstopFlag", track);
        flag.anchorMin = flag.anchorMax = new Vector2(0.5f, 1f);
        flag.pivot = new Vector2(0.5f, 0f);
        flag.sizeDelta = new Vector2(40f, 44f);
        RectTransform pole = Box("Pole", flag, ArtPalette.Steel);
        pole.anchorMin = pole.anchorMax = pole.pivot = new Vector2(0.5f, 0f);
        pole.sizeDelta = new Vector2(4f, 44f);
        RectTransform cloth = Box("Cloth", flag, ArtPalette.Copper);
        cloth.anchorMin = cloth.anchorMax = new Vector2(0.5f, 1f);
        cloth.pivot = new Vector2(0f, 1f);
        cloth.anchoredPosition = new Vector2(2f, 0f);
        cloth.sizeDelta = new Vector2(26f, 18f);

        TextMeshProUGUI pitstopText = Text("PitstopText", track, "PITSTOP\n0 m", 20, ArtPalette.BrassLight, TextAlignmentOptions.TopLeft, displayFont);
        RectTransform pitRect = pitstopText.rectTransform;
        pitRect.anchorMin = pitRect.anchorMax = new Vector2(1f, 1f);
        pitRect.pivot = new Vector2(0f, 1f);
        pitRect.anchoredPosition = new Vector2(12f, 44f);
        pitRect.sizeDelta = new Vector2(100f, 56f);
        pitstopText.lineSpacing = -10f;

        // Cloud puffs at the bottom of the tube.
        RectTransform clouds = Container("Clouds", track);
        clouds.anchorMin = clouds.anchorMax = new Vector2(0.5f, 0f);
        clouds.sizeDelta = Vector2.zero;
        Knob("PuffL", clouds, ArtPalette.Cream, new Vector2(-16f, -6f), new Vector2(30f, 26f));
        Knob("PuffR", clouds, ArtPalette.Cream, new Vector2(16f, -6f), new Vector2(30f, 26f));
        Knob("PuffM", clouds, ArtPalette.Cream, new Vector2(0f, 0f), new Vector2(36f, 32f));

        // Ship marker: a tiny airship with an ink outline.
        RectTransform marker = Container("ShipMarker", track);
        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(48f, 32f);
        Knob("EnvelopeOutline", marker, ArtPalette.Ink, new Vector2(0f, 5f), new Vector2(52f, 28f));
        Knob("Envelope", marker, ArtPalette.Canvas, new Vector2(0f, 5f), new Vector2(48f, 24f));
        RectTransform stripe = Box("Stripe", marker, ArtPalette.Signal);
        stripe.anchoredPosition = new Vector2(0f, 5f);
        stripe.sizeDelta = new Vector2(38f, 4f);
        RectTransform gondolaOutline = Box("GondolaOutline", marker, ArtPalette.Ink);
        gondolaOutline.anchoredPosition = new Vector2(0f, -11f);
        gondolaOutline.sizeDelta = new Vector2(26f, 12f);
        RectTransform gondola = Box("Gondola", marker, ArtPalette.Wood);
        gondola.anchoredPosition = new Vector2(0f, -11f);
        gondola.sizeDelta = new Vector2(22f, 8f);

        TextMeshProUGUI gapText = Text("GapText", marker, "+0 m", 24, ArtPalette.Cream, TextAlignmentOptions.Left, bodyFont);
        RectTransform gapRect = gapText.rectTransform;
        gapRect.anchorMin = gapRect.anchorMax = new Vector2(1f, 0.5f);
        gapRect.pivot = new Vector2(0f, 0.5f);
        gapRect.anchoredPosition = new Vector2(6f, 0f);
        gapRect.sizeDelta = new Vector2(90f, 30f);

        AltitudeMeter meter = panel.gameObject.AddComponent<AltitudeMeter>();
        Assign(meter, "ship", ship);
        Assign(meter, "cloudSea", cloudSea);
        Assign(meter, "pitstops", pitstops);
        Assign(meter, "fill", fill);
        Assign(meter, "fillImage", fill.GetComponent<Image>());
        Assign(meter, "shipMarker", marker);
        Assign(meter, "gapText", gapText);
        Assign(meter, "pitstopText", pitstopText);

        var so = new SerializedObject(meter);
        so.FindProperty("safeColor").colorValue = ArtPalette.Moss;
        so.FindProperty("warnColor").colorValue = ArtPalette.Brass;
        so.FindProperty("dangerColor").colorValue = ArtPalette.Signal;
        so.ApplyModifiedPropertiesWithoutUndo();
        return meter;
    }

    // ---------- Resource HUD ----------

    public static ResourceHUD BuildResourceHUD(Transform canvas, Furnace furnace)
    {
        LoadArt();
        DestroyChild(canvas, "ResourceHUD");

        // Top-right: clear of the PiP in both views.
        RectTransform panel = Sliced("ResourceHUD", canvas, frame, Color.white);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-20f, -20f);
        panel.sizeDelta = new Vector2(290f, 214f);

        // Same pixel art as in the world.
        TextMeshProUGUI coal = Row(panel, "Coal", 0);
        PixelIcon(coal.transform.parent, VisualsBuilder.CoalSprite, new Vector2(30f, 0f), new Vector2(34f, 32f));

        TextMeshProUGUI milk = Row(panel, "Milk", 1);
        PixelIcon(milk.transform.parent, VisualsBuilder.MilkBottleSprite, new Vector2(30f, 0f), new Vector2(18f, 35f));

        TextMeshProUGUI coins = Row(panel, "Coins", 2);
        Knob("IconOutline", coins.transform.parent, ArtPalette.Ink, new Vector2(30f, 0f), new Vector2(28f, 28f));
        Knob("Icon", coins.transform.parent, ArtPalette.Brass, new Vector2(30f, 0f), new Vector2(24f, 24f));
        Knob("Shine", coins.transform.parent, ArtPalette.BrassLight, new Vector2(27f, 3f), new Vector2(10f, 10f));

        TextMeshProUGUI heatLabel = Text("HeatLabel", panel, "Heat", 18, ArtPalette.Parchment, TextAlignmentOptions.Left, displayFont);
        RectTransform heatLabelRect = heatLabel.rectTransform;
        heatLabelRect.anchorMin = new Vector2(0f, 0f);
        heatLabelRect.anchorMax = new Vector2(1f, 0f);
        heatLabelRect.pivot = new Vector2(0.5f, 0f);
        heatLabelRect.offsetMin = new Vector2(26f, 50f);
        heatLabelRect.offsetMax = new Vector2(-26f, 72f);

        RectTransform heatBar = Sliced("HeatBar", panel, tube, Color.white);
        heatBar.anchorMin = new Vector2(0f, 0f);
        heatBar.anchorMax = new Vector2(1f, 0f);
        heatBar.pivot = new Vector2(0.5f, 0f);
        heatBar.offsetMin = new Vector2(24f, 22f);
        heatBar.offsetMax = new Vector2(-24f, 48f);

        RectTransform heatFill = Sliced("HeatFill", heatBar, tubeFill, ArtPalette.Flame);
        heatFill.anchorMin = Vector2.zero;
        heatFill.anchorMax = new Vector2(0.5f, 1f);
        heatFill.offsetMin = new Vector2(3f, 3f);
        heatFill.offsetMax = new Vector2(-3f, -3f);

        ResourceHUD hud = panel.gameObject.AddComponent<ResourceHUD>();
        Assign(hud, "furnace", furnace);
        Assign(hud, "coalText", coal);
        Assign(hud, "milkText", milk);
        Assign(hud, "coinText", coins);
        Assign(hud, "heatFill", heatFill);
        Assign(hud, "heatFillImage", heatFill.GetComponent<Image>());

        var so = new SerializedObject(hud);
        so.FindProperty("normalTextColor").colorValue = ArtPalette.Cream;
        so.FindProperty("lowTextColor").colorValue = ArtPalette.Signal;
        so.FindProperty("coldColor").colorValue = ArtPalette.Steel;
        so.FindProperty("hotColor").colorValue = ArtPalette.Flame;
        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    // A row: icon slot on the left, label, and a right-aligned value (returned).
    private static TextMeshProUGUI Row(RectTransform panel, string name, int index)
    {
        RectTransform row = Container(name + "Row", panel);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.offsetMin = new Vector2(14f, -(24f + 40f * (index + 1)));
        row.offsetMax = new Vector2(-26f, -(24f + 40f * index));

        TextMeshProUGUI label = Text("Label", row, name, 24, ArtPalette.Parchment, TextAlignmentOptions.Left, displayFont);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(54f, 0f);

        TextMeshProUGUI value = Text("Value", row, "0", 30, ArtPalette.Cream, TextAlignmentOptions.Right, bodyFont);
        Stretch(value.rectTransform);
        return value;
    }

    // ---------- Small helpers ----------

    private static RectTransform Container(string name, Transform parent) => CreateUI(name, parent);

    private static RectTransform Box(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateUI(name, parent, color);
        rect.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    private static RectTransform Sliced(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rect = Box(name, parent, color);
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        return rect;
    }

    private static void PixelIcon(Transform row, Sprite sprite, Vector2 position, Vector2 size)
    {
        RectTransform rect = Box("Icon", row, Color.white);
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        AnchorLeft(rect, position, size);
    }

    private static void Knob(string name, Transform parent, Color color, Vector2 position, Vector2 size)
    {
        RectTransform rect = Box(name, parent, color);
        rect.GetComponent<Image>().sprite = knob;
        AnchorLeftIfRow(rect, parent, position, size);
    }

    // Inside HUD rows, icons sit on the left; everywhere else they're centered on the parent.
    private static void AnchorLeftIfRow(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
    {
        if (parent.name.EndsWith("Row"))
        {
            AnchorLeft(rect, position, size);
            return;
        }
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void AnchorLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        TextMeshProUGUI tmp = CreateText(name, parent, size, color, alignment);
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
