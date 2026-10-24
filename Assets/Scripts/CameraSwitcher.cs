using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraSwitcher : MonoBehaviour
{
    public enum View
    {
        Outside,
        Cabin
    }

    [SerializeField] private float outsideY = 0f;
    [SerializeField] private float cabinY = -50f;
    [SerializeField] private float fadeOutDuration = 0.1f;
    [SerializeField] private float fadeInDuration = 0.16f;
    [SerializeField] private RawImage fadeOverlay;

    public View CurrentView { get; private set; } = View.Outside;
    public bool IsTransitioning { get; private set; }
    public bool InputLocked { get; set; } // e.g. stay outside while docked at a pitstop
    public event Action<View> OnViewChanged;

    private View targetView = View.Outside;
    private float fadeAlpha;
    private bool fadingOut;

    private void Awake()
    {
        EnsureFadeOverlay();
    }

    private void Update()
    {
        ReadInput();
        if (fadeOverlay == null)
            return;

        if (!IsTransitioning)
        {
            if (targetView == CurrentView)
                return;

            IsTransitioning = true;
            fadingOut = true;
        }
        else if (!fadingOut && targetView != CurrentView)
        {
            fadingOut = true;
        }

        if (fadingOut)
        {
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeOutDuration));
            SetFadeAlpha(fadeAlpha);
            if (fadeAlpha >= 1f)
            {
                if (targetView != CurrentView)
                    ChangeViewWhileCovered(targetView);

                fadingOut = false;
            }
        }
        else
        {
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeInDuration));
            SetFadeAlpha(fadeAlpha);
            if (fadeAlpha <= 0f)
                IsTransitioning = false;
        }
    }

    private void ReadInput()
    {
        if (InputLocked)
            return;

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
    }

    public void ToggleView()
    {
        targetView = targetView == View.Outside ? View.Cabin : View.Outside;
    }

    public void GoTo(View view)
    {
        targetView = view;
    }

    private void ChangeViewWhileCovered(View view)
    {
        float targetY = view == View.Outside ? outsideY : cabinY;
        Vector3 position = transform.position;
        position.y = targetY;
        transform.position = position;

        CurrentView = view;
        OnViewChanged?.Invoke(view);
    }

    private void EnsureFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            SetFadeAlpha(0f);
            fadeOverlay.transform.SetAsLastSibling();
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("CameraSwitcher requires a Canvas to display its transition fade.", this);
            return;
        }

        var overlayObject = new GameObject("TransitionFade", typeof(RectTransform), typeof(RawImage));
        overlayObject.transform.SetParent(canvas.transform, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeOverlay = overlayObject.GetComponent<RawImage>();
        fadeOverlay.texture = Texture2D.whiteTexture;
        fadeOverlay.raycastTarget = false;
        SetFadeAlpha(0f);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null)
            return;

        fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
    }
}
