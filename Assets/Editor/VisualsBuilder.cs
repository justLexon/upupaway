using UnityEditor;
using UnityEngine;
using static BuilderUtils;

// Tools > Up Up Away > Build Visuals.
// Restyles the ship, steering wheel, furnace (+ coal pile) and birds out of simple shapes.
// Safe to re-run: it deletes the previous visuals before rebuilding. Gameplay objects/components are untouched.
public static class VisualsBuilder
{
    private static Sprite square;
    private static Sprite circle;

    [MenuItem("Tools/Up Up Away/Build Visuals")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        ApplyToOpenScene();

        SaveScene(scene);
        Debug.Log("Visuals built. Press Play!");
    }

    // Just swaps the ship's art (e.g. after editing the airship sprite sheet).
    [MenuItem("Tools/Up Up Away/Build Ship Sprite")]
    public static void BuildShipOnly()
    {
        if (!OpenMainScene(out var scene)) return;

        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        if (ship == null)
        {
            EditorUtility.DisplayDialog("No ship", "Build the earlier phases first.", "OK");
            return;
        }
        BuildShip(ship);

        SaveScene(scene);
        Debug.Log("Ship sprite applied. Press Play!");
    }

    // Just swaps the background cloud art (the SkyPuff prefab).
    [MenuItem("Tools/Up Up Away/Build Cloud Sprite")]
    public static void BuildCloudsOnly()
    {
        if (!OpenMainScene(out var scene)) return;

        if (RebuildPrefab<AltitudeObject>("SkyPuff", CreateSkyPuffPrefab).Count == 0)
        {
            EditorUtility.DisplayDialog("No clouds yet", "Build Phase 3 first (it adds the background clouds).", "OK");
            return;
        }

        SaveScene(scene);
        Debug.Log("Cloud sprite applied. Press Play!");
    }

    // Just swaps the steering wheel + stand art.
    [MenuItem("Tools/Up Up Away/Build Wheel Sprite")]
    public static void BuildWheelOnly()
    {
        if (!OpenMainScene(out var scene)) return;

        SteeringWheel wheel = Object.FindAnyObjectByType<SteeringWheel>();
        if (wheel == null)
        {
            EditorUtility.DisplayDialog("No steering wheel", "Build the earlier phases first.", "OK");
            return;
        }
        wheel.transform.localPosition = new Vector3(0f, FloorTop + StandBaseBelowCenter, 0f);
        BuildWheel(wheel);

        SaveScene(scene);
        Debug.Log("Wheel sprite applied. Press Play!");
    }

    // Also called by Build Art Style.
    public static void ApplyToOpenScene()
    {
        LoadSprites();

        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        SteeringWheel wheel = Object.FindAnyObjectByType<SteeringWheel>();
        Furnace furnace = Object.FindAnyObjectByType<Furnace>();
        GameObject cabinZone = GameObject.Find("CabinZone");

        // Cabin layout: wheel in the middle, furnace + coal on the left, everything standing on the floor.
        if (cabinZone != null) BuildCabinFloor(cabinZone.transform);
        if (wheel != null) wheel.transform.localPosition = new Vector3(0f, FloorTop + StandBaseBelowCenter, 0f); // stand rests on the floor
        if (furnace != null) furnace.transform.localPosition = new Vector3(-5f, FloorTop + 1.32f, 0f);

        if (ship != null) BuildShip(ship);
        if (wheel != null) BuildWheel(wheel);
        if (furnace != null) BuildFurnace(furnace);

        RebuildPrefab<Bird>("Bird", CreateBirdPrefab);
        RebuildPrefab<MilkJug>("MilkJug", CreateMilkJugPrefab);
        foreach (ObstacleSpawner decor in RebuildPrefab<AltitudeObject>("SkyPuff", CreateSkyPuffPrefab))
        {
            // Clouds are bigger now, so space them out more.
            SetFloat(decor, "minSpacing", 12f);
            SetFloat(decor, "maxSpacing", 22f);
        }
    }

    // Ink silhouette outline: an Ink copy of every part, slightly bigger, drawn behind everything.
    // Animated parts (propeller, flames) are skipped since their outline wouldn't animate with them.
    private static void AddOutlines(Transform root, float thickness, int sortingOrder, bool includeRoot)
    {
        DestroyChild(root, "RootOutline");

        foreach (SpriteRenderer part in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            bool isRoot = part.transform == root;
            if (isRoot && !includeRoot) continue;
            if (part.color.a < 0.99f) continue; // shines and highlights
            if (part.GetComponent<PropellerSpin>() != null || part.GetComponentInParent<FurnaceFlames>() != null) continue;

            var outline = new GameObject(isRoot ? "RootOutline" : part.name + "_Outline").transform;
            Vector3 world = part.transform.lossyScale;
            float growX = (Mathf.Abs(world.x) + thickness) / Mathf.Max(0.0001f, Mathf.Abs(world.x));
            float growY = (Mathf.Abs(world.y) + thickness) / Mathf.Max(0.0001f, Mathf.Abs(world.y));

            if (isRoot)
            {
                outline.SetParent(root, false);
                outline.localScale = new Vector3(growX, growY, 1f);
            }
            else
            {
                outline.SetParent(part.transform.parent, false);
                outline.localPosition = part.transform.localPosition;
                outline.localRotation = part.transform.localRotation;
                Vector3 local = part.transform.localScale;
                outline.localScale = new Vector3(local.x * growX, local.y * growY, 1f);
            }

            var renderer = outline.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = part.sprite;
            renderer.color = ArtPalette.Ink;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = part.sharedMaterial;
        }
    }

    private const float FloorTop = -3.6f; // cabin-local Y where things stand

    private static void LoadSprites()
    {
        square = SquareSprite();
        circle = GetOrCreateCircleSprite();
    }

    // ---------- Cabin floor ----------

    private static void BuildCabinFloor(Transform cabin)
    {
        DestroyChild(cabin, "CabinFloor");
        Transform floor = Container(cabin, "CabinFloor");

        Color plank = ArtPalette.WoodDark;
        Color gap = ArtPalette.Ink;
        Color trim = ArtPalette.Ink;

        float height = 5f + FloorTop; // from the cabin's bottom edge (-5) up to the floor top
        Part(floor, "Planks", square, plank, -9, new Vector2(0f, -5f + height / 2f), new Vector2(18f, height));
        Part(floor, "Baseboard", square, trim, -8, new Vector2(0f, FloorTop + 0.06f), new Vector2(18f, 0.14f));

        // Plank seams, staggered between the two rows.
        float midY = -5f + height / 2f;
        Part(floor, "RowSeam", square, gap, -8, new Vector2(0f, midY), new Vector2(18f, 0.04f));
        for (int i = 0; i < 12; i++)
        {
            float x = -8.25f + i * 1.5f;
            Part(floor, "SeamTop" + i, square, gap, -8, new Vector2(x, midY + height / 4f), new Vector2(0.04f, height / 2f));
            Part(floor, "SeamBottom" + i, square, gap, -8, new Vector2(x + 0.75f, midY - height / 4f), new Vector2(0.04f, height / 2f));
        }
    }

    // ---------- Ship ----------

    private static void BuildShip(ShipController ship)
    {
        Transform root = ship.transform;

        // The art is laid out at scale 1, so the old 2x1 stretch goes away.
        root.localScale = Vector3.one;
        var oldRenderer = root.GetComponent<SpriteRenderer>();
        if (oldRenderer != null)
            Object.DestroyImmediate(oldRenderer);

        var collider = root.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = new Vector2(3.2f, 2.4f);
            collider.offset = new Vector2(0f, 0.65f);
        }
        SetFloat(ship, "xLimit", 6.8f); // the ship is wider now

        // Jug slots along the bottom of the keel.
        Transform slots = root.Find("JugSlots");
        if (slots != null)
        {
            for (int i = 0; i < slots.childCount; i++)
            {
                float t = slots.childCount > 1 ? i / (slots.childCount - 1f) : 0.5f;
                slots.GetChild(i).localPosition = new Vector3(Mathf.Lerp(-0.85f, 0.85f, t), -0.82f, 0f);
            }
        }

        DestroyChild(root, "Visuals");
        Transform v = Container(root, "Visuals");

        // Hand-drawn pixel art from the sliced sprite sheet, looping while the game runs.
        Sprite[] frames = LoadShipFrames();
        if (frames.Length == 0)
        {
            Debug.LogWarning($"No sprites found in {ShipSheetPath}. Slice it in the Sprite Editor first.");
            return;
        }

        GameObject art = Part(v, "ShipSprite", frames[0], Color.white, 2, new Vector2(0f, 0.6f), new Vector2(1.8f, 1.8f));
        art.AddComponent<SpriteLoop>().SetFramesForSceneBuilder(frames);

        // Fit the hitbox to the sprite (envelope top to gondola bottom).
        if (collider != null)
        {
            collider.size = new Vector2(3.3f, 2.7f);
            collider.offset = new Vector2(0f, 0.66f);
        }
    }

    private const string ShipSheetPath = "Assets/Art/Sprites/airship-sprite-sheet-.png";

    // All slices of the sheet in order (_0, _1, ...), imported crisp for pixel art.
    private static Sprite[] LoadShipFrames()
    {
        var importer = AssetImporter.GetAtPath(ShipSheetPath) as TextureImporter;
        if (importer == null)
            return new Sprite[0];
        if (importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        var frames = new System.Collections.Generic.List<Sprite>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ShipSheetPath))
            if (asset is Sprite sprite)
                frames.Add(sprite);
        frames.Sort((a, b) => FrameIndex(a).CompareTo(FrameIndex(b)));
        return frames.ToArray();
    }

    private const string CloudSpritePath = "Assets/Art/Sprites/cloud-pixelart.png";

    // A single-image sprite, imported crisp for pixel art.
    private static Sprite LoadPixelSprite(string path)
    {
        AssetDatabase.ImportAsset(path); // in case Unity hasn't picked up a newly added file yet
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"{path} not found.");
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static int FrameIndex(Sprite sprite)
    {
        int underscore = sprite.name.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(sprite.name.Substring(underscore + 1), out int index) ? index : 0;
    }

    // ---------- Steering wheel ----------

    private static void BuildWheel(SteeringWheel wheel)
    {
        Transform root = wheel.transform;
        DestroyChild(root, "Spoke");
        DestroyChild(root, "Visuals");
        DestroyChild(root, "RootOutline");

        // Pixel art: both images share the same 256px canvas and center, so they stack at the same spot and scale.
        // The wheel is drawn by the root (which SteeringWheel rotates); the stand is a sibling, so it never turns.
        root.localScale = new Vector3(WheelArtScale, WheelArtScale, 1f);
        var rim = root.GetComponent<SpriteRenderer>();
        rim.sprite = LoadPixelSprite(WheelSpritePath);
        rim.color = Color.white;
        rim.sortingOrder = 6;

        var collider = root.GetComponent<CircleCollider2D>();
        if (collider != null)
            collider.radius = 0.43f; // the wheel is ~86px across in the image, including handles

        Transform parent = root.parent;
        DestroyChild(parent, "WheelStand");
        Transform stand = Container(parent, "WheelStand", root.localPosition);
        Part(stand, "Stand", LoadPixelSprite(StandSpritePath), Color.white, 2, Vector2.zero, new Vector2(WheelArtScale, WheelArtScale));
    }

    private const string WheelSpritePath = "Assets/Art/Sprites/steering-wheel-pixelart.png";
    private const string StandSpritePath = "Assets/Art/Sprites/stand-for-steering-wheel-pixelart.png";
    private const float WheelArtScale = 2f;
    private const float StandBaseBelowCenter = 0.88f * WheelArtScale; // the stand's base is 88px below the image center

    // ---------- Furnace + coal pile ----------

    private static void BuildFurnace(Furnace furnace)
    {
        Transform root = furnace.transform; // scaled 2x2, so 1 local unit = 2 world units
        DestroyChild(root, "Visuals");

        Color iron = ArtPalette.Iron;
        Color ironDark = ArtPalette.IronDark;
        Color rivet = ArtPalette.Brass;

        var body = root.GetComponent<SpriteRenderer>();
        body.color = iron;
        body.sortingOrder = 5;

        Transform v = Container(root, "Visuals");

        Part(v, "Chimney", square, iron, 4, new Vector2(0.25f, 2.25f), new Vector2(0.22f, 3.5f)); // up to the ceiling
        Part(v, "ChimneyBand", square, ironDark, 5, new Vector2(0.25f, 0.75f), new Vector2(0.28f, 0.06f));
        Part(v, "Cap", square, ironDark, 6, new Vector2(0f, 0.52f), new Vector2(1.1f, 0.12f));
        Part(v, "Base", square, ironDark, 6, new Vector2(0f, -0.5f), new Vector2(1.1f, 0.1f));
        Part(v, "FootL", square, ironDark, 6, new Vector2(-0.4f, -0.6f), new Vector2(0.15f, 0.12f));
        Part(v, "FootR", square, ironDark, 6, new Vector2(0.4f, -0.6f), new Vector2(0.15f, 0.12f));

        for (int i = 0; i < 4; i++)
        {
            var pos = new Vector2(i % 2 == 0 ? -0.42f : 0.42f, i < 2 ? 0.36f : -0.36f);
            Part(v, "Rivet" + i, circle, rivet, 6, pos, new Vector2(0.06f, 0.06f));
        }

        // Pressure gauge
        Part(v, "GaugeRim", circle, ironDark, 6, new Vector2(-0.28f, 0.3f), new Vector2(0.22f, 0.22f));
        Part(v, "GaugeFace", circle, ArtPalette.Cream, 7, new Vector2(-0.28f, 0.3f), new Vector2(0.17f, 0.17f));
        Part(v, "GaugeNeedle", square, ArtPalette.Signal, 8, new Vector2(-0.28f, 0.3f), new Vector2(0.02f, 0.08f), -40f);

        // Door: the glow is what Furnace tints by heat.
        Part(v, "DoorFrame", square, ironDark, 6, new Vector2(0f, -0.08f), new Vector2(0.62f, 0.46f));
        GameObject glow = Part(v, "FireGlow", square, ArtPalette.IronDark, 7, new Vector2(0f, -0.08f), new Vector2(0.52f, 0.36f));
        Assign(furnace, "spriteRenderer", glow.GetComponent<SpriteRenderer>());

        Transform flamesRoot = Container(v, "Flames", new Vector2(0f, -0.14f));
        var flames = new Transform[3];
        Color[] flameColors = { ArtPalette.Flame, ArtPalette.Ember, ArtPalette.Flame };
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 0.14f;
            float height = i == 1 ? 0.28f : 0.2f;
            flames[i] = Part(flamesRoot, "Flame" + i, circle, flameColors[i], 8, new Vector2(x, 0f), new Vector2(0.14f, height)).transform;
        }
        var flameComp = flamesRoot.gameObject.AddComponent<FurnaceFlames>();
        Assign(flameComp, "furnace", furnace);
        var flameSo = new SerializedObject(flameComp);
        SerializedProperty flameList = flameSo.FindProperty("flames");
        flameList.arraySize = flames.Length;
        for (int i = 0; i < flames.Length; i++)
            flameList.GetArrayElementAtIndex(i).objectReferenceValue = flames[i];
        flameSo.ApplyModifiedPropertiesWithoutUndo();

        AddOutlines(root, 0.06f, 3, true);

        BuildCoalPile(furnace, glow.transform);
    }

    private static void BuildCoalPile(Furnace furnace, Transform door)
    {
        Transform parent = furnace.transform.parent;
        DestroyChild(parent, "CoalPile");

        Vector3 pilePosition = furnace.transform.localPosition + new Vector3(-2.4f, -1.05f, 0f);
        Transform pile = Container(parent, "CoalPile", pilePosition);

        // Pyramid of lumps, bottom row first. Fixed seed so it looks the same every rebuild.
        var random = new System.Random(7);
        int[] rows = { 4, 3, 2, 1 };
        var lumps = new System.Collections.Generic.List<GameObject>();
        for (int row = 0; row < rows.Length; row++)
        {
            for (int i = 0; i < rows[row]; i++)
            {
                float x = (i - (rows[row] - 1) / 2f) * 0.36f + (float)(random.NextDouble() - 0.5) * 0.06f;
                float y = row * 0.27f;
                float size = 0.36f + (float)random.NextDouble() * 0.1f;
                float shade = 0.1f + (float)random.NextDouble() * 0.08f;
                lumps.Add(Part(pile, $"Lump{lumps.Count}", circle, new Color(shade, shade, shade + 0.01f), 6,
                    new Vector2(x, y), new Vector2(size, size * 0.85f), (float)random.NextDouble() * 360f));
            }
        }

        CoalPile pileComp = pile.gameObject.AddComponent<CoalPile>();
        Assign(pileComp, "furnace", furnace);
        Assign(pileComp, "throwTarget", door);
        var so = new SerializedObject(pileComp);
        SerializedProperty list = so.FindProperty("lumps");
        list.arraySize = lumps.Count;
        for (int i = 0; i < lumps.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = lumps[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Bird ----------

    // Overwriting a prefab can change its internal IDs, so remember which spawner entries used it and re-point them.
    // Returns the spawners that use it.
    private static System.Collections.Generic.List<ObstacleSpawner> RebuildPrefab<T>(string prefabName, System.Func<T> create)
        where T : AltitudeObject
    {
        string path = $"{PrefabFolder}/{prefabName}.prefab";
        var links = new System.Collections.Generic.List<(ObstacleSpawner spawner, int index)>();
        var users = new System.Collections.Generic.List<ObstacleSpawner>();
        foreach (ObstacleSpawner spawner in Object.FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None))
        {
            SerializedProperty entries = new SerializedObject(spawner).FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                Object prefab = entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue;
                if (prefab != null && AssetDatabase.GetAssetPath(prefab) == path)
                {
                    links.Add((spawner, i));
                    if (!users.Contains(spawner))
                        users.Add(spawner);
                }
            }
        }

        // Only rebuild prefabs the scene already uses (e.g. no MilkJug before Phase 4).
        if (links.Count == 0)
            return users;

        T created = create();
        foreach (var (spawner, index) in links)
        {
            var so = new SerializedObject(spawner);
            so.FindProperty("entries").GetArrayElementAtIndex(index).FindPropertyRelative("prefab").objectReferenceValue = created;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The balloon pity rule points at a prefab too.
        foreach (ObstacleSpawner spawner in Object.FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None))
        {
            Object pity = GetReference(spawner, "pityPrefab");
            if (pity != null && AssetDatabase.GetAssetPath(pity) == path)
                Assign(spawner, "pityPrefab", created);
        }
        return users;
    }

    // ---------- Milk jug (on a balloon) ----------

    // Also used by the Phase 4 builder. No red in the palette, so the red coal balloon stays easy to spot.
    private static readonly Color[] JugBalloonColors =
    {
        new Color(0.25f, 0.55f, 0.95f), // blue
        new Color(0.3f, 0.8f, 0.4f),    // green
        new Color(1f, 0.85f, 0.25f),    // yellow
        new Color(0.65f, 0.4f, 0.9f),   // purple
        new Color(1f, 0.5f, 0.75f),     // pink
        new Color(1f, 0.6f, 0.2f),      // orange
        new Color(0.25f, 0.8f, 0.8f),   // teal
    };

    // The jug + its balloon, centered on the jug. Shared by the MilkJug obstacle and the pitstop's milk display.
    // Returns the balloon (so the obstacle can pop it).
    public static Transform BuildMilkJugArt(Transform t, int orderOffset = 0, bool unscaledSway = false)
    {
        LoadSprites();

        Part(t, "Body", square, new Color(0.97f, 0.97f, 0.95f), 4 + orderOffset, Vector2.zero, new Vector2(0.3f, 0.45f));
        Part(t, "Cap", square, new Color(0.2f, 0.45f, 0.9f), 5 + orderOffset, new Vector2(0f, 0.2475f), new Vector2(0.18f, 0.09f));

        // Balloon, pivoting at the cap so it sways.
        Transform balloon = Container(t, "Balloon", new Vector2(0f, 0.29f));
        Part(balloon, "String", square, new Color(0.35f, 0.3f, 0.25f), 3 + orderOffset, new Vector2(0f, 0.2f), new Vector2(0.025f, 0.4f));
        GameObject knot = Part(balloon, "Knot", square, Color.white, 4 + orderOffset, new Vector2(0f, 0.42f), new Vector2(0.08f, 0.08f), 45f);
        GameObject body = Part(balloon, "BalloonBody", circle, Color.white, 4 + orderOffset, new Vector2(0f, 0.7f), new Vector2(0.48f, 0.58f));
        Part(balloon, "Shine", circle, new Color(1f, 1f, 1f, 0.6f), 5 + orderOffset, new Vector2(-0.1f, 0.8f), new Vector2(0.1f, 0.16f), 20f);

        var sway = balloon.gameObject.AddComponent<Flapper>();
        var swaySo = new SerializedObject(sway);
        swaySo.FindProperty("angle").floatValue = 6f;
        swaySo.FindProperty("speed").floatValue = 2f;
        swaySo.FindProperty("useUnscaledTime").boolValue = unscaledSway;
        swaySo.ApplyModifiedPropertiesWithoutUndo();

        var look = balloon.gameObject.AddComponent<RandomLook>();
        var lookSo = new SerializedObject(look);
        SerializedProperty targets = lookSo.FindProperty("tintTargets");
        targets.arraySize = 2;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = body.GetComponent<SpriteRenderer>();
        targets.GetArrayElementAtIndex(1).objectReferenceValue = knot.GetComponent<SpriteRenderer>();
        SetColors(lookSo.FindProperty("palette"), JugBalloonColors);
        lookSo.ApplyModifiedPropertiesWithoutUndo();

        return balloon;
    }

    public static MilkJug CreateMilkJugPrefab()
    {
        var go = new GameObject("MilkJug");
        Transform balloon = BuildMilkJugArt(go.transform);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.3f, 0.45f); // just the jug, so clicking it once attached is precise
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        MilkJug jug = go.AddComponent<MilkJug>();
        Assign(jug, "balloon", balloon.gameObject);
        SetFloat(jug, "driftSpeed", 0.5f); // floats up gently
        return SavePrefab(go).GetComponent<MilkJug>();
    }

    // ---------- Background clouds ----------

    // Also used by the Phase 3 builder.
    public static AltitudeObject CreateSkyPuffPrefab()
    {
        var go = new GameObject("SkyPuff");

        // Hand-drawn pixel-art cloud. The visible cloud is ~60% of the 256px image, so scale it up.
        Sprite cloud = LoadPixelSprite(CloudSpritePath);
        Part(go.transform, "Cloud", cloud, Color.white, -6, Vector2.zero, new Vector2(1.6f, 1.6f));

        go.AddComponent<AltitudeObject>();
        var look = go.AddComponent<RandomLook>();
        var lookSo = new SerializedObject(look);
        lookSo.FindProperty("scaleRange").vector2Value = new Vector2(0.6f, 1.6f);
        lookSo.FindProperty("randomFlipX").boolValue = true; // mirror half of them so they don't all look the same
        lookSo.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(go).GetComponent<AltitudeObject>();
    }

    private static void SetColors(SerializedProperty list, Color[] colors)
    {
        list.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
            list.GetArrayElementAtIndex(i).colorValue = colors[i];
    }

    // ---------- Bird ----------

    // Also used by the Phase 3 builder, so fresh builds get the nice bird too.
    public static Bird CreateBirdPrefab()
    {
        LoadSprites();

        Color body = ArtPalette.IronDark;
        Color wing = ArtPalette.Iron;

        var go = new GameObject("Bird");
        Transform t = go.transform;

        Part(t, "Tail", square, body, 2, new Vector2(-0.3f, 0.02f), new Vector2(0.18f, 0.18f), 45f);
        Part(t, "Body", circle, body, 3, Vector2.zero, new Vector2(0.55f, 0.32f));
        Part(t, "Beak", square, ArtPalette.Flame, 3, new Vector2(0.43f, 0.08f), new Vector2(0.12f, 0.12f), 45f);
        Part(t, "Head", circle, body, 4, new Vector2(0.28f, 0.1f), new Vector2(0.28f, 0.28f));
        Part(t, "Eye", circle, Color.white, 5, new Vector2(0.32f, 0.14f), new Vector2(0.08f, 0.08f));
        Part(t, "Pupil", circle, Color.black, 6, new Vector2(0.335f, 0.14f), new Vector2(0.04f, 0.04f));

        Transform wingPivot = Container(t, "WingPivot", new Vector2(-0.02f, 0.06f));
        wingPivot.gameObject.AddComponent<Flapper>();
        Part(wingPivot, "Wing", circle, wing, 5, new Vector2(-0.12f, 0.1f), new Vector2(0.42f, 0.14f), 15f);

        AddOutlines(t, 0.05f, 1, false);

        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.28f;
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<Bird>();
        return SavePrefab(go).GetComponent<Bird>();
    }
}
