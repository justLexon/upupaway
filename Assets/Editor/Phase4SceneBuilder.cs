using TMPro;
using UnityEditor;
using UnityEngine;
using static BuilderUtils;

// Tools > Up Up Away > Build Phase 4 Scene.
// Additive: adds Phase 4 (and Phase 3 first, if missing) to the existing Main scene, keeping any hand edits.
public static class Phase4SceneBuilder
{
    private const int JugSlotCount = 6;

    [MenuItem("Tools/Up Up Away/Build Phase 4 Scene")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        if (IsBuilt())
        {
            EditorUtility.DisplayDialog("Already built", "This scene already has Phase 4 (an AltitudeMeter). Nothing to do.", "OK");
            return;
        }

        if (!Phase3SceneBuilder.IsBuilt() && !Phase3SceneBuilder.AddToOpenScene()) return;
        if (!AddToOpenScene()) return;

        SaveScene(scene);
        Debug.Log("Phase 4 built in " + ScenePath + ". Base game complete. Press Play!");
    }

    public static bool IsBuilt() => Object.FindAnyObjectByType<AltitudeMeter>() != null;

    public static bool AddToOpenScene()
    {
        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        CloudSea cloudSea = Object.FindAnyObjectByType<CloudSea>();
        Furnace furnace = Object.FindAnyObjectByType<Furnace>();
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        GameObject canvas = GameObject.Find("Canvas");
        ObstacleSpawner obstacleSpawner = FindBirdSpawner();
        var gameOverPanel = gameManager != null ? GetReference(gameManager, "gameOverPanel") as GameObject : null;

        if (ship == null || cloudSea == null || furnace == null || gameManager == null
            || canvas == null || obstacleSpawner == null || gameOverPanel == null)
        {
            EditorUtility.DisplayDialog("Missing objects",
                "Couldn't find the Ship, CloudSea, Furnace, GameManager (with its Game Over Panel), Canvas, " +
                "or the bird ObstacleSpawner. Is this the Phase 3 scene?", "OK");
            return false;
        }

        // Jug slots along the ship's bottom edge (local units; the ship is scaled 2x wide).
        var slotsRoot = new GameObject("JugSlots").transform;
        slotsRoot.SetParent(ship.transform, false);
        var shipSo = new SerializedObject(ship);
        SerializedProperty slots = shipSo.FindProperty("jugSlots");
        slots.arraySize = JugSlotCount;
        for (int i = 0; i < JugSlotCount; i++)
        {
            var slot = new GameObject("Slot" + i).transform;
            slot.SetParent(slotsRoot, false);
            slot.localPosition = new Vector3(Mathf.Lerp(-0.45f, 0.45f, i / (JugSlotCount - 1f)), -0.8f, 0f);
            slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
        }
        shipSo.ApplyModifiedPropertiesWithoutUndo();

        // Prefabs + spawner mix
        MilkJug jugPrefab = VisualsBuilder.CreateMilkJugPrefab();
        ResourceBalloon balloonPrefab = VisualsBuilder.CreateResourceBalloonPrefab();
        Bird birdPrefab = AssetDatabase.LoadAssetAtPath<Bird>(PrefabFolder + "/Bird.prefab");

        SetSpawnEntries(obstacleSpawner, (birdPrefab, 40f), (jugPrefab, 35f), (balloonPrefab, 25f));
        Assign(obstacleSpawner, "pityPrefab", balloonPrefab);

        HudBuilder.BuildAltitudeMeter(canvas.transform, ship, cloudSea, null);
        HudBuilder.BuildResourceHUD(canvas.transform, furnace);

        // Game over results
        TextMeshProUGUI resultText = CreateText("ResultText", gameOverPanel.transform, 44, Color.white, TextAlignmentOptions.Center);
        RectTransform resultRect = resultText.rectTransform;
        resultRect.anchorMin = resultRect.anchorMax = resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = new Vector2(0f, -200f);
        resultRect.sizeDelta = new Vector2(900f, 150f);
        Assign(gameManager, "ship", ship);
        Assign(gameManager, "resultText", resultText);

        return true;
    }

    private static ObstacleSpawner FindBirdSpawner()
    {
        foreach (ObstacleSpawner spawner in Object.FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None))
        {
            SerializedProperty entries = new SerializedObject(spawner).FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue is Bird)
                    return spawner;
            }
        }
        return null;
    }

}
