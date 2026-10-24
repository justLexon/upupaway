using UnityEngine;

// Plays a list of sprites (e.g. a sliced sprite sheet) on repeat.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 10f;
    [SerializeField] private bool useUnscaledTime = false; // true = keeps animating while paused

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int frame;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2 || framesPerSecond <= 0f)
            return;

        timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameTime = 1f / framesPerSecond;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            frame = (frame + 1) % frames.Length;
            spriteRenderer.sprite = frames[frame];
        }
    }

    public void SetFramesForSceneBuilder(Sprite[] newFrames) => frames = newFrames;
}
