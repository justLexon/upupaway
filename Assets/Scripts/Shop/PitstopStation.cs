using System;
using System.Collections;
using UnityEngine;

// The floating market that slides in at a pitstop. Builds one stall per ShopItem.
public class PitstopStation : MonoBehaviour
{
    [SerializeField] private ShopItem[] items;
    [SerializeField] private ShopStall stallPrefab;
    [SerializeField] private Transform stallRow;
    [SerializeField] private float rowWidth = 7.2f;
    [SerializeField] private float maxStallSpacing = 3.6f;
    [SerializeField] private ShopButton departButton;
    [SerializeField] private float shownX = 4.6f;
    [SerializeField] private float hiddenX = 16f;
    [SerializeField] private float slideTime = 0.6f;

    public event Action DepartPressed;

    private Coroutine slideRoutine;

    private void Start()
    {
        int count = items.Length;
        float spacing = count > 1 ? Mathf.Min(maxStallSpacing, rowWidth / count) : 0f;
        for (int i = 0; i < count; i++)
        {
            ShopStall stall = Instantiate(stallPrefab, stallRow, false);
            stall.transform.localPosition = new Vector3((i - (count - 1) / 2f) * spacing, 0f, 0f);
            stall.Setup(items[i]);
        }

        departButton.Clicked += () => DepartPressed?.Invoke();
        SetX(hiddenX);
    }

    public void Open(Action onDone = null)
    {
        Slide(shownX, onDone);
    }

    public void Close(Action onDone = null)
    {
        Slide(hiddenX, onDone);
    }

    private void Slide(float targetX, Action onDone)
    {
        if (slideRoutine != null)
            StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideRoutine(targetX, onDone));
    }

    // Unscaled: the world is paused while docked.
    private IEnumerator SlideRoutine(float targetX, Action onDone)
    {
        float startX = transform.position.x;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / slideTime)
        {
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease out
            SetX(Mathf.Lerp(startX, targetX, eased));
            yield return null;
        }
        SetX(targetX);
        slideRoutine = null;
        onDone?.Invoke();
    }

    private void SetX(float x)
    {
        Vector3 position = transform.position;
        position.x = x;
        transform.position = position;
    }
}
