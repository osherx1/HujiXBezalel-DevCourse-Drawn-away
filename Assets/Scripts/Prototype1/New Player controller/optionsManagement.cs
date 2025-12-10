using UnityEngine;

/// <summary>
/// Lightweight placeholder for runtime options that other Toolkit-era scripts expect.
/// Extend this later if additional toggles are required by the project UI.
/// </summary>
[DisallowMultipleComponent]
public class optionsManagement : MonoBehaviour
{
    [Tooltip("Should camera shake and other on-hurt feedback play?")]
    public bool screenShake = true;

    public void SetScreenShake(bool enabled)
    {
        screenShake = enabled;
    }
}
