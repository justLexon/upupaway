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
    private static Sprite ring;

    [MenuItem("Tools/Up Up Away/Build Visuals")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        LoadSprites();

        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        SteeringWheel wheel = Object.FindAnyObjectByType<SteeringWheel>();
        Furnace furnace = Object.FindAnyObjectByType<Furnace>();
        GameObject cabinZone = GameObject.Find("CabinZone");

        // Cabin layout: wheel in the middle, furnace + coal on the left, everything standing on the floor.
        if (cabinZone != null) BuildCabinFloor(cabinZone.transform);
        if (wheel != null) wheel.transform.localPosition = new Vector3(0f, FloorTop + 2.75f, 0f);
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

        SaveScene(scene);
        Debug.Log("Visuals built. Press Play!");
    }

    private const float FloorTop = -3.6f; // cabin-local Y where things stand

    private static void LoadSprites()
    {
        square = SquareSprite();
        circle = GetOrCreateCircleSprite();
        ring = GetOrCreateRingSprite();
    }

    // ---------- Cabin floor ----------

    private static void BuildCabinFloor(Transform cabin)
    {
        DestroyChild(cabin, "CabinFloor");
        Transform floor = Container(cabin, "CabinFloor");

        Color plank = new Color(0.42f, 0.27f, 0.14f);
        Color gap = new Color(0.25f, 0.15f, 0.07f);
        Color trim = new Color(0.3f, 0.18f, 0.09f);

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

        Color canvas = new Color(0.9f, 0.82f, 0.64f);
        Color canvasLight = new Color(0.97f, 0.92f, 0.78f);
        Color stripe = new Color(0.72f, 0.2f, 0.2f);
        Color wood = new Color(0.55f, 0.34f, 0.18f);
        Color woodDark = new Color(0.33f, 0.19f, 0.09f);
        Color iron = new Color(0.2f, 0.2f, 0.22f);
        Color rope = new Color(0.4f, 0.32f, 0.22f);

        // Tail fins (behind the envelope)
        Part(v, "FinTop", square, stripe, 0, new Vector2(-1.6f, 1.7f), new Vector2(0.5f, 0.5f), 45f);
        Part(v, "FinBottom", square, stripe, 0, new Vector2(-1.6f, 0.6f), new Vector2(0.5f, 0.5f), 45f);
        Part(v, "FinBack", square, stripe, 0, new Vector2(-1.8f, 1.15f), new Vector2(0.45f, 0.45f), 45f);

        // Envelope (the balloon)
        Part(v, "Envelope", circle, canvas, 1, new Vector2(0f, 1.15f), new Vector2(3.6f, 1.5f));
        Part(v, "Highlight", circle, canvasLight, 2, new Vector2(-0.2f, 1.45f), new Vector2(2.4f, 0.45f));
        Part(v, "Stripe", square, stripe, 3, new Vector2(0f, 1.1f), new Vector2(3.3f, 0.16f));
        Part(v, "NoseCap", circle, stripe, 3, new Vector2(1.72f, 1.13f), new Vector2(0.18f, 0.5f));

        // Ropes from envelope to gondola
        Part(v, "RopeL", square, rope, 0, new Vector2(-0.75f, 0.32f), new Vector2(0.04f, 0.42f), -25f);
        Part(v, "RopeR", square, rope, 0, new Vector2(0.75f, 0.32f), new Vector2(0.04f, 0.42f), 25f);
        Part(v, "RopeML", square, rope, 0, new Vector2(-0.25f, 0.3f), new Vector2(0.04f, 0.35f));
        Part(v, "RopeMR", square, rope, 0, new Vector2(0.25f, 0.3f), new Vector2(0.04f, 0.35f));

        // Smokestack
        Part(v, "Smokestack", square, iron, 1, new Vector2(1.0f, 0.3f), new Vector2(0.14f, 0.35f));
        Part(v, "SmokestackRim", square, Color.black, 1, new Vector2(1.0f, 0.48f), new Vector2(0.2f, 0.06f));

        // Gondola
        Part(v, "Keel", circle, wood, 1, new Vector2(0f, -0.42f), new Vector2(2.0f, 0.4f));
        Part(v, "Gondola", square, wood, 2, new Vector2(0f, -0.15f), new Vector2(2.0f, 0.6f));
        Part(v, "TrimTop", square, woodDark, 3, new Vector2(0f, 0.15f), new Vector2(2.15f, 0.1f));
        Part(v, "TrimBottom", square, woodDark, 3, new Vector2(0f, -0.44f), new Vector2(2.0f, 0.07f));

        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 0.55f;
            Part(v, "WindowFrame" + i, circle, woodDark, 3, new Vector2(x, -0.12f), new Vector2(0.3f, 0.3f));
            Part(v, "Window" + i, circle, new Color(1f, 0.88f, 0.5f), 4, new Vector2(x, -0.12f), new Vector2(0.22f, 0.22f));
        }

        // Rear propeller
        Part(v, "PropHub", circle, iron, 4, new Vector2(-1.1f, -0.15f), new Vector2(0.16f, 0.16f));
        GameObject blade = Part(v, "PropBlade", square, new Color(0.55f, 0.55f, 0.6f), 3,
            new Vector2(-1.16f, -0.15f), new Vector2(0.07f, 0.65f));
        blade.AddComponent<PropellerSpin>();
    }

    // ---------- Steering wheel ----------

    private static void BuildWheel(SteeringWheel wheel)
    {
        Transform root = wheel.transform;
        DestroyChild(root, "Spoke");
        DestroyChild(root, "Visuals");

        Color wood = new Color(0.5f, 0.3f, 0.14f);
        Color woodLight = new Color(0.72f, 0.5f, 0.27f);
        Color brass = new Color(0.86f, 0.7f, 0.3f);

        // The root sprite becomes the rim.
        var rim = root.GetComponent<SpriteRenderer>();
        rim.sprite = ring;
        rim.color = wood;
        rim.sortingOrder = 6;

        var collider = root.GetComponent<CircleCollider2D>();
        if (collider != null)
            collider.radius = 0.62f; // include the handles

        Transform v = Container(root, "Visuals");

        // 4 spokes crossing the hub = 8 handles sticking out past the rim.
        for (int i = 0; i < 4; i++)
            Part(v, "Spoke" + i, square, woodLight, 5, Vector2.zero, new Vector2(0.07f, 1.2f), i * 45f);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 0.6f;
            Part(v, "Handle" + i, circle, woodLight, 6, pos, new Vector2(0.12f, 0.12f));
        }

        Part(v, "Hub", circle, brass, 7, Vector2.zero, new Vector2(0.24f, 0.24f));
        Part(v, "HubCenter", circle, wood, 8, Vector2.zero, new Vector2(0.1f, 0.1f));
        Part(v, "Marker", square, brass, 7, new Vector2(0f, 0.6f), new Vector2(0.08f, 0.12f)); // shows "straight ahead"

        // Stand: a sibling, so it doesn't rotate with the wheel.
        Transform parent = root.parent;
        DestroyChild(parent, "WheelStand");
        Transform stand = Container(parent, "WheelStand", root.localPosition);
        Part(stand, "Post", square, new Color(0.3f, 0.18f, 0.08f), 4, new Vector2(0f, -1.3f), new Vector2(0.4f, 2.6f));
        Part(stand, "Base", square, new Color(0.25f, 0.15f, 0.07f), 4, new Vector2(0f, -2.6f), new Vector2(1.4f, 0.3f));
    }

    // ---------- Furnace + coal pile ----------

    private static void BuildFurnace(Furnace furnace)
    {
        Transform root = furnace.transform; // scaled 2x2, so 1 local unit = 2 world units
        DestroyChild(root, "Visuals");

        Color iron = new Color(0.2f, 0.2f, 0.22f);
        Color ironDark = new Color(0.11f, 0.11f, 0.12f);
        Color rivet = new Color(0.42f, 0.42f, 0.45f);

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
        Part(v, "GaugeFace", circle, new Color(0.95f, 0.92f, 0.82f), 7, new Vector2(-0.28f, 0.3f), new Vector2(0.17f, 0.17f));
        Part(v, "GaugeNeedle", square, new Color(0.7f, 0.15f, 0.1f), 8, new Vector2(-0.28f, 0.3f), new Vector2(0.02f, 0.08f), -40f);

        // Door: the glow is what Furnace tints by heat.
        Part(v, "DoorFrame", square, ironDark, 6, new Vector2(0f, -0.08f), new Vector2(0.62f, 0.46f));
        GameObject glow = Part(v, "FireGlow", square, new Color(0.2f, 0.2f, 0.2f), 7, new Vector2(0f, -0.08f), new Vector2(0.52f, 0.36f));
        Assign(furnace, "spriteRenderer", glow.GetComponent<SpriteRenderer>());

        Transform flamesRoot = Container(v, "Flames", new Vector2(0f, -0.14f));
        var flames = new Transform[3];
        Color[] flameColors = { new Color(1f, 0.45f, 0.1f), new Color(1f, 0.8f, 0.2f), new Color(1f, 0.45f, 0.1f) };
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

    public static MilkJug CreateMilkJugPrefab()
    {
        LoadSprites();

        var go = new GameObject("MilkJug");
        Transform t = go.transform;

        // Same jug as before, just laid out at scale 1 so the balloon isn't stretched.
        Part(t, "Body", square, new Color(0.97f, 0.97f, 0.95f), 4, Vector2.zero, new Vector2(0.3f, 0.45f));
        Part(t, "Cap", square, new Color(0.2f, 0.45f, 0.9f), 5, new Vector2(0f, 0.2475f), new Vector2(0.18f, 0.09f));

        // Balloon, pivoting at the cap so it sways.
        Transform balloon = Container(t, "Balloon", new Vector2(0f, 0.29f));
        Part(balloon, "String", square, new Color(0.35f, 0.3f, 0.25f), 3, new Vector2(0f, 0.2f), new Vector2(0.025f, 0.4f));
        GameObject knot = Part(balloon, "Knot", square, Color.white, 4, new Vector2(0f, 0.42f), new Vector2(0.08f, 0.08f), 45f);
        GameObject body = Part(balloon, "BalloonBody", circle, Color.white, 4, new Vector2(0f, 0.7f), new Vector2(0.48f, 0.58f));
        Part(balloon, "Shine", circle, new Color(1f, 1f, 1f, 0.6f), 5, new Vector2(-0.1f, 0.8f), new Vector2(0.1f, 0.16f), 20f);

        var sway = balloon.gameObject.AddComponent<Flapper>();
        SetFloat(sway, "angle", 6f);
        SetFloat(sway, "speed", 2f);

        var look = balloon.gameObject.AddComponent<RandomLook>();
        var lookSo = new SerializedObject(look);
        SerializedProperty targets = lookSo.FindProperty("tintTargets");
        targets.arraySize = 2;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = body.GetComponent<SpriteRenderer>();
        targets.GetArrayElementAtIndex(1).objectReferenceValue = knot.GetComponent<SpriteRenderer>();
        SetColors(lookSo.FindProperty("palette"), JugBalloonColors);
        lookSo.ApplyModifiedPropertiesWithoutUndo();

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
        LoadSprites();

        Color top = new Color(0.98f, 0.99f, 1f);
        Color shade = new Color(0.82f, 0.88f, 0.96f);

        var go = new GameObject("SkyPuff");
        Transform t = go.transform;

        // Bumps on top + a flat bottom. A shaded copy sits slightly lower and behind.
        (Vector2 pos, Vector2 size)[] shapes =
        {
            (new Vector2(0f, 0.15f), new Vector2(1.2f, 1.1f)),
            (new Vector2(-0.7f, -0.05f), new Vector2(0.9f, 0.8f)),
            (new Vector2(0.7f, -0.05f), new Vector2(0.95f, 0.85f)),
            (new Vector2(-0.3f, 0.35f), new Vector2(0.8f, 0.8f)),
            (new Vector2(0.4f, 0.3f), new Vector2(0.9f, 0.85f)),
        };
        for (int i = 0; i < shapes.Length; i++)
        {
            Part(t, "Shade" + i, circle, shade, -7, shapes[i].pos + new Vector2(0.05f, -0.1f), shapes[i].size);
            Part(t, "Puff" + i, circle, top, -6, shapes[i].pos, shapes[i].size);
        }
        Part(t, "ShadeBase", square, shade, -7, new Vector2(0.05f, -0.4f), new Vector2(1.7f, 0.4f));
        Part(t, "Base", square, top, -6, new Vector2(0f, -0.3f), new Vector2(1.7f, 0.4f));

        go.AddComponent<AltitudeObject>();
        var look = go.AddComponent<RandomLook>();
        var lookSo = new SerializedObject(look);
        lookSo.FindProperty("scaleRange").vector2Value = new Vector2(0.6f, 1.6f);
        lookSo.FindProperty("randomFlipX").boolValue = true;
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

        Color body = new Color(0.22f, 0.22f, 0.28f);
        Color wing = new Color(0.32f, 0.32f, 0.4f);

        var go = new GameObject("Bird");
        Transform t = go.transform;

        Part(t, "Tail", square, body, 2, new Vector2(-0.3f, 0.02f), new Vector2(0.18f, 0.18f), 45f);
        Part(t, "Body", circle, body, 3, Vector2.zero, new Vector2(0.55f, 0.32f));
        Part(t, "Beak", square, new Color(1f, 0.6f, 0.15f), 3, new Vector2(0.43f, 0.08f), new Vector2(0.12f, 0.12f), 45f);
        Part(t, "Head", circle, body, 4, new Vector2(0.28f, 0.1f), new Vector2(0.28f, 0.28f));
        Part(t, "Eye", circle, Color.white, 5, new Vector2(0.32f, 0.14f), new Vector2(0.08f, 0.08f));
        Part(t, "Pupil", circle, Color.black, 6, new Vector2(0.335f, 0.14f), new Vector2(0.04f, 0.04f));

        Transform wingPivot = Container(t, "WingPivot", new Vector2(-0.02f, 0.06f));
        wingPivot.gameObject.AddComponent<Flapper>();
        Part(wingPivot, "Wing", circle, wing, 5, new Vector2(-0.12f, 0.1f), new Vector2(0.42f, 0.14f), 15f);

        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.28f;
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<Bird>();
        return SavePrefab(go).GetComponent<Bird>();
    }
}
