using UnityEngine;

public class PipController : MonoBehaviour
{
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private Camera pipCamera;
    [SerializeField] private RectTransform pipFrame;
    [SerializeField] private float outsideY = 0f;
    [SerializeField] private float cabinY = -50f;

    private void Start()
    {
        if (cameraSwitcher == null)
            cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();

        if (cameraSwitcher == null)
            return;

        if (pipCamera == null)
            pipCamera = GetComponentInChildren<Camera>();

        if (pipCamera != null && pipCamera.transform.parent != null)
            pipCamera.transform.SetParent(null, true);

        cameraSwitcher.OnViewChanged += HandleViewChanged;
        HandleViewChanged(cameraSwitcher.CurrentView);
    }

    private void OnDestroy()
    {
        if (cameraSwitcher != null)
            cameraSwitcher.OnViewChanged -= HandleViewChanged;
    }

    private void HandleViewChanged(CameraSwitcher.View view)
    {
        if (pipCamera == null || pipFrame == null)
            return;

        bool outside = view == CameraSwitcher.View.Outside;
        float otherY = outside ? cabinY : outsideY;
        pipCamera.transform.position = new Vector3(0f, otherY, -10f);

        pipFrame.anchorMin = pipFrame.anchorMax = pipFrame.pivot = new Vector2(0.5f, outside ? 0f : 1f);
        pipFrame.anchoredPosition = new Vector2(0f, outside ? 20f : -20f);
    }
}
