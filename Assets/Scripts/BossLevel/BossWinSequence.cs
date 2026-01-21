using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Drawing.LineControl; 

[RequireComponent(typeof(Collider2D))] // Forces a collider to exist
public class BossWinSequence : MonoBehaviour
{
    [Header("Win Settings")]
    [Tooltip("Tag of the player object.")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Sequence Settings")]
    [Tooltip("The name of the scene to load after the fade.")]
    [SerializeField] private string endSceneName = "EndScene";
    
    [Header("Video (Optional)")]
    [Tooltip("Assign a VideoPlayer here. If null, the video step is skipped.")]
    [SerializeField] private VideoPlayer victoryVideo;
    [Tooltip("Optional: A UI RawImage/Panel for the video. Enabled when video starts.")]
    [SerializeField] private GameObject videoDisplayObject;

    [Header("White Fade")]
    [Tooltip("A full-screen White UI Image. Starts transparent, fades to opaque.")]
    [SerializeField] private Image whiteFadePanel;
    [SerializeField] private float fadeDuration = 2.0f;

    [Header("Player Control")]
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private MonoBehaviour movementScript;
    [SerializeField] private MonoBehaviour jumpScript;
    [SerializeField] private LineManager lineManager;

    private bool _isWinning = false;

    private void Awake()
    {
        // Ensure the collider on this object acts as a trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isWinning) return;

        if (other.CompareTag(playerTag))
        {
            // Auto-grab player references if they are missing
            if (playerRb == null) playerRb = other.GetComponentInParent<Rigidbody2D>();
            if (movementScript == null) movementScript = other.GetComponentInParent<MonoBehaviour>(); // Fallback
            if (lineManager == null) lineManager = FindFirstObjectByType<LineManager>();

            StartCoroutine(WinRoutine());
        }
    }

    private IEnumerator WinRoutine()
    {
        _isWinning = true;
        Debug.Log("BOSS DEFEATED! Starting Win Sequence...");

        // 1. Disable Boss Attacks immediately
        var bossController = FindFirstObjectByType<GiantBossController>();
        if (bossController != null) 
        {
            bossController.DefeatBossInternal();
        }

        // 2. Lock Player Physics/Input
        LockPlayer(true);

        // 3. Play Video (Only if assigned and clip exists)
        if (victoryVideo != null && victoryVideo.clip != null)
        {
            if (videoDisplayObject != null) videoDisplayObject.SetActive(true);
            
            victoryVideo.Play();

            // Wait for video to actually start (buffering frame)
            while (!victoryVideo.isPlaying) yield return null;

            // Wait for the duration of the clip
            float duration = (float)victoryVideo.clip.length;
            yield return new WaitForSecondsRealtime(duration);
        }

        // 4. Fade to White
        yield return StartCoroutine(FadeToWhite());

        // 5. Load End Scene
        SceneManager.LoadScene(endSceneName);
    }

    private IEnumerator FadeToWhite()
    {
        if (whiteFadePanel == null) yield break;

        whiteFadePanel.gameObject.SetActive(true);
        Color c = whiteFadePanel.color;
        c.a = 0f;
        whiteFadePanel.color = c;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            c.a = Mathf.Clamp01(t);
            whiteFadePanel.color = c;
            yield return null;
        }
        // Ensure full opacity at end
        c.a = 1f;
        whiteFadePanel.color = c;
    }

    private void LockPlayer(bool locked)
    {
        if (movementScript) movementScript.enabled = !locked;
        if (jumpScript) jumpScript.enabled = !locked;
        if (lineManager) lineManager.enabled = !locked;

        if (playerRb)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.simulated = !locked; // Freeze physics completely
        }
    }
}