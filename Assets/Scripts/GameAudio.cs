using UnityEngine;

// All the game's sounds in one place. Looping sounds fade with what's happening (furnace heat, storm, wheel speed,
// birds around); one-shots play on gusts and lightning. Tweak the volumes here.
public class GameAudio : MonoBehaviour
{
    [Header("Game objects")]
    [SerializeField] private Furnace furnace;
    [SerializeField] private SteeringWheel wheel;
    [SerializeField] private WindGusts wind;
    [SerializeField] private WeatherSystem weather;

    [Header("Loops")]
    [SerializeField] private AudioClip furnaceLoop;
    [SerializeField, Range(0f, 1f)] private float furnaceVolume = 0.5f;
    [SerializeField] private AudioClip rainLoop;
    [SerializeField, Range(0f, 1f)] private float rainVolume = 0.7f;
    [SerializeField] private AudioClip wheelLoop;
    [SerializeField, Range(0f, 1f)] private float wheelVolume = 0.6f;
    [SerializeField] private float wheelFullSpeed = 150f; // degrees/s for full volume
    [SerializeField] private AudioClip birdsLoop;
    [SerializeField, Range(0f, 1f)] private float birdsVolume = 0.35f;
    [SerializeField] private int birdsForFullVolume = 3;

    [Header("One-shots")]
    [SerializeField] private AudioClip gustClip;
    [SerializeField, Range(0f, 1f)] private float gustVolume = 0.6f;
    [SerializeField] private AudioClip lightningClip;
    [SerializeField, Range(0f, 1f)] private float lightningVolume = 0.9f;

    [SerializeField] private float fadeSpeed = 2f; // volume change per second

    private AudioSource furnaceSource, rainSource, wheelSource, birdsSource, oneShots;

    private void Awake()
    {
        furnaceSource = CreateLoop(furnaceLoop);
        rainSource = CreateLoop(rainLoop);
        wheelSource = CreateLoop(wheelLoop);
        birdsSource = CreateLoop(birdsLoop);
        oneShots = gameObject.AddComponent<AudioSource>();
        oneShots.playOnAwake = false;
    }

    private void OnEnable()
    {
        if (wind != null) wind.OnGust += PlayGust;
        if (weather != null) weather.OnLightning += PlayLightning;
    }

    private void OnDisable()
    {
        if (wind != null) wind.OnGust -= PlayGust;
        if (weather != null) weather.OnLightning -= PlayLightning;
    }

    private void Update()
    {
        bool over = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        float mute = over ? 0f : 1f;

        // Furnace: a low rumble even when cold, roaring when hot.
        float heat = furnace != null ? furnace.Heat01 : 0f;
        Fade(furnaceSource, Mathf.Lerp(0.25f, 1f, heat) * furnaceVolume * mute);

        float storm = weather != null ? weather.StormIntensity : 0f;
        Fade(rainSource, storm * rainVolume * mute);

        float turning = wheel != null ? Mathf.Clamp01(wheel.TurnSpeed / wheelFullSpeed) : 0f;
        Fade(wheelSource, paused ? 0f : turning * wheelVolume * mute);

        float daylight = DayNightCycle.Instance != null ? 1f - DayNightCycle.Instance.Darkness : 1f;
        float birds = Mathf.Clamp01(Bird.ActiveCount / (float)Mathf.Max(1, birdsForFullVolume));
        Fade(birdsSource, birds * daylight * birdsVolume * mute);
    }

    private void PlayGust(float strength01)
    {
        if (gustClip != null)
            oneShots.PlayOneShot(gustClip, Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(strength01)) * gustVolume);
    }

    private void PlayLightning(bool hitShip)
    {
        if (lightningClip != null)
            oneShots.PlayOneShot(lightningClip, (hitShip ? 1f : 0.75f) * lightningVolume);
    }

    private AudioSource CreateLoop(AudioClip clip)
    {
        if (clip == null)
            return null;
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = 0f;
        source.playOnAwake = false;
        source.time = Random.Range(0f, clip.length * 0.9f); // don't always start at the same spot
        source.Play();
        return source;
    }

    // Unscaled time, so sounds still fade out while the game is paused or over.
    private void Fade(AudioSource source, float target)
    {
        if (source != null)
            source.volume = Mathf.MoveTowards(source.volume, target, fadeSpeed * Time.unscaledDeltaTime);
    }
}
