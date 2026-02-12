using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Controls a video cutscene (e.g. splash or intro), then fades to black and loads the next scene.
/// Uses VideoPlayer.loopPointReached for completion detection and a CanvasGroup for fade overlay.
/// </summary>
public class VideoSceneLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Overlay used for fade. Alpha 1 = black, Alpha 0 = transparent (video visible).")]
    [SerializeField] private CanvasGroup fadeOverlay;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float delayAfterVideo = 0.5f;

    [Header("Fade In")]
    [Tooltip("If true, fade from black into the video at start. If false, video is shown immediately.")]
    [SerializeField] private bool fadeInAtStart = true;

    [Header("Next Scene")]
    [Tooltip("Load by scene name. Ignored if Use Build Index is true.")]
    [SerializeField] private string nextSceneName = "";
    [Tooltip("If true, load by build index instead of scene name.")]
    [SerializeField] private bool useBuildIndex;
    [Tooltip("Scene index in Build Settings. Used only when Use Build Index is true.")]
    [SerializeField] private int nextSceneBuildIndex;

    private Coroutine _transitionCoroutine;

    private void Start()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
            fadeOverlay.interactable = true;
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("VideoSceneLoader: VideoPlayer is not assigned. Starting transition after delay.");
            _transitionCoroutine = StartCoroutine(TransitionSequence());
            return;
        }

        if (fadeInAtStart && fadeOverlay != null)
            StartCoroutine(FadeOverlay(1f, 0f, fadeDuration));
        else if (fadeOverlay != null)
            fadeOverlay.alpha = 0f;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    /// <summary>
    /// Called when the video reaches the end (loopPointReached).
    /// </summary>
    private void OnVideoFinished(VideoPlayer source)
    {
        if (_transitionCoroutine != null)
            return;

        _transitionCoroutine = StartCoroutine(TransitionSequence());
    }

    /// <summary>
    /// Sequence: delay -> fade out to black -> load next scene asynchronously.
    /// </summary>
    private IEnumerator TransitionSequence()
    {
        yield return new WaitForSecondsRealtime(delayAfterVideo);

        if (fadeOverlay != null)
            yield return FadeOverlay(0f, 1f, fadeDuration);

        if (useBuildIndex)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneBuildIndex);
            while (op != null && !op.isDone)
                yield return null;
        }
        else
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("VideoSceneLoader: nextSceneName is empty and Use Build Index is false.");
                yield break;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
            while (op != null && !op.isDone)
                yield return null;
        }

        _transitionCoroutine = null;
    }

    /// <summary>
    /// Lerps the fade overlay alpha from startAlpha to endAlpha over the given duration.
    /// </summary>
    private IEnumerator FadeOverlay(float startAlpha, float endAlpha, float duration)
    {
        if (fadeOverlay == null || duration <= 0f)
        {
            if (fadeOverlay != null)
                fadeOverlay.alpha = endAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        fadeOverlay.alpha = endAlpha;
    }

    /// <summary>
    /// Editor-only: run the transition sequence manually (delay -> fade out -> load scene).
    /// Video state is ignored; use for testing the transition in Edit mode if supported.
    /// </summary>
    [ContextMenu("Test Transition (Delay -> Fade Out -> Load Scene)")]
    private void EditorTestTransition()
    {
        if (Application.isPlaying)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionSequence());
        }
        else
        {
            Debug.LogWarning("VideoSceneLoader: Test Transition runs only in Play Mode.");
        }
    }
}
