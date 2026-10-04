using UnityEngine;

/// <summary>
/// Run state that has to survive scene loads. A static class rather than a DontDestroyOnLoad
/// object: there is nothing to render or update, and a static cannot be duplicated by loading the
/// menu scene twice.
///
/// Scene names live here so the menu, the stopgap Esc handler and the editor builders agree on
/// them. Every scene named here must also be in the build list - Tools > DroneMissile > Configure
/// Build Scenes - or SceneManager.LoadScene fails at runtime. See docs/decisions.md #21.
/// </summary>
public static class GameSession
{
    // Hardcore = Normal's rules without upgrades (docs/plans/hardcore-mode.md). Rules that differ
    // only on Easy check for Easy explicitly, so Hardcore inherits Normal everywhere else.
    public enum Difficulty { Easy, Normal, Hardcore }

    public const string MainMenuScene = "MainMenu";

    // The wave run (docs/decisions.md #23). SampleScene stays as the free-flight test map.
    public const string GameScene = "Arena";

    public const string SandboxScene = "Sandbox";

    // Chosen on the New game screen. Read by DroneControls (arena walls) and RunManager (death).
    public static Difficulty CurrentDifficulty = Difficulty.Normal;

    // The card system (docs/plans/upgrades.md) must ask this before offering or granting anything.
    public static bool UpgradesEnabled => CurrentDifficulty != Difficulty.Hardcore;

    // There is no save system yet, so Continue is always greyed out.
    public static bool HasSave => false;

    // ---- run stats, for the between-wave and end screens -------------------------------------

    public static int Wave;
    public static int DamageThisWave;
    public static int DamageTotal;
    public static int TurretsDestroyed;

    public static void ResetRun()
    {
        Wave = 0;
        DamageThisWave = 0;
        DamageTotal = 0;
        TurretsDestroyed = 0;
        ClearUpgrades();
    }

    // ---- upgrades owned in this run (docs/plans/upgrades.md) --------------------------------
    // Survive an Easy wave restart; cleared by ResetRun (new run, back in the main menu).

    static readonly System.Collections.Generic.List<string> upgradeOrder = new System.Collections.Generic.List<string>();
    static readonly System.Collections.Generic.Dictionary<string, int> upgradeStacks = new System.Collections.Generic.Dictionary<string, int>();

    // Distinct upgrade ids in the order they were first picked.
    public static System.Collections.Generic.IReadOnlyList<string> OwnedUpgrades => upgradeOrder;

    public static int Stacks(string id)
    {
        return upgradeStacks.TryGetValue(id, out int n) ? n : 0;
    }

    // `sandbox` skips the Hardcore check: the sandbox console grants on request, and the difficulty
    // left over from the last run means nothing there.
    public static void AddUpgrade(string id, bool sandbox = false)
    {
        if (!UpgradesEnabled && !sandbox) return;   // Hardcore (#25)
        if (!upgradeStacks.ContainsKey(id))
        {
            upgradeStacks[id] = 0;
            upgradeOrder.Add(id);
        }
        upgradeStacks[id]++;
    }

    // One stack. Sandbox console only - nothing in a run takes an upgrade away.
    public static bool RemoveUpgrade(string id)
    {
        if (!upgradeStacks.TryGetValue(id, out int n)) return false;
        if (n > 1)
        {
            upgradeStacks[id] = n - 1;
        }
        else
        {
            upgradeStacks.Remove(id);
            upgradeOrder.Remove(id);
        }
        return true;
    }

    public static void ClearUpgrades()
    {
        upgradeOrder.Clear();
        upgradeStacks.Clear();
    }

    public static void ResetWaveStats()
    {
        DamageThisWave = 0;
    }

    // Called by EnemyTurret with the HP actually removed, so overkill does not inflate the total.
    public static void RecordDamage(int amount)
    {
        DamageThisWave += amount;
        DamageTotal += amount;
    }

    // Best wave survives the run and the session. Kept per difficulty - an Easy record says little
    // about Normal.
    public static int BestWave
    {
        get { return PlayerPrefs.GetInt(BestWaveKey, 0); }
    }

    // Returns true if this run set a new record.
    public static bool SubmitBestWave(int wave)
    {
        if (wave <= BestWave) return false;
        PlayerPrefs.SetInt(BestWaveKey, wave);
        PlayerPrefs.Save();
        return true;
    }

    static string BestWaveKey => "BestWave_" + CurrentDifficulty;
}
