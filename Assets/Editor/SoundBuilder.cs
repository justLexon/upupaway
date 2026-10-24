using UnityEditor;
using UnityEngine;
using static BuilderUtils;

// Tools > Up Up Away > Build Sounds.
// Adds GameAudio to Managers and hooks up the clips from Assets/Audio. Safe to re-run (keeps volume tweaks).
public static class SoundBuilder
{
    private const string AudioFolder = "Assets/Audio";

    [MenuItem("Tools/Up Up Away/Build Sounds")]
    public static void Build()
    {
        if (!OpenMainScene(out var scene)) return;

        GameObject managers = GameObject.Find("Managers");
        if (managers == null)
        {
            EditorUtility.DisplayDialog("Missing Managers", "Couldn't find the Managers object. Build the earlier phases first.", "OK");
            return;
        }

        GameAudio audio = managers.GetComponent<GameAudio>();
        if (audio == null)
            audio = managers.AddComponent<GameAudio>();

        Assign(audio, "furnace", Object.FindAnyObjectByType<Furnace>());
        Assign(audio, "wheel", Object.FindAnyObjectByType<SteeringWheel>());
        Assign(audio, "wind", Object.FindAnyObjectByType<WindGusts>());
        Assign(audio, "weather", Object.FindAnyObjectByType<WeatherSystem>());

        Assign(audio, "furnaceLoop", Clip("Furnace-Burning"));
        Assign(audio, "rainLoop", Clip("Rain-Sound"));
        Assign(audio, "wheelLoop", Clip("Steering-wheel-turning"));
        Assign(audio, "birdsLoop", Clip("Bird-Noisess"));
        Assign(audio, "gustClip", Clip("Gust-of-Wind"));
        Assign(audio, "lightningClip", Clip("Lightning-Strike"));

        SaveScene(scene);
        Debug.Log("Sounds added. Press Play!");
    }

    private static AudioClip Clip(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{name}.mp3");
        if (clip == null)
            Debug.LogWarning($"Missing sound: {AudioFolder}/{name}.mp3 (that sound will be skipped).");
        return clip;
    }
}
