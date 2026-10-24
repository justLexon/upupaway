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

    public static Sprite GetOrCreateCircleSprite() => GetOrCreateRoundSprite(CircleSpritePath, 0f);

    // A circle with a hole in the middle; innerRatio is the hole's size relative to the whole.
    public static Sprite GetOrCreateRingSprite() => GetOrCreateRoundSprite(RingSpritePath, 0.78f);

    public const string RingSpritePath = "Assets/Sprites/Ring.png";

    private static Sprite GetOrCreateRoundSprite(string path, float innerRatio)
    {
        if (!File.Exists(path))
        {
            const int size = 128;
            var tex = new Texture2D(size, size);
            float radius = size / 2f;
            float innerRadius = radius * innerRatio;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01(radius - distance); // 1px soft edge
                    if (innerRatio > 0f)
                        alpha = Mathf.Min(alpha, Mathf.Clamp01(distance - innerRadius));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size; // 1 world unit across
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // A child sprite positioned in its parent's local space. Used to compose art from simple shapes.
    public static GameObject Part(Transform parent, string name, Sprite sprite, Color color, int sortingOrder,
        Vector2 position, Vector2 scale, float rotation = 0f)
    {
        GameObject go = CreateSprite(name, sprite, color, sortingOrder);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        return go;
    }

    public static Transform Container(Transform parent, string name, Vector2 position = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        return go.transform;
    }

    public static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
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
