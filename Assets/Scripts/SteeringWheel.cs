using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringWheel : MonoBehaviour, IClickable
{
    [SerializeField] private float sensitivity = 0.5f; // degrees per pixel of mouse movement
    [SerializeField] private float maxAngle = 90f;
    [SerializeField] private float returnSpeed = 60f;  // degrees per second back to center when released

    // -1 = full left, +1 = full right
    public float SteerInput => -angle / maxAngle;

    private float angle;
    private bool held;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        // Mouse delta is already per frame, so it isn't scaled by deltaTime.
        if (held && Mouse.current != null)
            angle -= Mouse.current.delta.ReadValue().x * sensitivity;
        else
            angle = Mathf.MoveTowards(angle, 0f, returnSpeed * Time.deltaTime);

        angle = Mathf.Clamp(angle, -maxAngle, maxAngle);
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void OnPress()
    {
        held = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void OnRelease()
    {
        held = false;
        Cursor.lockState = CursorLockMode.None;
    }
}
