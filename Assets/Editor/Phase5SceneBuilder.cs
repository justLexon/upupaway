using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using static BuilderUtils;

// Tools > Up Up Away > Build Phase 5 Scene.
// Pitstops (shop + pause), coins/milk economy, and the new altitude meter + resource HUD.
// Safe to re-run: rebuilds the station, stall/display prefabs and HUD. Shop item assets are only created if missing,
// so price tweaks made in the Inspector survive.
public static class Phase5SceneBuilder
{
    private const string ShopFolder = "Assets/Shop";
    private const string ShopPrefabFolder = "Assets/Prefabs/Shop";
    private const int Order = 11; // above the cloud sea (10), so the market floats over it

    private static readonly Color Wood = ArtPalette.Wood;
    private static readonly Color WoodLight = ArtPalette.WoodLight;
    private static readonly Color WoodDark = ArtPalette.WoodDark;
    private static readonly Color Rope = ArtPalette.WoodDark;
    private static readonly Color BuyColor = ArtPalette.Moss;     // painted planks
    private static readonly Color SellColor = ArtPalette.Copper;
    private static readonly Color DepartColor = ArtPalette.Verdigris;

    private static Sprite square;
    private static Sprite circle;
    private static Sprite plank;
    private static TMP_FontAsset displayFont;
    private static TMP_FontAsset bodyFont;

    [MenuItem("Tools/Up Up Away/Build Phase 5 Scene")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        if (!Phase4SceneBuilder.IsBuilt())
        {
            if (!Phase3SceneBuilder.IsBuilt() && !Phase3SceneBuilder.AddToOpenScene()) return;
            if (!Phase4SceneBuilder.AddToOpenScene()) return;
        }

        if (!AddToOpenScene()) return;

        SaveScene(scene);
        Debug.Log("Phase 5 built: pitstops, shop and new HUD. Press Play!");
    }

    public static bool AddToOpenScene()
    {
        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        CloudSea cloudSea = Object.FindAnyObjectByType<CloudSea>();
        Furnace furnace = Object.FindAnyObjectByType<Furnace>();
        CameraSwitcher switcher = Object.FindAnyObjectByType<CameraSwitcher>();
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        GameObject outsideZone = GameObject.Find("OutsideZone");
        GameObject managers = GameObject.Find("Managers");
        GameObject canvas = GameObject.Find("Canvas");
        GameObject pipFrame = GameObject.Find("PipFrame");

        if (ship == null || cloudSea == null || furnace == null || switcher == null || gameManager == null
            || outsideZone == null || managers == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Missing objects",
                "Couldn't find the Ship, CloudSea, Furnace, CameraSwitcher, GameManager, OutsideZone, Managers, or Canvas.", "OK");
            return false;
        }

        square = SquareSprite();
        circle = GetOrCreateCircleSprite();
        plank = ArtStyleAssets.WoodPlank;
        displayFont = ArtStyleAssets.DisplayFont;
        bodyFont = ArtStyleAssets.BodyFont;
        Directory.CreateDirectory(ShopFolder);
        Directory.CreateDirectory(ShopPrefabFolder);

        // Items: data only. Add more by making another ShopItem asset and adding it to the station.
        ShopItem milk = GetOrCreateItem("Milk", item =>
        {
            item.displayName = "Milk";
            item.resource = ResourceType.Milk;
            item.amountPerTrade = 1;
            item.canBuy = false;
            item.canSell = true;
            item.sellPrice = 4;
        });
        ShopItem coal = GetOrCreateItem("Coal", item =>
        {
            item.displayName = "Coal";
            item.resource = ResourceType.Coal;
            item.amountPerTrade = 5;
            item.canBuy = true;
            item.buyPrice = 5;
            item.canSell = true;
            item.sellPrice = 3;
        });
        milk.displayPrefab = CreateMilkDisplay();
        coal.displayPrefab = CreateCoalDisplay();
        EditorUtility.SetDirty(milk);
        EditorUtility.SetDirty(coal);
        AssetDatabase.SaveAssets();

        ShopStall stallPrefab = CreateStallPrefab();
        PitstopStation station = BuildStation(outsideZone.transform, stallPrefab, new[] { milk, coal });

        PitstopManager pitstops = managers.GetComponent<PitstopManager>();
        if (pitstops == null)
            pitstops = managers.AddComponent<PitstopManager>();
        Assign(pitstops, "ship", ship);
        Assign(pitstops, "station", station);
        Assign(pitstops, "cameraSwitcher", switcher);
        Assign(pitstops, "pipFrame", pipFrame);

        AltitudeMeter meter = HudBuilder.BuildAltitudeMeter(canvas.transform, ship, cloudSea, pitstops);
        ResourceHUD hud = HudBuilder.BuildResourceHUD(canvas.transform, furnace);

        // Keep the game over panel drawn on top of the HUD.
        if (GetReference(gameManager, "gameOverPanel") is GameObject gameOverPanel)
        {
            int index = gameOverPanel.transform.GetSiblingIndex();
            meter.transform.SetSiblingIndex(index);
            hud.transform.SetSiblingIndex(index);
        }

        return true;
    }

    private static ShopItem GetOrCreateItem(string name, System.Action<ShopItem> setup)
    {
        string path = $"{ShopFolder}/{name}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ShopItem>(path);
        if (item != null)
            return item;

        item = ScriptableObject.CreateInstance<ShopItem>();
        setup(item);
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    // ---------- Item displays (origin = deck surface) ----------

    private static GameObject CreateMilkDisplay()
    {
        var go = new GameObject("MilkDisplay");
        Transform art = Container(go.transform, "Jug", new Vector2(0f, VisualsBuilder.MilkBottleHalfHeight)); // bottle stands on the deck
        VisualsBuilder.BuildMilkJugArt(art, Order + 2 - 4, unscaledSway: true);
        return SaveShopPrefab(go);
    }

    private static GameObject CreateCoalDisplay()
    {
        var go = new GameObject("CoalDisplay");
        Transform t = go.transform;

        SlicedPart(t, "Crate", plank, Wood, Order + 3, new Vector2(0f, 0.3f), new Vector2(1f, 0.62f));
        Part(t, "Brace", square, ArtPalette.WithAlpha(ArtPalette.Ink, 0.7f), Order + 4, new Vector2(0f, 0.3f), new Vector2(0.9f, 0.06f), -28f);

        // Lumps heaped on top.
        (float x, float y, float size)[] lumps =
        {
            (-0.3f, 0.66f, 0.28f), (0f, 0.68f, 0.3f), (0.3f, 0.66f, 0.27f), (-0.15f, 0.84f, 0.26f), (0.15f, 0.84f, 0.25f),
        };
        // Behind the crate's front edge, so the coal looks heaped inside it.
        for (int i = 0; i < lumps.Length; i++)
            VisualsBuilder.CoalLump(t, "Lump" + i, Order + 2, new Vector2(lumps[i].x, lumps[i].y), lumps[i].size, i % 2 == 1);
        return SaveShopPrefab(go);
    }

    // ---------- Stall (origin = deck top, stall center) ----------

    private static ShopStall CreateStallPrefab()
    {
        var go = new GameObject("ShopStall");
        Transform t = go.transform;

        Transform anchor = Container(t, "DisplayAnchor");

        // Price sign on two posts.
        Part(t, "PostL", square, WoodDark, Order, new Vector2(-0.8f, 0.85f), new Vector2(0.06f, 1.7f));
        Part(t, "PostR", square, WoodDark, Order, new Vector2(0.8f, 0.85f), new Vector2(0.06f, 1.7f));
        SlicedPart(t, "Sign", plank, WoodLight, Order + 2, new Vector2(0f, 2.1f), new Vector2(1.9f, 0.85f));
        TextMeshPro title = WorldText(t, "Title", "ITEM", new Vector2(0f, 2.27f), new Vector2(1.5f, 0.34f), ArtPalette.Ink, Order + 3, 3.5f, displayFont);
        TextMeshPro info = WorldText(t, "Info", "You have 0", new Vector2(0f, 1.95f), new Vector2(1.5f, 0.24f), ArtPalette.Ink, Order + 3, 2.4f, bodyFont);

        // Buy/Sell signs hanging under the deck (deck is 0.5 thick).
        ShopButton buy = CreateButton(t, "BuyButton", new Vector2(-0.52f, -0.98f), new Vector2(0.95f, 0.6f), BuyColor, "BUY", 0.18f);
        ShopButton sell = CreateButton(t, "SellButton", new Vector2(0.52f, -0.98f), new Vector2(0.95f, 0.6f), SellColor, "SELL", 0.18f);

        ShopStall stall = go.AddComponent<ShopStall>();
        Assign(stall, "displayAnchor", anchor);
        Assign(stall, "titleText", title);
        Assign(stall, "infoText", info);
        Assign(stall, "buyButton", buy);
        Assign(stall, "sellButton", sell);
        return SaveShopPrefab(go).GetComponent<ShopStall>();
    }

    // ---------- Station (scene object) ----------

    private static PitstopStation BuildStation(Transform outsideZone, ShopStall stallPrefab, ShopItem[] items)
    {
        DestroyChild(outsideZone, "PitstopStation");
        Transform root = Container(outsideZone, "PitstopStation", new Vector2(16f, 0f)); // starts off screen
        const float deckTop = -1.35f;

        // Balloons holding the market up: the pixel-art milk balloon (without its string), tied with ropes.
        Sprite balloonSprite = VisualsBuilder.BalloonBodySprite;
        (Vector2 pos, float width, Color color)[] balloons =
        {
            (new Vector2(-2.7f, 2.3f), 1.3f, new Color(0.25f, 0.55f, 0.95f)),
            (new Vector2(0f, 2.8f), 1.5f, new Color(1f, 0.85f, 0.25f)),
            (new Vector2(2.5f, 2.25f), 1.2f, new Color(1f, 0.5f, 0.75f)),
        };
        for (int i = 0; i < balloons.Length; i++)
        {
            var (pos, width, color) = balloons[i];
            float scale = width / VisualsBuilder.BalloonBodyWidth;
            float bottom = pos.y - VisualsBuilder.BalloonBodyHeight / 2f * scale;
            if (i == 1)
            {
                // Middle balloon: two lines, with the Set Sail sign hanging between them.
                StringLine(root, "StringML", new Vector2(-0.35f, deckTop), bottom);
                StringLine(root, "StringMR", new Vector2(0.35f, deckTop), bottom);
            }
            else
            {
                StringLine(root, "String" + i, new Vector2(pos.x, deckTop), bottom);
            }
            Part(root, "Balloon" + i, balloonSprite, color, Order + 1, pos, new Vector2(scale, scale));
        }
        WorldText(root, "Banner", "PITSTOP", new Vector2(0f, 2.8f), new Vector2(1.3f, 0.45f), ArtPalette.Ink, Order + 3, 4f, displayFont);

        ShopButton depart = CreateButton(root, "DepartButton", new Vector2(0f, 1.6f), new Vector2(2.3f, 0.62f), DepartColor, "SET SAIL", 0f);

        // Deck
        SlicedPart(root, "Deck", plank, Wood, Order, new Vector2(0f, deckTop - 0.25f), new Vector2(7.9f, 0.55f));
        for (int i = 0; i < 9; i++)
            Part(root, "Seam" + i, square, ArtPalette.WithAlpha(ArtPalette.Ink, 0.6f), Order + 1,
                new Vector2(-3.2f + i * 0.8f, deckTop - 0.27f), new Vector2(0.03f, 0.36f));

        Transform stallRow = Container(root, "StallRow", new Vector2(0f, deckTop));

        PitstopStation station = root.gameObject.AddComponent<PitstopStation>();
        var so = new SerializedObject(station);
        SerializedProperty list = so.FindProperty("items");
        list.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        Assign(station, "stallPrefab", stallPrefab);
        Assign(station, "stallRow", stallRow);
        Assign(station, "departButton", depart);
        return station;
    }

    // ---------- Helpers ----------

    private static ShopButton CreateButton(Transform parent, string name, Vector2 position, Vector2 size, Color color,
        string text, float ropeLength)
    {
        Transform root = Container(parent, name, position);

        if (ropeLength > 0f)
        {
            float top = size.y / 2f;
            Part(root, "RopeL", square, Rope, Order, new Vector2(-size.x * 0.35f, top + ropeLength / 2f), new Vector2(0.03f, ropeLength));
            Part(root, "RopeR", square, Rope, Order, new Vector2(size.x * 0.35f, top + ropeLength / 2f), new Vector2(0.03f, ropeLength));
        }
        // A painted plank: the plank art is grayscale, so the tint is the paint color.
        GameObject background = SlicedPart(root, "Background", plank, color, Order + 4, Vector2.zero, size);
        TextMeshPro label = WorldText(root, "Label", text, Vector2.zero, size - new Vector2(0.2f, 0.12f), ArtPalette.Cream, Order + 5, 3f, displayFont);

        var collider = root.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        ShopButton button = root.gameObject.AddComponent<ShopButton>();
        Assign(button, "background", background.GetComponent<SpriteRenderer>());
        Assign(button, "label", label);
        var so = new SerializedObject(button);
        so.FindProperty("enabledColor").colorValue = color;
        so.FindProperty("disabledColor").colorValue = ArtPalette.Steel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    private static void StringLine(Transform parent, string name, Vector2 from, float toY)
    {
        float length = toY - from.y;
        Part(parent, name, square, Rope, Order - 1, new Vector2(from.x, from.y + length / 2f), new Vector2(0.03f, length));
    }

    // 9-sliced world sprite (drawn at `size` world units instead of being scaled).
    private static GameObject SlicedPart(Transform parent, string name, Sprite sprite, Color color, int order, Vector2 position, Vector2 size)
    {
        GameObject go = Part(parent, name, sprite, color, order, position, Vector2.one);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        return go;
    }

    private static TextMeshPro WorldText(Transform parent, string name, string text, Vector2 position, Vector2 size,
        Color color, int sortingOrder, float maxFontSize, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshPro>();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.5f;
        tmp.fontSizeMax = maxFontSize;
        tmp.sortingOrder = sortingOrder;
        tmp.rectTransform.sizeDelta = size;
        tmp.rectTransform.localPosition = position;
        return tmp;
    }

    private static GameObject SaveShopPrefab(GameObject go)
    {
        string path = $"{ShopPrefabFolder}/{go.name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
