using UnityEngine;

// Swings back and forth on Z (bird wings).
public class Flapper : MonoBehaviour
{
    [SerializeField] private float angle = 35f; // degrees each way
    [SerializeField] private float speed = 10f;

    private float phase;

    private void Start()
    {
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * speed + phase) * angle);
    }
}
