using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every upgrade in the game, found as assets under Assets/Resources/Upgrades/, and the card draw
/// (docs/plans/upgrades.md): roll a rarity by weight, re-roll a rarity that has nothing left to
/// offer, never the same upgrade twice in one offer. With fewer upgrades than slots the offer is
/// simply shorter.
/// </summary>
public static class UpgradeCatalog
{
    static UpgradeDefinition[] all;

    public static IReadOnlyList<UpgradeDefinition> All
    {
        get
        {
            if (all == null)
            {
                all = Resources.LoadAll<UpgradeDefinition>("Upgrades");
                System.Array.Sort(all, (a, b) =>
                    a.rarity != b.rarity ? a.rarity.CompareTo(b.rarity) : string.CompareOrdinal(a.displayName, b.displayName));
            }
            return all;
        }
    }

    public static UpgradeDefinition Find(string id)
    {
        foreach (UpgradeDefinition d in All) if (d.id == id) return d;
        return null;
    }

    public static List<UpgradeDefinition> Draw(int count)
    {
        List<UpgradeDefinition> pool = new List<UpgradeDefinition>(All);
        List<UpgradeDefinition> offer = new List<UpgradeDefinition>();

        while (offer.Count < count && pool.Count > 0)
        {
            Rarity r = RollRarity(pool);
            List<UpgradeDefinition> ofRarity = pool.FindAll(d => d.rarity == r);
            UpgradeDefinition pick = ofRarity[Random.Range(0, ofRarity.Count)];
            offer.Add(pick);
            pool.Remove(pick);   // no duplicates in one offer
        }
        return offer;
    }

    // Rolling only among rarities still present in the pool is the same as re-rolling an empty
    // rarity until something lands - without the loop.
    static Rarity RollRarity(List<UpgradeDefinition> pool)
    {
        float total = 0f;
        bool[] present = new bool[6];
        foreach (UpgradeDefinition d in pool) present[(int)d.rarity] = true;
        for (int i = 0; i < present.Length; i++) if (present[i]) total += Rarities.Weight((Rarity)i);

        float roll = Random.value * total;
        for (int i = 0; i < present.Length; i++)
        {
            if (!present[i]) continue;
            roll -= Rarities.Weight((Rarity)i);
            if (roll <= 0f) return (Rarity)i;
        }
        return pool[0].rarity;
    }
}
