using UnityEngine;

// Spawns by distance climbed (not time), so hovering doesn't flood the screen.
public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public AltitudeObject prefab;
        public float weight = 1f;
        public bool dayOnly; // e.g. birds
    }

    [SerializeField] private ShipController ship;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private SpawnEntry[] entries;
    [SerializeField] private float spawnAhead = 60f; // meters above the ship (just off the top of the screen)
    [SerializeField] private float minSpacing = 20f; // meters between spawns
    [SerializeField] private float maxSpacing = 40f;
    [SerializeField] private float xLimit = 7f;
    [SerializeField] private float prefillBelow = 0f; // > 0: at start, also fill the sky from this many meters below the ship (decor clouds)

    [Header("Pity rule (optional): force a coal balloon when the player is low")]
    [SerializeField] private AltitudeObject pityPrefab;
    [SerializeField] private int pityCoalThreshold = 3;
    [SerializeField] private int pityAfterSpawns = 3; // spawns without one before it's forced

    private float nextSpawnAltitude;
    private int spawnsSincePity;

    private void Start()
    {
        nextSpawnAltitude = ship.Altitude + spawnAhead;

        // Start with the screen already populated instead of an empty sky.
        if (prefillBelow > 0f)
        {
            for (float altitude = ship.Altitude - prefillBelow; altitude < nextSpawnAltitude; altitude += Random.Range(minSpacing, maxSpacing))
                Spawn(altitude);
        }
    }

    private void Update()
    {
        while (ship.MaxAltitude + spawnAhead >= nextSpawnAltitude)
        {
            Spawn(nextSpawnAltitude);
            nextSpawnAltitude += Random.Range(minSpacing, maxSpacing);
        }
    }

    private void Spawn(float altitude)
    {
        AltitudeObject prefab = ShouldForcePity() ? pityPrefab : PickPrefab();
        if (prefab == null)
            return;

        spawnsSincePity = prefab == pityPrefab ? 0 : spawnsSincePity + 1;

        var position = new Vector3(Random.Range(-xLimit, xLimit), 0f, 0f);
        AltitudeObject instance = Instantiate(prefab, position, prefab.transform.rotation, spawnParent);
        instance.Init(ship, altitude);
    }

    private bool ShouldForcePity()
    {
        return pityPrefab != null
            && ResourceManager.Instance != null
            && ResourceManager.Instance.Coal <= pityCoalThreshold
            && spawnsSincePity >= pityAfterSpawns;
    }

    private AltitudeObject PickPrefab()
    {
        bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;

        float total = 0f;
        foreach (SpawnEntry entry in entries)
        {
            if (!(entry.dayOnly && night))
                total += entry.weight;
        }
        if (total <= 0f)
            return null;

        float roll = Random.value * total;
        AltitudeObject last = null;
        foreach (SpawnEntry entry in entries)
        {
            if (entry.dayOnly && night)
                continue;
            last = entry.prefab;
            roll -= entry.weight;
            if (roll <= 0f)
                return entry.prefab;
        }
        return last;
    }
}
