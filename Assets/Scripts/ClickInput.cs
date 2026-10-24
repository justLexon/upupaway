using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ClickInput : MonoBehaviour
{
    private CameraSwitcher cameraSwitcher;
    private IClickable held;

    private void Awake()
    {
        Camera mainCamera = Camera.main;
        cameraSwitcher = mainCamera != null
            ? mainCamera.GetComponent<CameraSwitcher>()
            : FindAnyObjectByType<CameraSwitcher>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            (cameraSwitcher == null || !cameraSwitcher.IsTransitioning))
            TryPress();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            Release();
    }

    private void TryPress()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null)
            return;

        Vector2 pointer = Mouse.current.position.ReadValue();
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(pointer);
        worldPoint.z = 0f;

        // Check every collider under the mouse, so e.g. the ship's collider can't hide a milk jug.
        foreach (Collider2D hit in Physics2D.OverlapPointAll(worldPoint))
        {
            var clickable = hit.GetComponentInParent<IClickable>();
            if (clickable == null)
                continue;

            held = clickable;
            held.OnPress();
            return;
        }
    }

    private void Release()
    {
        if (held == null)
            return;

        held.OnRelease();
        held = null;
    }
}
