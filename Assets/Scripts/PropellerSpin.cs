using UnityEngine;

// Side-view propeller: squashes the blade to fake a spin.
public class PropellerSpin : MonoBehaviour
{
    [SerializeField] private float speed = 25f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        Vector3 scale = baseScale;
        scale.y *= Mathf.Max(0.1f, Mathf.Abs(Mathf.Cos(Time.time * speed)));
        transform.localScale = scale;
    }
}
