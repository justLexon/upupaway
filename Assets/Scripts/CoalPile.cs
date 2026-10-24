using System.Collections;
using UnityEngine;

// Coal pile next to the furnace: shrinks as coal runs out, and tosses a lump into the furnace on each click.
public class CoalPile : MonoBehaviour
{
    [SerializeField] private Furnace furnace;
    [SerializeField] private Transform throwTarget;  // the furnace door
    [SerializeField] private GameObject[] lumps;     // bottom row first, so the top disappears first
    [SerializeField] private int coalPerLump = 2;
    [SerializeField] private float throwTime = 0.3f;
    [SerializeField] private float throwHeight = 1.2f;
    [SerializeField] private int thrownSortingOrder = 9;

    private void OnEnable()
    {
        furnace.OnCoalAdded += ThrowLump;
    }

    private void OnDisable()
    {
        furnace.OnCoalAdded -= ThrowLump;
    }

    private void Update()
    {
        int coal = ResourceManager.Instance != null ? ResourceManager.Instance.Coal : 0;
        int visible = Mathf.Min(lumps.Length, Mathf.CeilToInt(coal / (float)coalPerLump));
        for (int i = 0; i < lumps.Length; i++)
        {
            if (lumps[i].activeSelf != i < visible)
                lumps[i].SetActive(i < visible);
        }
    }

    private void ThrowLump()
    {
        if (lumps.Length == 0 || throwTarget == null)
            return;

        // Start from the highest lump still on the pile (or the pile itself if it's empty).
        Vector3 start = transform.position;
        for (int i = lumps.Length - 1; i >= 0; i--)
        {
            if (lumps[i].activeSelf)
            {
                start = lumps[i].transform.position;
                break;
            }
        }

        GameObject lump = Instantiate(lumps[0], start, Quaternion.identity, transform.parent);
        lump.SetActive(true);
        lump.transform.localScale = lumps[0].transform.lossyScale;
        lump.GetComponent<SpriteRenderer>().sortingOrder = thrownSortingOrder;
        StartCoroutine(Fly(lump.transform, start));
    }

    private IEnumerator Fly(Transform lump, Vector3 start)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / throwTime)
        {
            Vector3 end = throwTarget.position;
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * throwHeight;
            lump.position = position;
            lump.Rotate(0f, 0f, 720f * Time.deltaTime);
            yield return null;
        }
        Destroy(lump.gameObject);
    }
}
