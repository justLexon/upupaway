using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static BuilderUtils;

// Tools > Up Up Away > Build Phase 6 Scene.
// Day/night cycle, night storms (rain + lightning), day-only birds, and the trimmed-down altitude meter.
// Safe to re-run: reuses the existing DayNightCycle / WeatherSystem and re-links them.
public static class Phase6SceneBuilder
{
    private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    [MenuItem("Tools/Up Up Away/Build Phase 6 Scene")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        // Earlier phases first, if they're missing.
        if (Object.FindAnyObjectByType<PitstopManager>() == null)
        {
            if (!Phase3SceneBuilder.IsBuilt() && !Phase3SceneBuilder.AddToOpenScene()) return;
            if (!Phase4SceneBuilder.IsBuilt() && !Phase4SceneBuilder.AddToOpenScene()) return;
            if (!Phase5SceneBuilder.AddToOpenScene()) return;
        }

        if (!AddToOpenScene()) return;

        SaveScene(scene);
        Debug.Log("Phase 6 built: day/night, storms, lightning. Press Play!");
    }

    public static bool AddToOpenScene()
    {
        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        CloudSea cloudSea = Object.FindAnyObjectByType<CloudSea>();
        PitstopManager pitstops = Object.FindAnyObjectByType<PitstopManager>();
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        WindGusts wind = Object.FindAnyObjectByType<WindGusts>();
        GameObject outsideZone = GameObject.Find("OutsideZone");
        GameObject managers = GameObject.Find("Managers");
        GameObject canvas = GameObject.Find("Canvas");
        GameObject pipCamera = GameObject.Find("PipCamera");

        if (ship == null || cloudSea == null || gameManager == null || outsideZone == null || managers == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Missing objects",
                "Couldn't find the Ship, CloudSea, GameManager, OutsideZone, Managers, or Canvas.", "OK");
            return false;
        }
        if (wind == null)
            Debug.LogWarning("No WindGusts in the scene (run Build Wind Gusts). Storms will still rain, just without stronger gusts.");

        Light2D globalLight = FindGlobalLight();
        if (globalLight == null)
            Debug.LogWarning("No Global Light 2D found. Night won't darken the sprites, only the sky.");

        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
        if (unlit == null)
            Debug.LogWarning("Couldn't find Sprite-Unlit-Default. Stars, rain and lightning will dim at night.");

        Sprite circle = GetOrCreateCircleSprite();
        Sprite square = SquareSprite();

        // Day/night
        DayNightCycle cycle = managers.GetComponent<DayNightCycle>();
        if (cycle == null)
            cycle = managers.AddComponent<DayNightCycle>();
        var cycleSo = new SerializedObject(cycle);
        SerializedProperty cameras = cycleSo.FindProperty("skyCameras");
        Camera pip = pipCamera != null ? pipCamera.GetComponent<Camera>() : null;
        cameras.arraySize = pip != null ? 2 : 1;
        cameras.GetArrayElementAtIndex(0).objectReferenceValue = Camera.main;
        if (pip != null)
            cameras.GetArrayElementAtIndex(1).objectReferenceValue = pip;
        cycleSo.FindProperty("globalLight").objectReferenceValue = globalLight;
        cycleSo.FindProperty("starSprite").objectReferenceValue = circle;
        cycleSo.FindProperty("unlitMaterial").objectReferenceValue = unlit;
        cycleSo.FindProperty("starParent").objectReferenceValue = outsideZone.transform;
        cycleSo.ApplyModifiedPropertiesWithoutUndo();

        // Weather
        WeatherSystem weather = managers.GetComponent<WeatherSystem>();
        if (weather == null)
            weather = managers.AddComponent<WeatherSystem>();
        Assign(weather, "ship", ship);
        Assign(weather, "wind", wind);
        Assign(weather, "rainSprite", square);
        Assign(weather, "unlitMaterial", unlit);
        Assign(weather, "effectsParent", outsideZone.transform);

        // Birds only by day.
        foreach (ObstacleSpawner spawner in Object.FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None))
        {
            var so = new SerializedObject(spawner);
            SerializedProperty entries = so.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("prefab").objectReferenceValue is Bird)
                    entry.FindPropertyRelative("dayOnly").boolValue = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Meter without the altitude header and "CLOUDS" label.
        AltitudeMeter meter = HudBuilder.BuildAltitudeMeter(canvas.transform, ship, cloudSea, pitstops);
        if (GetReference(gameManager, "gameOverPanel") is GameObject gameOverPanel)
            meter.transform.SetSiblingIndex(gameOverPanel.transform.GetSiblingIndex());

        return true;
    }

    private static Light2D FindGlobalLight()
    {
        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType == Light2D.LightType.Global)
                return light;
        }
        return null;
    }
}
