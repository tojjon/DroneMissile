using UnityEngine;

/// <summary>
/// The main menu's backdrop: circles the camera around a pivot and keeps it looking at it. Lives
/// only in MainMenu.unity, placed by Tools > DroneMissile > Build Main Menu Scene.
/// </summary>
public class MenuCameraOrbit : MonoBehaviour
{
    [Tooltip("What to circle. Falls back to orbiting the world origin if left empty.")]
    public Transform pivot;

    public float radius = 9f;

    [Tooltip("Camera height above the pivot.")]
    public float height = 3f;

    [Tooltip("Degrees per second. Negative orbits the other way.")]
    public float speed = 6f;

    [Tooltip("Aim this far above the pivot, so the subject sits a little below the screen centre.")]
    public float lookHeight = 0.5f;

    private float angle;

    void Start()
    {
        // Start from wherever the camera was placed, so the first frame does not jump.
        Vector3 offset = transform.position - PivotPosition();
        angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        // Unscaled, so a future pause (timeScale 0) on top of the menu would not freeze the backdrop.
        angle += speed * Time.unscaledDeltaTime;

        Vector3 centre = PivotPosition();
        Quaternion around = Quaternion.Euler(0f, angle, 0f);
        transform.position = centre + around * new Vector3(0f, height, radius);
        transform.LookAt(centre + Vector3.up * lookHeight);
    }

    Vector3 PivotPosition()
    {
        return pivot != null ? pivot.position : Vector3.zero;
    }
}
