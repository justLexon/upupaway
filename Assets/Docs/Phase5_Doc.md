# Phase 5: Milestone Shop

**Goal:** Add a reusable shop that opens at the start of each run and periodically as the ship climbs. Players can sell attached milk or excess coal for coins, then spend coins on coal.

## Behavior

- The first shop opens automatically when the run begins and pauses gameplay until the player continues.
- After that, a shop opens every 500 meters climbed. Milestones use the ship's best altitude so sinking and climbing back does not re-trigger a visit.
- The left altitude meter shows a gold shop marker moving up the meter as the next visit approaches, plus the distance remaining. The existing cloud-danger meter remains unchanged.
- Buying is optional; the player can continue without spending coins.
- Resource balloons continue to grant coal only; they do not award coins.
- A run starts with 0 coins. Selling each attached milk jug earns 3 coins and removes its weight.
- Excess coal can be sold in bundles of 5 for 1 coin while keeping a 10-coal reserve.
- The default purchase costs 3 coins for 5 coal.

## Extending the shop

- `ShopItem` is an abstract, serializable offer. Add subclasses to grant other resources or upgrades.
- The offers and shop interval are serialized on `ShopManager`.
- Use **Tools > Up Up Away > Install Shop** on an existing Phase 4 scene to add a persistent, configurable `ShopManager`. Scenes with an altitude meter also create one at runtime if it is missing.

## Test

1. Start a run. The shop opens immediately, shows the coin balance, and pauses the game.
2. Sell attached milk; coins increase and the jugs disappear, reducing ship weight.
3. Sell excess coal; coins increase and at least 10 coal remains.
4. Buy coal with enough coins; coal rises and coins decrease. Unaffordable actions are disabled.
5. Continue without trading. The altitude meter shows the shop marker and distance.
6. Climb to the configured milestone. The shop opens once; sinking and climbing back does not reopen it.
7. Collect a resource balloon and confirm it adds coal but no coins.
8. Restart after game over. The initial shop appears again with the starting coin balance.
