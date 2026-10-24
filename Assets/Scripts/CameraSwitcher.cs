using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    public enum View
    {
        Outside,
        Cabin
    }

    [SerializeField] private float outsideY = 0f;
    [SerializeField] private float cabinY = -50f;
    [SerializeField] private float smoothTime = 0.15f;

    public View CurrentView { get; private set; } = View.Outside;
    public event Action<View> OnViewChanged;

    private View targetView = View.Outside;
    private float velocity;

    private void Start()
    {
        UpdateCameraPosition();
    }

    private void Update()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0.1f)
                targetView = View.Outside;
            else if (scroll < -0.1f)
                targetView = View.Cabin;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                targetView = View.Outside;
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                targetView = View.Cabin;
        }

        UpdateCameraPosition();
    }

    public void ToggleView()
    {
        targetView = targetView == View.Outside ? View.Cabin : View.Outside;
    }

    private void UpdateCameraPosition()
    {
        float targetY = targetView == View.Outside ? outsideY : cabinY;
        Vector3 position = transform.position;
        position.y = Mathf.SmoothDamp(position.y, targetY, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        transform.position = position;

        View visible = position.y > ((outsideY + cabinY) * 0.5f) ? View.Outside : View.Cabin;
        if (visible != CurrentView)
        {
            CurrentView = visible;
            OnViewChanged?.Invoke(visible);
        }
    }
}
