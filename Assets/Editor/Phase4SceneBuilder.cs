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

        Sprite square = SquareSprite();
        Sprite circle = GetOrCreateCircleSprite();

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
        ResourceBalloon balloonPrefab = CreateBalloonPrefab(square, circle);
        Bird birdPrefab = AssetDatabase.LoadAssetAtPath<Bird>(PrefabFolder + "/Bird.prefab");

        SetSpawnEntries(obstacleSpawner, (birdPrefab, 40f), (jugPrefab, 35f), (balloonPrefab, 25f));
        Assign(obstacleSpawner, "pityPrefab", balloonPrefab);

        BuildAltitudeMeter(canvas.transform, ship, cloudSea);
        BuildResourceHUD(canvas.transform, furnace);

        // Game over results
        TextMeshProUGUI resultText = CreateText("ResultText", gameOverPanel.transform, 44, Color.white, TextAlignmentOptions.Center);
        RectTransform resultRect = resultText.rectTransform;
        resultRect.anchorMin = resultRect.anchorMax = resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = new Vector2(0f, -200f);
        resultRect.sizeDelta = new Vector2(900f, 150f);
        Assign(gameManager, "ship", ship);
        Assign(gameManager, "resultText", resultText);

        if (gameManager.GetComponent<ShopManager>() == null)
            gameManager.gameObject.AddComponent<ShopManager>();

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

    private static ResourceBalloon CreateBalloonPrefab(Sprite square, Sprite circle)
    {
        GameObject go = CreateSprite("ResourceBalloon", circle, new Color(0.9f, 0.25f, 0.3f), 3);
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        GameObject rope = CreateSprite("Rope", square, new Color(0.3f, 0.25f, 0.2f), 2);
        rope.transform.SetParent(go.transform, false);
        rope.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        rope.transform.localScale = new Vector3(0.05f, 0.4f, 1f);

        GameObject crate = CreateSprite("Crate", square, new Color(0.55f, 0.35f, 0.15f), 3);
        crate.transform.SetParent(go.transform, false);
        crate.transform.localPosition = new Vector3(0f, -0.95f, 0f);
        crate.transform.localScale = new Vector3(0.6f, 0.5f, 1f);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1.75f);
        collider.offset = new Vector2(0f, -0.35f);
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        ResourceBalloon balloon = go.AddComponent<ResourceBalloon>();
        SetFloat(balloon, "driftSpeed", 1.5f);
        return SavePrefab(go).GetComponent<ResourceBalloon>();
    }

    private static void BuildAltitudeMeter(Transform canvas, ShipController ship, CloudSea cloudSea)
    {
        RectTransform bar = CreateUI("AltitudeMeter", canvas, new Color(0f, 0f, 0f, 0.5f));
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 0.5f);
        bar.anchoredPosition = new Vector2(40f, -40f);
        bar.sizeDelta = new Vector2(40f, 600f);
        bar.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

        RectTransform fill = CreateUI("Fill", bar, Color.green);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(1f, 0.5f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

        RectTransform marker = CreateUI("ShipMarker", bar, new Color(0.55f, 0.35f, 0.2f));
        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(70f, 14f);
        marker.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

        TextMeshProUGUI altitudeText = CreateText("AltitudeText", bar, 40, Color.black, TextAlignmentOptions.BottomLeft);
        RectTransform textRect = altitudeText.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 10f);
        textRect.sizeDelta = new Vector2(320f, 60f);

        AltitudeMeter meter = bar.gameObject.AddComponent<AltitudeMeter>();
        Assign(meter, "ship", ship);
        Assign(meter, "cloudSea", cloudSea);
        Assign(meter, "fill", fill);
        Assign(meter, "fillImage", fill.GetComponent<UnityEngine.UI.Image>());
        Assign(meter, "marker", marker);
        Assign(meter, "altitudeText", altitudeText);
    }

    private static void BuildResourceHUD(Transform canvas, Furnace furnace)
    {
        // Top-right: clear of the PiP in both views.
        RectTransform panel = CreateUI("ResourceHUD", canvas, new Color(0f, 0f, 0f, 0.45f));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-20f, -20f);
        panel.sizeDelta = new Vector2(320f, 110f);
        panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

        TextMeshProUGUI coalText = CreateText("CoalText", panel, 36, Color.white, TextAlignmentOptions.Left);
        RectTransform coalRect = coalText.rectTransform;
        coalRect.anchorMin = new Vector2(0f, 1f);
        coalRect.anchorMax = new Vector2(1f, 1f);
        coalRect.pivot = new Vector2(0.5f, 1f);
        coalRect.offsetMin = new Vector2(16f, -60f);
        coalRect.offsetMax = new Vector2(-16f, -8f);

        RectTransform heatBar = CreateUI("HeatBar", panel, new Color(0.15f, 0.15f, 0.15f));
        heatBar.anchorMin = new Vector2(0f, 0f);
        heatBar.anchorMax = new Vector2(1f, 0f);
        heatBar.pivot = new Vector2(0.5f, 0f);
        heatBar.offsetMin = new Vector2(16f, 14f);
        heatBar.offsetMax = new Vector2(-16f, 40f);
        heatBar.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

        RectTransform heatFill = CreateUI("HeatFill", heatBar, new Color(1f, 0.55f, 0.1f));
        heatFill.anchorMin = Vector2.zero;
        heatFill.anchorMax = new Vector2(0.5f, 1f);
        heatFill.offsetMin = heatFill.offsetMax = Vector2.zero;
        heatFill.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

        ResourceHUD hud = panel.gameObject.AddComponent<ResourceHUD>();
        Assign(hud, "furnace", furnace);
        Assign(hud, "coalText", coalText);
        Assign(hud, "heatFill", heatFill);
        Assign(hud, "heatFillImage", heatFill.GetComponent<UnityEngine.UI.Image>());
    }
}
