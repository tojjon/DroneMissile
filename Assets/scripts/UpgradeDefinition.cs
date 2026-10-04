using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

/// <summary>
/// One upgrade card (docs/plans/upgrades.md). Adding an upgrade = adding one of these assets under
/// Assets/Resources/Upgrades/ - the card offer, the main-menu catalog and the end screen find it
/// through UpgradeCatalog on their own. What the upgrade DOES lives in code keyed by `id`
/// (Shoting, RocketProjectile); the asset is its identity and its look.
/// </summary>
[CreateAssetMenu(menuName = "DroneMissile/Upgrade", fileName = "NewUpgrade")]
public class UpgradeDefinition : ScriptableObject
{
    [Tooltip("Stable key used in code and typed in the sandbox console. Lowercase, no spaces.")]
    public string id;

    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("Card art, 71 x 100 pixel art (Assets/UI/Upgrades). Rarity is the colour in its corners.")]
    public Sprite card;

    public Rarity rarity;
}

/// <summary>
/// Rarity weights and colours. Weights are the spec's (game-structure.md); colours are taken from the
/// corners of Viktor's cards where a card of that rarity exists, placeholders otherwise.
/// </summary>
public static class Rarities
{
    // Percent. Must stay in the same order as the enum.
    static readonly float[] Weights = { 60f, 20f, 10f, 6f, 3f, 1f };

    static readonly Color[] Colours =
    {
        new Color32(163, 163, 163, 255),   // Common - from Double Strike's corners
        new Color32(40, 200, 60, 255),     // Uncommon - placeholder green (no card yet)
        new Color32(0, 30, 255, 255),      // Rare - from Lightning's corners
        new Color32(157, 0, 255, 255),     // Epic - from Homing's corners
        new Color32(255, 150, 0, 255),     // Legendary - placeholder orange (no card yet)
        new Color32(230, 20, 20, 255),     // Mythic - placeholder red (no card yet)
    };

    public static float Weight(Rarity r) => Weights[(int)r];
    public static Color Colour(Rarity r) => Colours[(int)r];
}

/// <summary>Code-side ids of the upgrades whose effects are implemented.</summary>
public static class UpgradeIds
{
    public const string DoubleStrike = "double_strike";
    public const string Homing = "homing";
    public const string Lightning = "lightning";
}
