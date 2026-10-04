using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets up the upgrade cards (docs/plans/upgrades.md): imports the card art in Assets/UI/Upgrades
/// as crisp pixel-art sprites, and creates one UpgradeDefinition asset per upgrade under
/// Assets/Resources/Upgrades/, where UpgradeCatalog finds them at runtime.
///
/// Existing definition assets are LEFT ALONE, so edits made in the Inspector survive a rerun.
/// Adding a new upgrade later: drop its card PNG in Assets/UI/Upgrades, rerun this (or create the
/// asset by hand via Create > DroneMissile > Upgrade), and implement its effect by id.
///
/// Headless: Unity -batchmode -quit -executeMethod UpgradesBuilder.BuildAll
/// </summary>
public static class UpgradesBuilder
{
    const string ArtDir = "Assets/UI/Upgrades/";
    const string AssetDir = "Assets/Resources/Upgrades/";

    struct Spec
    {
        public string id, name, description;
        public Rarity rarity;
    }

    // From Viktor's Upgrades notes and card art, 04.10.2026. The card file is ArtDir + id + ".png".
    static readonly Spec[] Specs =
    {
        new Spec { id = UpgradeIds.DoubleStrike, name = "Double Strike", rarity = Rarity.Common,
                   description = "Fires another shot right after the first one. +1 shot per card." },
        new Spec { id = UpgradeIds.Lightning, name = "Lightning", rarity = Rarity.Rare,
                   description = "Hits chain to the nearest turret within 40 m for half damage. +1 jump per card." },
        new Spec { id = UpgradeIds.Homing, name = "Homing", rarity = Rarity.Epic,
                   description = "Rockets home onto turrets in the square scope. Bigger scope and sharper turns per card." },
    };

    [MenuItem("Tools/DroneMissile/Build Upgrades")]
    public static void BuildAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir.TrimEnd('/') }))
        {
            ImportAsPixelSprite(AssetDatabase.GUIDToAssetPath(guid));
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(AssetDir.TrimEnd('/'))) AssetDatabase.CreateFolder("Assets/Resources", "Upgrades");

        foreach (Spec s in Specs)
        {
            string path = AssetDir + s.id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path) != null) continue;

            UpgradeDefinition def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.id = s.id;
            def.displayName = s.name;
            def.description = s.description;
            def.rarity = s.rarity;
            def.card = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + s.id + ".png");
            if (def.card == null) Debug.LogWarning("UpgradesBuilder: no card art at " + ArtDir + s.id + ".png");

            AssetDatabase.CreateAsset(def, path);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("UpgradesBuilder: " + Specs.Length + " upgrades ready in " + AssetDir);
    }

    // Pixel art: point filtering and no compression, or 71 x 100 cards blur and smear at 3x.
    static void ImportAsPixelSprite(string path)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
    }
}
