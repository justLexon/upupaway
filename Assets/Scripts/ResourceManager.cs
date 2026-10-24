using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private int startingCoal = 20;

    public int Coal { get; private set; }

    private void Awake()
    {
        Instance = this;
        Coal = startingCoal;
    }

    public bool TrySpendCoal(int amount = 1)
    {
        if (Coal < amount) return false;
        Coal -= amount;
        return true;
    }

    public void AddCoal(int amount)
    {
        Coal += amount;
    }
}
