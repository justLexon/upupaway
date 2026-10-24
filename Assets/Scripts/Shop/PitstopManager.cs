using System.Collections;
using UnityEngine;

// Every `interval` meters: pause the world, dock the ship next to the PitstopStation, and resume when the player sets sail.
public class PitstopManager : MonoBehaviour
{
    [SerializeField] private ShipController ship;
    [SerializeField] private PitstopStation station;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private GameObject pipFrame; // hidden while docked
    [SerializeField] private float interval = 250f; // meters between pitstops
    [SerializeField] private float dockX = -4f;     // where the ship sits while docked
    [SerializeField] private float dockMoveTime = 0.5f;

    public float NextPitstopAltitude { get; private set; }
    public float DistanceToNext => Mathf.Max(0f, NextPitstopAltitude - ship.Altitude);
    public bool IsDocked { get; private set; }

    private bool departing;

    private void Start()
    {
        NextPitstopAltitude = ship.Altitude + interval;
        station.DepartPressed += Depart;
    }

    private void Update()
    {
        if (IsDocked || GameManager.Instance.IsGameOver)
            return;

        if (ship.Altitude >= NextPitstopAltitude)
            Dock();
    }

    private void Dock()
    {
        IsDocked = true;
        GameManager.Instance.SetPaused(true);

        cameraSwitcher.GoTo(CameraSwitcher.View.Outside);
        cameraSwitcher.InputLocked = true;
        if (pipFrame != null)
            pipFrame.SetActive(false);

        ClearSky();
        StartCoroutine(MoveShip(dockX));
        station.Open();
    }

    private void Depart()
    {
        if (!IsDocked || departing)
            return;

        departing = true;
        station.Close(() =>
        {
            NextPitstopAltitude += interval;
            cameraSwitcher.InputLocked = false;
            if (pipFrame != null)
                pipFrame.SetActive(true);

            GameManager.Instance.SetPaused(false);
            IsDocked = false;
            departing = false;
        });
    }

    // Safe harbor: clear anything flying around (but keep jugs stuck to the ship so they can be unloaded and sold).
    private void ClearSky()
    {
        foreach (Obstacle obstacle in FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
        {
            if (obstacle is MilkJug jug && jug.IsAttached)
                continue;
            Destroy(obstacle.gameObject);
        }
    }

    private IEnumerator MoveShip(float targetX)
    {
        Transform shipTransform = ship.transform;
        float startX = shipTransform.position.x;
        Quaternion startRotation = shipTransform.rotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / dockMoveTime)
        {
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 position = shipTransform.position;
            position.x = Mathf.Lerp(startX, targetX, eased);
            shipTransform.position = position;
            shipTransform.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, eased);
            yield return null;
        }
    }
}
