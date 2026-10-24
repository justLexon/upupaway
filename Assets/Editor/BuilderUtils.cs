using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shared helpers for the additive phase builders (Phase 3+).
public static class BuilderUtils
{
    public const string ScenePath = "Assets/Scenes/Main.unity";
    public const string SquareSpritePath = "Assets/Sprites/Square.png";
    public const string CircleSpritePath = "Assets/Sprites/Circle.png";
    public const string PrefabFolder = "Assets/Prefabs";

    // Saves/prompts, then opens Main. Returns false if the user cancelled or Main doesn't exist.
    public static bool OpenMainScene(out Scene scene)
    {
        scene = default;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("No Main scene", "Run Build Phase 2 Scene first.", "OK");
            return false;
        }

        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    public static void SaveScene(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static Sprite SquareSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);

    public static Sprite GetOrCreateCircleSprite()
    {
        if (!File.Exists(CircleSpritePath))
        {
            const int size = 64;
            var tex = new Texture2D(size, size);
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01(radius - distance); // 1px soft edge
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(CircleSpritePath));
            File.WriteAllBytes(CircleSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(CircleSpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(CircleSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size; // 1 world unit across
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
    }

    public static GameObject CreateSprite(string name, Sprite sprite, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    public static GameObject SavePrefab(GameObject go)
    {
        Directory.CreateDirectory(PrefabFolder);
        string path = $"{PrefabFolder}/{go.name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    public static RectTransform CreateUI(string name, Transform parent, Color? imageColor = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (imageColor.HasValue)
            go.AddComponent<Image>().color = imageColor.Value;
        return go.GetComponent<RectTransform>();
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUI(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    public static void Assign(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static Object GetReference(Object target, string field)
    {
        return new SerializedObject(target).FindProperty(field).objectReferenceValue;
    }

    public static void SetSpawnEntries(ObstacleSpawner spawner, params (AltitudeObject prefab, float weight)[] items)
    {
        var so = new SerializedObject(spawner);
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("prefab").objectReferenceValue = items[i].prefab;
            entry.FindPropertyRelative("weight").floatValue = items[i].weight;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
