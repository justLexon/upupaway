using UnityEngine;

public class ResourceBalloon : Obstacle
{
    [SerializeField] private int coalAmount = 5;

    protected override void OnHitShip(ShipController ship)
    {
        Collect();
    }

    // Also the entry point for the harpoon later.
    public void Collect()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddCoal(coalAmount);
        Destroy(gameObject);
    }
}
