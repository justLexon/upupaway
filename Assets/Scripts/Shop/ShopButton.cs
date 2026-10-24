using System;
using System.Collections;
using TMPro;
using UnityEngine;

// A clickable wooden sign in the world. Works while the game is paused.
public class ShopButton : MonoBehaviour, IClickable
{
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color enabledColor = new Color(0.45f, 0.75f, 0.35f);
    [SerializeField] private Color disabledColor = new Color(0.45f, 0.42f, 0.4f);

    public event Action Clicked;
    public bool WorksWhilePaused => true;

    private bool interactable = true;
    private Vector3 baseScale;
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        baseScale = transform.localScale;
        ApplyColor();
    }

    public void SetLabel(string text)
    {
        if (label != null)
            label.text = text;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        ApplyColor();
    }

    public void OnPress()
    {
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        if (!interactable)
        {
            feedbackRoutine = StartCoroutine(Shake());
            return;
        }

        feedbackRoutine = StartCoroutine(Punch());
        Clicked?.Invoke();
    }

    public void OnRelease()
    {
    }

    private void ApplyColor()
    {
        if (background != null)
            background.color = interactable ? enabledColor : disabledColor;
    }

    // Unscaled: the shop runs while the world is paused.
    private IEnumerator Punch()
    {
        transform.localScale = baseScale * 0.9f;
        yield return new WaitForSecondsRealtime(0.08f);
        transform.localScale = baseScale;
        feedbackRoutine = null;
    }

    private IEnumerator Shake()
    {
        Vector3 start = transform.localPosition;
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            transform.localPosition = start + Vector3.right * Mathf.Sin(t * 70f) * 0.06f;
            yield return null;
        }
        transform.localPosition = start;
        feedbackRoutine = null;
    }
}
