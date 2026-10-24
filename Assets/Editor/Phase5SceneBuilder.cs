using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BuilderUtils;

public static class Phase5SceneBuilder
{
    [MenuItem("Tools/Up Up Away/Install Shop")]
    public static void InstallShop()
    {
        if (!OpenMainScene(out Scene scene))
            return;

        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        AltitudeMeter altitudeMeter = Object.FindAnyObjectByType<AltitudeMeter>();
        if (gameManager == null || altitudeMeter == null)
        {
            EditorUtility.DisplayDialog(
                "Missing Phase 4 objects",
                "Build Phase 4 first so the scene has a GameManager and AltitudeMeter.",
                "OK");
            return;
        }

        if (gameManager.GetComponent<ShopManager>() == null)
            gameManager.gameObject.AddComponent<ShopManager>();

        SaveScene(scene);
        Debug.Log("Shop installed in " + ScenePath + ".");
    }
}
