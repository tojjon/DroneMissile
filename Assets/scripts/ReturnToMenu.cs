using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// TEMPORARY: Esc goes straight back to the main menu. It exists only so the menu can be tested in
/// a loop until the real ESC pause menu (docs/design/game-structure.md) replaces it - delete this
/// component and its GameObject from SampleScene and Sandbox when that lands.
/// </summary>
public class ReturnToMenu : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(GameSession.MainMenuScene);
        }
    }
}
