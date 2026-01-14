using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Prototype1
{
    /// <summary>
    /// Drives sword UI from SwordProgressManager.
    /// Configure stages in the Inspector (e.g., 0 pieces = locked, 1 piece, 2 pieces, ...).
    /// </summary>
    public class SwordUIController : MonoBehaviour
    {
        [Serializable]
        public class Stage
        {
            [Tooltip("Inclusive minimum collected pieces required to be considered in this stage.")]
            public int minCollectedPieces;

            [Tooltip("If true, this stage requires IsUnlocked==true.")]
            public bool requireUnlocked;

            [Header("Activation")]
            [Tooltip("Optional: if set, this GameObject will be enabled for this stage and all other stageRoots will be disabled.")]
            public GameObject stageRoot;

            [Tooltip("Optional additional objects to enable when entering this stage.")]
            public GameObject[] enableObjects;

            [Tooltip("Optional objects to disable when entering this stage.")]
            public GameObject[] disableObjects;

            [Header("Optional Sprite Swap")]
            public Image targetImage;
            public Sprite sprite;

            [Header("Events")]
            public UnityEvent onEnterStage;
        }

        [Header("Progress Source")]
        [Tooltip("Optional: assign a specific SwordProgressManager. If empty, uses SwordProgressManager.Instance.")]
        [SerializeField] private SwordProgressManager progress;

        [Header("UI Root")]
        [Tooltip("Optional: entire UI root to enable when unlocked (or always).")]
        [SerializeField] private GameObject uiRoot;

        [Tooltip("If true, uiRoot is enabled only when progress.IsUnlocked==true.")]
        [SerializeField] private bool showUiOnlyWhenUnlocked = true;

        [Header("Stages")]
        [Tooltip("Stages are evaluated by highest minCollectedPieces first.")]
        [SerializeField] private List<Stage> stages = new List<Stage>();

        private int _currentStageIndex = -1;

        private void Reset()
        {
            if (uiRoot == null)
            {
                uiRoot = gameObject;
            }
        }

        private void Awake()
        {
            if (uiRoot == null)
            {
                uiRoot = gameObject;
            }
        }

        private void OnEnable()
        {
            ResolveProgress();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveProgress()
        {
            if (progress == null)
            {
                progress = SwordProgressManager.Instance;
            }
        }

        private void Subscribe()
        {
            if (progress == null)
            {
                return;
            }

            progress.ProgressChanged += OnProgressChanged;
        }

        private void Unsubscribe()
        {
            if (progress == null)
            {
                return;
            }

            progress.ProgressChanged -= OnProgressChanged;
        }

        private void OnProgressChanged(object sender, SwordProgressManager.ProgressChangedEventArgs e)
        {
            Refresh();
        }

        public void Refresh()
        {
            ResolveProgress();
            if (progress == null)
            {
                return;
            }

            bool unlocked = progress.IsUnlocked;
            int collected = progress.CollectedCount;

            if (uiRoot != null)
            {
                uiRoot.SetActive(!showUiOnlyWhenUnlocked || unlocked);
            }

            int bestStage = FindBestStageIndex(unlocked, collected);
            if (bestStage == _currentStageIndex)
            {
                return;
            }

            _currentStageIndex = bestStage;
            ApplyStage(bestStage);
        }

        private int FindBestStageIndex(bool unlocked, int collected)
        {
            if (stages == null || stages.Count == 0)
            {
                return -1;
            }

            int bestIndex = -1;
            int bestMin = int.MinValue;

            for (int i = 0; i < stages.Count; i++)
            {
                Stage s = stages[i];
                if (s == null)
                {
                    continue;
                }

                if (s.requireUnlocked && !unlocked)
                {
                    continue;
                }

                if (collected < s.minCollectedPieces)
                {
                    continue;
                }

                if (s.minCollectedPieces > bestMin)
                {
                    bestMin = s.minCollectedPieces;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void ApplyStage(int stageIndex)
        {
            if (stageIndex < 0 || stages == null || stageIndex >= stages.Count)
            {
                return;
            }

            // Ensure only one stageRoot is active at a time.
            if (stages != null)
            {
                foreach (Stage st in stages)
                {
                    if (st?.stageRoot != null)
                    {
                        st.stageRoot.SetActive(false);
                    }
                }
            }

            Stage s = stages[stageIndex];
            if (s == null)
            {
                return;
            }

            if (s.stageRoot != null)
            {
                s.stageRoot.SetActive(true);
            }

            if (s.disableObjects != null)
            {
                foreach (GameObject go in s.disableObjects)
                {
                    if (go != null) go.SetActive(false);
                }
            }

            if (s.enableObjects != null)
            {
                foreach (GameObject go in s.enableObjects)
                {
                    if (go != null) go.SetActive(true);
                }
            }

            if (s.targetImage != null && s.sprite != null)
            {
                s.targetImage.sprite = s.sprite;
                s.targetImage.enabled = true;
            }

            s.onEnterStage?.Invoke();
        }

        public void ShowUiNow()
        {
            if (uiRoot != null)
            {
                uiRoot.SetActive(true);
            }
        }

        public void HideUiNow()
        {
            if (uiRoot != null)
            {
                uiRoot.SetActive(false);
            }
        }
    }
}
