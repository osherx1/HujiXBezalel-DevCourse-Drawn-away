using UnityEngine;

/// <summary>
/// Minimal stand-in for the Platformer Toolkit lab state tracker.
/// characterHurt consults these flags to avoid respawning while menus are open.
/// </summary>
[DisallowMultipleComponent]
public class labOpener : MonoBehaviour
{
    [Tooltip("True while the lab UI is currently visible.")]
    public bool labIsOpen;

    [Tooltip("True while transitioning into or out of the lab UI.")]
    public bool labIsTransitioning;
}
