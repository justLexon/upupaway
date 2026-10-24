using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BuilderUtils;

// Tools > Up Up Away > Build Art Style.
// Applies the steampunk look everywhere: palette colors + ink outlines on the world art, brass/glass HUD frames,
// wooden shop signs, the Rye / Special Elite fonts, and a sharper PiP. Safe to re-run.
public static class ArtStyleBuilder
{
    private const string PipTexturePath = "Assets/Rendering/PipTexture.renderTexture";

    [MenuItem("Tools/Up Up Away/Build Art Style")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        if (Object.FindAnyObjectByType<DayNightCycle>() == null)
        {
            EditorUtility.DisplayDialog("Build Phase 6 first", "Run Tools > Up Up Away > Build Phase 6 Scene, then Build Art Style.", "OK");
            return;
        }

        TMP_FontAsset display = ArtStyleAssets.DisplayFont;
        TMP_FontAsset body = ArtStyleAssets.BodyFont;
        SetDefaultFont(body);
        SharpenPip();

        // World art, then shop + HUD, then the meter + day/night links (each rebuilds with the new style).
        VisualsBuilder.ApplyToOpenScene();
        if (!Phase5SceneBuilder.AddToOpenScene()) return;
        if (!Phase6SceneBuilder.AddToOpenScene()) return;

        StyleSceneLeftovers(display, body);

        SaveScene(scene);
        Debug.Log("Art style applied. Press Play!");
    }

    // New TMP text uses the body font from now on.
    private static void SetDefaultFont(TMP_FontAsset font)
    {
        if (font == null || TMP_Settings.instance == null)
            return;
        var so = new SerializedObject(TMP_Settings.instance);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(TMP_Settings.instance);
    }

    // The PiP was 640x360, which looked blurry when scaled up.
    private static void SharpenPip()
    {
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(PipTexturePath);
        if (texture == null || texture.width >= 1280)
            return;
        texture.Release();
        texture.width = 1280;
        texture.height = 720;
        EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
    }

    // Things built by earlier phases that the other builders don't rebuild.
    private static void StyleSceneLeftovers(TMP_FontAsset display, TMP_FontAsset body)
    {
        // Cabin wall: darker than the floor so the stations stand out.
        GameObject cabinBackground = GameObject.Find("CabinBackground");
        if (cabinBackground != null)
            cabinBackground.GetComponent<SpriteRenderer>().color = Color.Lerp(ArtPalette.Ink, ArtPalette.WoodDark, 0.55f);

        GameObject cloudVisual = GameObject.Find("CloudVisual");
        if (cloudVisual != null)
            cloudVisual.GetComponent<SpriteRenderer>().color = ArtPalette.Cream;

        DayNightCycle cycle = Object.FindAnyObjectByType<DayNightCycle>();
        if (cycle != null)
        {
            var so = new SerializedObject(cycle);
            so.FindProperty("daySky").colorValue = ArtPalette.Sky;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // PiP: brass frame around the view.
        GameObject pipFrame = GameObject.Find("PipFrame");
        if (pipFrame != null)
        {
            Image frameImage = pipFrame.GetComponent<Image>();
            frameImage.sprite = ArtStyleAssets.BrassFrame;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = Color.white;

            Transform view = pipFrame.transform.Find("PipView");
            if (view != null)
            {
                var rect = (RectTransform)view;
                rect.offsetMin = new Vector2(16f, 16f);
                rect.offsetMax = new Vector2(-16f, -16f);
            }
        }

        // Game over screen.
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager != null && GetReference(gameManager, "gameOverPanel") is GameObject panel)
        {
            panel.GetComponent<Image>().color = ArtPalette.WithAlpha(ArtPalette.Ink, 0.82f);

            Transform title = panel.transform.Find("GameOverText");
            if (title != null)
            {
                var text = title.GetComponent<TMP_Text>();
                if (display != null) text.font = display;
                text.color = ArtPalette.Parchment;
            }

            Transform result = panel.transform.Find("ResultText");
            if (result != null)
            {
                var text = result.GetComponent<TMP_Text>();
                if (body != null) text.font = body;
                text.color = ArtPalette.BrassLight;
            }
        }

        // Anything still on TMP's default font (e.g. the F1 debug text) gets the body font.
        if (body != null)
        {
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.font == null || text.font.name.StartsWith("LiberationSans"))
                    text.font = body;
            }
        }
    }
}
