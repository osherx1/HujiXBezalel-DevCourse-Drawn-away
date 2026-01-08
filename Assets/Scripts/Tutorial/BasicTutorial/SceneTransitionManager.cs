using UnityEngine;
using DG.Tweening; // Required for DOTween
using System.Collections;

public class SceneTransitionManager : MonoSingleton<SceneTransitionManager>
{
    [Header("UI Settings")]
    [SerializeField] private CanvasGroup fadePanel; // Assign the Panel with CanvasGroup here
    [SerializeField] private float fadeDuration = 1.0f;

    private void Awake()
    {
        // Ensure screen starts clear
        fadePanel.alpha = 0; 
        fadePanel.blocksRaycasts = false;
    }

    public void TeleportPlayer(Transform playerTransform, Transform targetDestination)
    {
        StartCoroutine(TeleportRoutine(playerTransform, targetDestination));
    }

    private IEnumerator TeleportRoutine(Transform player, Transform destination)
    {
        // 1. Fade OUT (to black)
        fadePanel.blocksRaycasts = true; // Block input so player can't move during fade
        yield return fadePanel.DOFade(1f, fadeDuration).WaitForCompletion();

        // 2. Move the Player
        // We temporarily disable physics/colliders to prevent glitches while moving
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false; 
        
        player.position = destination.position;

        yield return new WaitForSeconds(0.5f); // Wait a moment in the dark

        if (rb != null) rb.simulated = true;

        // 3. Fade IN (to clear)
        yield return fadePanel.DOFade(0f, fadeDuration).WaitForCompletion();
        fadePanel.blocksRaycasts = false;
    }
}