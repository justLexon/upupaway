using UnityEngine;

// Swings back and forth on Z (bird wings, balloon sway).
public class Flapper : MonoBehaviour
{
    [SerializeField] private float angle = 35f; // degrees each way
    [SerializeField] private float speed = 10f;
    [SerializeField] private bool useUnscaledTime; // keep moving while paused (pitstop displays)

    private float phase;

    private void Start()
    {
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * speed + phase) * angle);
    }
}
