using UnityEditor;
using UnityEngine;
using static BuilderUtils;

// Tools > Up Up Away > Build Phase 3 Scene.
// Additive: adds Phase 3 to the existing Main scene, keeping any hand edits.
public static class Phase3SceneBuilder
{
    [MenuItem("Tools/Up Up Away/Build Phase 3 Scene")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        if (IsBuilt())
        {
            EditorUtility.DisplayDialog("Already built", "This scene already has Phase 3 (a SteeringWheel). Nothing to do.", "OK");
            return;
        }

        if (!AddToOpenScene()) return;

        SaveScene(scene);
        Debug.Log("Phase 3 built in " + ScenePath + ". Press Play!");
    }

    public static bool IsBuilt() => Object.FindAnyObjectByType<SteeringWheel>() != null;

    // Adds Phase 3 to whatever scene is open. Later phases call this if Phase 3 is missing.
    public static bool AddToOpenScene()
    {
        GameObject cabinZone = GameObject.Find("CabinZone");
        GameObject outsideZone = GameObject.Find("OutsideZone");
        GameObject managers = GameObject.Find("Managers");
        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        if (cabinZone == null || outsideZone == null || managers == null || ship == null)
        {
            EditorUtility.DisplayDialog("Missing objects",
                "Couldn't find CabinZone, OutsideZone, Managers, or the Ship. Is this the Phase 2 scene?", "OK");
            return false;
        }

        Sprite square = SquareSprite();

        // Steering wheel (cabin, right of the furnace). The spoke makes rotation visible.
        GameObject wheel = CreateSprite("SteeringWheel", square, new Color(0.45f, 0.3f, 0.15f), 5);
        wheel.transform.SetParent(cabinZone.transform, false);
        wheel.transform.localPosition = new Vector3(4f, -1f, 0f);
        wheel.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        wheel.AddComponent<CircleCollider2D>();
        SteeringWheel wheelComp = wheel.AddComponent<SteeringWheel>();

        GameObject spoke = CreateSprite("Spoke", square, new Color(0.85f, 0.7f, 0.4f), 6);
        spoke.transform.SetParent(wheel.transform, false);
        spoke.transform.localScale = new Vector3(0.15f, 1f, 1f);

        // Ship physics: Dynamic with no gravity and frozen, so triggers fire reliably while we move it by transform.
        GameObject shipGO = ship.gameObject;
        if (shipGO.GetComponent<BoxCollider2D>() == null)
            shipGO.AddComponent<BoxCollider2D>();
        Rigidbody2D shipBody = shipGO.GetComponent<Rigidbody2D>();
        if (shipBody == null)
            shipBody = shipGO.AddComponent<Rigidbody2D>();
        shipBody.bodyType = RigidbodyType2D.Dynamic;
        shipBody.gravityScale = 0f;
        shipBody.constraints = RigidbodyConstraints2D.FreezeAll;

        Assign(ship, "wheel", wheelComp);

        // Prefabs
        Bird birdPrefab = VisualsBuilder.CreateBirdPrefab();
        AltitudeObject puffPrefab = VisualsBuilder.CreateSkyPuffPrefab();

        // Spawners: one for obstacles, one for background puffs that make climbing feel like movement.
        ObstacleSpawner obstacleSpawner = managers.AddComponent<ObstacleSpawner>();
        Assign(obstacleSpawner, "ship", ship);
        Assign(obstacleSpawner, "spawnParent", outsideZone.transform);
        SetSpawnEntries(obstacleSpawner, (birdPrefab, 1f));

        ObstacleSpawner decorSpawner = managers.AddComponent<ObstacleSpawner>();
        Assign(decorSpawner, "ship", ship);
        Assign(decorSpawner, "spawnParent", outsideZone.transform);
        SetSpawnEntries(decorSpawner, (puffPrefab, 1f));
        SetFloat(decorSpawner, "minSpacing", 12f);
        SetFloat(decorSpawner, "maxSpacing", 22f);
        SetFloat(decorSpawner, "xLimit", 9f);

        return true;
    }
}
