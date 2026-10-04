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
    public enum Difficulty { Easy, Normal }

    public const string MainMenuScene = "MainMenu";

    // The wave run (docs/decisions.md #23). SampleScene stays as the free-flight test map.
    public const string GameScene = "Arena";

    public const string SandboxScene = "Sandbox";

    // Chosen on the New game screen. Read by DroneControls (arena walls) and RunManager (death).
    public static Difficulty CurrentDifficulty = Difficulty.Normal;

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
