using UnityEditor;
using UnityEngine;
using static BuilderUtils;

// Tools > Up Up Away > Build Wind Gusts.
// Adds the WindGusts manager to the Main scene. Safe to re-run: it reuses the existing one and re-links it.
public static class WindBuilder
{
    [MenuItem("Tools/Up Up Away/Build Wind Gusts")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        ShipController ship = Object.FindAnyObjectByType<ShipController>();
        GameObject outsideZone = GameObject.Find("OutsideZone");
        GameObject managers = GameObject.Find("Managers");
        if (ship == null || outsideZone == null || managers == null)
        {
            EditorUtility.DisplayDialog("Missing objects", "Couldn't find the Ship, OutsideZone, or Managers.", "OK");
            return;
        }

        WindGusts wind = managers.GetComponent<WindGusts>();
        if (wind == null)
            wind = managers.AddComponent<WindGusts>();

        Assign(wind, "ship", ship);
        Assign(wind, "streakSprite", SquareSprite());
        Assign(wind, "streakParent", outsideZone.transform);

        SaveScene(scene);
        Debug.Log("Wind gusts built. Press Play!");
    }
}
