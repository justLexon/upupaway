using UnityEngine;

// Spawns by distance climbed (not time), so hovering doesn't flood the screen.
public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public AltitudeObject prefab;
        public float weight = 1f;
    }

    [SerializeField] private ShipController ship;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private SpawnEntry[] entries;
    [SerializeField] private float spawnAhead = 60f; // meters above the ship (just off the top of the screen)
    [SerializeField] private float minSpacing = 20f; // meters between spawns
    [SerializeField] private float maxSpacing = 40f;
    [SerializeField] private float xLimit = 7f;

    [Header("Pity rule (optional): force a coal balloon when the player is low")]
    [SerializeField] private AltitudeObject pityPrefab;
    [SerializeField] private int pityCoalThreshold = 3;
    [SerializeField] private int pityAfterSpawns = 3; // spawns without one before it's forced

    private float nextSpawnAltitude;
    private int spawnsSincePity;

    private void Start()
    {
        nextSpawnAltitude = ship.Altitude + spawnAhead;
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
        float total = 0f;
        foreach (SpawnEntry entry in entries)
            total += entry.weight;

        float roll = Random.value * total;
        foreach (SpawnEntry entry in entries)
        {
            roll -= entry.weight;
            if (roll <= 0f)
                return entry.prefab;
        }
        return entries.Length > 0 ? entries[entries.Length - 1].prefab : null;
    }
}
