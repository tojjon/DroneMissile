using UnityEngine;

/// <summary>
/// Marks an arena wall or the ceiling. On Easy, DroneControls stuns instead of crashing when it hits
/// one of these (docs/decisions.md #20); the floor deliberately has none, so it kills on both
/// difficulties. A component rather than a tag: tags are a silent contract in this project.
/// Placed by Tools > DroneMissile > Build Arena.
/// </summary>
public class ArenaWall : MonoBehaviour
{
}
