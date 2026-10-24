using UnityEngine;

// Randomizes how a spawned object looks: color from a palette, size, and facing.
public class RandomLook : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] tintTargets;
    [SerializeField] private Color[] palette;
    [SerializeField] private Vector2 scaleRange = Vector2.one;
    [SerializeField] private bool randomFlipX;

    private void Awake()
    {
        if (palette != null && palette.Length > 0)
        {
            Color color = palette[Random.Range(0, palette.Length)];
            foreach (SpriteRenderer target in tintTargets)
            {
                if (target != null)
                    target.color = color;
            }
        }

        Vector3 scale = transform.localScale * Random.Range(scaleRange.x, scaleRange.y);
        if (randomFlipX && Random.value < 0.5f)
            scale.x = -scale.x;
        transform.localScale = scale;
    }
}
