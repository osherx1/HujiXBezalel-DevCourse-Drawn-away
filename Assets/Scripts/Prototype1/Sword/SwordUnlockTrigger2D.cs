using UnityEngine;
using Drawing.Data;
using Drawing.Managers.Core.Managers;

namespace Prototype1
{
    /// <summary>
    /// Trigger/NPC helper: unlocks the ability to collect sword pieces the first time the player interacts.
    /// Persists via SwordProgressManager (DontDestroyOnLoad + optional PlayerPrefs).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SwordUnlockTrigger2D : MonoBehaviour
    {
        [Header("Trigger")]
        [SerializeField] private string requiredTag = "Player";

        [Header("Progress")]
        [Tooltip("Optional: assign explicitly. If empty, uses SwordProgressManager.Instance.")]
        [SerializeField] private SwordProgressManager progress;

        [Header("UI")]
        [Tooltip("Optional: a SwordUIController to force-show on first unlock.")]
        [SerializeField] private SwordUIController uiController;

        [Tooltip("Optional: direct UI root to enable on first unlock (if you don't want to reference the controller).")]
        [SerializeField] private GameObject uiRootToEnable;

        [Tooltip("If true, shows/enables the UI only the first time the sword is unlocked.")]
        [SerializeField] private bool showUiOnlyFirstTime = true;

        [Header("One-shot")]
        [SerializeField] private bool triggerOnce = true;

        private bool _used;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (progress == null)
            {
                progress = SwordProgressManager.Instance;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used && triggerOnce)
            {
                return;
            }

            if (other == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            if (progress == null)
            {
                progress = SwordProgressManager.Instance;
            }

            if (progress == null)
            {
                return;
            }

            bool alreadyShown = progress.UiUnlockedShownOnce;

            // Unlock collection ability.
            progress.UnlockSwordCollection(markUiUnlockedShownOnce: false);

            bool shouldShow = !showUiOnlyFirstTime || (!alreadyShown);
            if (shouldShow)
            {
                var audio = AudioManager.Instance;
                if (audio != null)
                {
                    audio.PlaySoundByAudioType(GameSoundsSo.AudioType.SwordUI);
                }

                if (uiController != null)
                {
                    uiController.ShowUiNow();
                    uiController.Refresh();
                }

                if (uiRootToEnable != null)
                {
                    uiRootToEnable.SetActive(true);
                }

                progress.MarkUiUnlockedShownOnce();
            }

            _used = true;
        }
    }
}
