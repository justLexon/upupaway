using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private int startingCoal = 20;
    [SerializeField] private int startingCoins;

    public int Coal { get; private set; }
    public int Coins { get; private set; }

    private void Awake()
    {
        Instance = this;
        Coal = startingCoal;
        Coins = startingCoins;
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

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || Coins < amount)
            return false;

        Coins -= amount;
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount > 0)
            Coins += amount;
    }
}
