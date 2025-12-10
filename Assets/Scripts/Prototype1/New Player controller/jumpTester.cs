using UnityEngine;

/// <summary>
/// Minimal data holder used by characterJuice to track jump/fall state for camera logic.
/// </summary>
[DisallowMultipleComponent]
public class jumpTester : MonoBehaviour
{
    [Header("State shared with characterJuice")]
    public float characterY;

    [Header("Optional audio toggles")]
    public AudioSource jumpSFX;
    [Header("Optional audio toggles")]
    public AudioSource landSFX;

    public void toggleJumpSFX(bool turnOn)
    {
        if (jumpSFX != null)
        {
            jumpSFX.enabled = turnOn;
        }
    }

    public void toggleLandSFX(bool turnOn)
    {
        if (landSFX != null)
        {
            landSFX.enabled = turnOn;
        }
    }
}
