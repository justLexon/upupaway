using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Generates the themed UI art (9-sliced brass frame, glass tube, tube fill, wood plank) and the TMP font assets.
// Everything is cached: assets are only generated if missing (delete them to regenerate).
public static class ArtStyleAssets
{
    private const string ArtFolder = "Assets/Art/UI";
    private const string FontFolder = "Assets/Fonts";
    private const float PixelsPerUnit = 100f;

    // ---------- Fonts ----------

    // Headings, signs, buttons: Victorian poster lettering.
    public static TMP_FontAsset DisplayFont => GetOrCreateFont("Rye-Regular", "Rye SDF");

    // Numbers and readouts: old typewriter.
    public static TMP_FontAsset BodyFont => GetOrCreateFont("SpecialElite-Regular", "Special Elite SDF");

    private static TMP_FontAsset GetOrCreateFont(string fontFile, string assetName)
    {
        string assetPath = $"{FontFolder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{fontFile}.ttf");
        if (font == null)
        {
            Debug.LogWarning($"Font {fontFile}.ttf not found in {FontFolder}; keeping the default font.");
            return null;
        }

        // Dynamic atlas: any character used gets added automatically, in the editor and in builds.
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
        asset.name = assetName;
        asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 $+-.,:;!?'\"()/%x");

        AssetDatabase.CreateAsset(asset, assetPath);
        asset.atlasTextures[0].name = assetName + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = assetName + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    // ---------- Sprites ----------

    // Riveted brass frame around a dark iron face. For HUD panels and the PiP frame.
    public static Sprite BrassFrame => GetOrCreate("BrassFrame", 96, 96, 22, DrawBrassFrame);

    // Dark glass tube with a rim highlight. Works vertical or horizontal (gauges, heat bar).
    public static Sprite Tube => GetOrCreate("Tube", 32, 32, 12, DrawTube);

    // White rounded fill with a cylindrical shade; tint it (the liquid in a tube).
    public static Sprite TubeFill => GetOrCreate("TubeFill", 32, 32, 12, DrawTubeFill);

    // Light grayscale plank with grain, outline and nails; tint it wood-colored or painted.
    public static Sprite WoodPlank => GetOrCreate("WoodPlank", 128, 64, 16, DrawWoodPlank);

    private delegate Color Painter(float x, float y, int width, int height);

    private static Sprite GetOrCreate(string name, int width, int height, int border, Painter painter)
    {
        string path = $"{ArtFolder}/{name}.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = painter(x + 0.5f, y + 0.5f, width, height);
            tex.SetPixels(pixels);

            Directory.CreateDirectory(ArtFolder);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // required for 9-slicing
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Painters ----------

    private static Color DrawBrassFrame(float x, float y, int w, int h)
    {
        float d = RoundedRect(x, y, w, h, 16f);
        if (d > 0.5f)
            return Color.clear;
        float coverage = Mathf.Clamp01(0.5f - d);

        // Light comes from the top-left.
        float light = ((w - x) + y) / (w + h);

        Color color;
        if (d > -2f)
        {
            color = ArtPalette.Ink;
        }
        else if (d > -12f)
        {
            // Brass band with a rounded ridge.
            float across = (-d - 2f) / 10f;
            float ridge = Mathf.Sin(across * Mathf.PI);
            Color baseColor = Color.Lerp(ArtPalette.BrassDark, ArtPalette.Brass, light);
            color = Color.Lerp(baseColor, ArtPalette.BrassLight, ridge * 0.55f * (0.4f + light));
        }
        else if (d > -14f)
        {
            color = ArtPalette.Ink; // inner groove
        }
        else
        {
            // Iron face: subtle mottling, darker toward the bottom, inner shadow along the edge.
            float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.08f;
            float shade = Mathf.Lerp(0.8f, 1.1f, y / h) + noise;
            float shadow = Mathf.Clamp01((-d - 14f) / 6f);
            color = ArtPalette.IronDark * shade * Mathf.Lerp(0.6f, 1f, shadow);
            color.a = 0.94f;
        }

        // Rivets in the corners.
        float inset = 11f;
        foreach (var (rx, ry) in new[] { (inset, inset), (w - inset, inset), (inset, h - inset), (w - inset, h - inset) })
        {
            float r = Mathf.Sqrt((x - rx) * (x - rx) + (y - ry) * (y - ry));
            if (r < 4.2f)
            {
                if (r > 3.2f)
                    color = ArtPalette.Ink;
                else
                {
                    float dome = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(rx - 1.2f, ry + 1.2f)) / 3.2f);
                    color = Color.Lerp(ArtPalette.BrassDark, ArtPalette.Cream, dome * 0.9f);
                }
                color.a = 1f;
            }
        }

        color.a *= coverage;
        return color;
    }

    private static Color DrawTube(float x, float y, int w, int h)
    {
        float d = RoundedRect(x, y, w, h, 10f);
        if (d > 0.5f)
            return Color.clear;
        float coverage = Mathf.Clamp01(0.5f - d);

        Color color;
        if (d > -2f)
            color = ArtPalette.Ink;
        else if (d > -4f)
            color = new Color(1f, 1f, 1f, 0.22f); // glass rim catching the light
        else
            color = ArtPalette.WithAlpha(ArtPalette.Ink, 0.6f);

        color.a *= coverage;
        return color;
    }

    private static Color DrawTubeFill(float x, float y, int w, int h)
    {
        float d = RoundedRect(x, y, w, h, 10f);
        if (d > 0.5f)
            return Color.clear;
        float coverage = Mathf.Clamp01(0.5f - d);

        // Brighter in the middle, darker at the edges: reads as liquid in a round tube.
        float shade = 0.72f + 0.28f * Mathf.Clamp01(-d / 8f);
        return new Color(shade, shade, shade, coverage);
    }

    private static Color DrawWoodPlank(float x, float y, int w, int h)
    {
        float d = RoundedRect(x, y, w, h, 6f);
        if (d > 0.5f)
            return Color.clear;
        float coverage = Mathf.Clamp01(0.5f - d);

        Color color;
        if (d > -3f)
        {
            color = ArtPalette.Ink;
        }
        else
        {
            // Long grain along x, with a few darker streaks.
            float grain = Mathf.PerlinNoise(x * 0.02f, y * 0.45f);
            float streak = Mathf.PerlinNoise(x * 0.01f + 7.3f, y * 0.18f);
            float shade = 0.78f + grain * 0.22f - Mathf.Clamp01(streak - 0.6f) * 0.5f;

            // Bevel: lit top edge, shaded bottom edge.
            if (d > -5f)
                shade *= y > h / 2f ? 1.12f : 0.75f;

            color = new Color(shade, shade, shade, 1f);
        }

        // Nails in the corners (inside the slice border, so they never stretch).
        foreach (var (nx, ny) in new[] { (9f, 9f), (w - 9f, 9f), (9f, h - 9f), (w - 9f, h - 9f) })
        {
            float r = Mathf.Sqrt((x - nx) * (x - nx) + (y - ny) * (y - ny));
            if (r < 2.6f)
                color = r < 1.6f ? new Color(0.55f, 0.55f, 0.55f, 1f) : ArtPalette.Ink;
        }

        color.a *= coverage;
        return color;
    }

    // Signed distance to a rounded rectangle filling the texture (negative inside).
    private static float RoundedRect(float x, float y, int w, int h, float radius)
    {
        float qx = Mathf.Abs(x - w / 2f) - (w / 2f - radius);
        float qy = Mathf.Abs(y - h / 2f) - (h / 2f - radius);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
    }
}
