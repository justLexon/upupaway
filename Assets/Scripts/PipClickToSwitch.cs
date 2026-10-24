using UnityEngine;
using UnityEngine.EventSystems;

public class PipClickToSwitch : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CameraSwitcher cameraSwitcher;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cameraSwitcher != null)
            cameraSwitcher.ToggleView();
    }
}
