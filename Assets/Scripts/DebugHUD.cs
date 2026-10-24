using TMPro;
using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private ShipController ship;
    [SerializeField] private Furnace furnace;
    [SerializeField] private CloudSea cloudSea;

    private void Update()
    {
        text.text =
            $"Altitude: {ship.Altitude:0} m (best {ship.MaxAltitude:0})\n" +
            $"Speed: {ship.VerticalSpeed:+0.0;-0.0} m/s\n" +
            $"Coal: {ResourceManager.Instance.Coal}\n" +
            $"Heat: {furnace.Heat:0} / {furnace.MaxHeat:0}\n" +
            $"Clouds: {ship.Altitude - cloudSea.Altitude:0} m below";
    }
}
