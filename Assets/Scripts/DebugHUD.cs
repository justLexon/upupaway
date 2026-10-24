using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private ShipController ship;
    [SerializeField] private Furnace furnace;
    [SerializeField] private CloudSea cloudSea;
    [SerializeField] private bool visibleOnStart = false;

    private void Awake()
    {
        text.gameObject.SetActive(visibleOnStart);
    }

    private void Update()
    {
        // F1 toggles the readout.
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            text.gameObject.SetActive(!text.gameObject.activeSelf);

        if (!text.gameObject.activeSelf)
            return;

        text.text =
            $"Altitude: {ship.Altitude:0} m (best {ship.MaxAltitude:0})\n" +
            $"Speed: {ship.VerticalSpeed:+0.0;-0.0} m/s\n" +
            $"Weight: {ship.Weight:0.0} m/s\n" +
            $"Coal: {ResourceManager.Instance.Coal}\n" +
            $"Heat: {furnace.Heat:0} / {furnace.MaxHeat:0}\n" +
            $"Clouds: {ship.Altitude - cloudSea.Altitude:0} m below";
    }
}
