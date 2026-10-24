using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds the Phase 1 scene in one click: Tools > Up Up Away > Build Phase 1 Scene.
// Copies SampleScene (keeps its URP camera + Global Light 2D) to Main.unity and adds everything else.
public static class Phase1SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string SpritePath = "Assets/Sprites/Square.png";

    [MenuItem("Tools/Up Up Away/Build Phase 1 Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Main scene?",
                    $"{ScenePath} already exists. Rebuild it from scratch? Any changes in it will be lost.",
                    "Rebuild", "Cancel"))
                return;
            AssetDatabase.DeleteAsset(ScenePath);
        }

        AssetDatabase.CopyAsset(SourceScenePath, ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // SampleScene's test square isn't needed here.
        GameObject oldSquare = GameObject.Find("Square");
        if (oldSquare != null) Object.DestroyImmediate(oldSquare);

        Sprite square = GetOrCreateSquareSprite();

        // Camera
        Camera cam = Camera.main;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.98f);

        // Ship
        GameObject ship = CreateSprite("Ship", square, new Color(0.55f, 0.35f, 0.2f), 0);
        ship.transform.position = new Vector3(0f, 1f, 0f);
        ship.transform.localScale = new Vector3(2f, 1f, 1f);

        // Cloud sea: the visual's top edge sits at the parent's origin.
        var cloudSea = new GameObject("CloudSea");
        cloudSea.transform.position = new Vector3(0f, -10f, 0f);
        GameObject cloudVisual = CreateSprite("CloudVisual", square, Color.white, 10);
        cloudVisual.transform.SetParent(cloudSea.transform, false);
        cloudVisual.transform.localPosition = new Vector3(0f, -5f, 0f);
        cloudVisual.transform.localScale = new Vector3(30f, 10f, 1f);

        var furnace = new GameObject("Furnace");
        var managers = new GameObject("Managers");

        // UI
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        TextMeshProUGUI debugText = CreateText("DebugText", canvasGO.transform, 28, Color.black, TextAlignmentOptions.TopLeft);
        RectTransform debugRect = debugText.rectTransform;
        debugRect.anchorMin = debugRect.anchorMax = debugRect.pivot = new Vector2(0f, 1f);
        debugRect.anchoredPosition = new Vector2(20f, -20f);
        debugRect.sizeDelta = new Vector2(500f, 250f);

        var panelGO = new GameObject("GameOverPanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        Stretch(panelGO.GetComponent<RectTransform>());
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        TextMeshProUGUI gameOverText = CreateText("GameOverText", panelGO.transform, 72, Color.white, TextAlignmentOptions.Center);
        Stretch(gameOverText.rectTransform);
        gameOverText.text = "GAME OVER\nPress R to restart";
        panelGO.SetActive(false);

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // Components + Inspector wiring
        GameManager gameManager = managers.AddComponent<GameManager>();
        managers.AddComponent<ResourceManager>();
        Furnace furnaceComp = furnace.AddComponent<Furnace>();
        ShipController shipComp = ship.AddComponent<ShipController>();
        CloudSea cloudComp = cloudSea.AddComponent<CloudSea>();
        DebugHUD hud = canvasGO.AddComponent<DebugHUD>();

        Assign(gameManager, "gameOverPanel", panelGO);
        Assign(shipComp, "furnace", furnaceComp);
        Assign(cloudComp, "ship", shipComp);
        Assign(hud, "text", debugText);
        Assign(hud, "ship", shipComp);
        Assign(hud, "furnace", furnaceComp);
        Assign(hud, "cloudSea", cloudComp);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildList();

        Debug.Log("Phase 1 scene built at " + ScenePath + ". Press Play!");
    }

    private static Sprite GetOrCreateSquareSprite()
    {
        if (!File.Exists(SpritePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
            var tex = new Texture2D(4, 4);
            tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 4f; // 4px texture = 1 world unit
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static GameObject CreateSprite(string name, Sprite sprite, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Assign(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSceneToBuildList()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
