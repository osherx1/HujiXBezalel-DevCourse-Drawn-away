using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototype1
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) progress for collecting sword pieces across scenes.
    /// Stores whether the player is allowed to collect sword pieces and which piece IDs were collected.
    /// </summary>
    public class SwordProgressManager : MonoBehaviour
    {
        [Serializable]
        public class ProgressChangedEventArgs : EventArgs
        {
            public bool IsUnlocked;
            public int TotalPieces;
            public int CollectedCount;
        }

        [Serializable]
        private class SaveData
        {
            public bool unlocked;
            public bool uiUnlockedShownOnce;
            public int totalPieces;
            public List<int> collectedPieceIds = new List<int>();
        }

        private static SwordProgressManager _instance;

        public static SwordProgressManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindObjectOfType<SwordProgressManager>(true);
                if (_instance != null)
                {
                    return _instance;
                }

                GameObject go = new GameObject(nameof(SwordProgressManager));
                _instance = go.AddComponent<SwordProgressManager>();
                return _instance;
            }
        }

        [Header("Config")]
        [SerializeField, Min(1)] private int totalPieces = 3;

        [Header("Persistence")]
        [SerializeField] private bool usePlayerPrefs = true;
        [SerializeField] private string playerPrefsKey = "Prototype1.SwordProgress";

        [Header("State (Read Only)")]
        [SerializeField] private bool unlocked;
        [SerializeField] private bool uiUnlockedShownOnce;
        [SerializeField] private List<int> collectedPieceIds = new List<int>();

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public bool IsUnlocked => unlocked;
        public int TotalPieces => Mathf.Max(1, totalPieces);
        public int CollectedCount => collectedPieceIds != null ? collectedPieceIds.Count : 0;
        public bool UiUnlockedShownOnce => uiUnlockedShownOnce;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (collectedPieceIds == null)
            {
                collectedPieceIds = new List<int>();
            }

            Load();
            RaiseChanged();
        }

        public bool HasPiece(int pieceId)
        {
            return collectedPieceIds != null && collectedPieceIds.Contains(pieceId);
        }

        public bool CanCollectPieces()
        {
            return unlocked;
        }

        public void UnlockSwordCollection(bool markUiUnlockedShownOnce = false)
        {
            bool changed = false;
            if (!unlocked)
            {
                unlocked = true;
                changed = true;
            }

            if (markUiUnlockedShownOnce && !uiUnlockedShownOnce)
            {
                uiUnlockedShownOnce = true;
                changed = true;
            }

            if (changed)
            {
                Save();
                RaiseChanged();
            }
        }

        public void MarkUiUnlockedShownOnce()
        {
            if (uiUnlockedShownOnce)
            {
                return;
            }

            uiUnlockedShownOnce = true;
            Save();
            RaiseChanged();
        }

        public bool TryCollectPiece(int pieceId)
        {
            if (!unlocked)
            {
                return false;
            }

            if (collectedPieceIds == null)
            {
                collectedPieceIds = new List<int>();
            }

            if (collectedPieceIds.Contains(pieceId))
            {
                return false;
            }

            collectedPieceIds.Add(pieceId);
            Save();
            RaiseChanged();
            return true;
        }

        public void ResetProgress(bool alsoLock = true)
        {
            if (collectedPieceIds == null)
            {
                collectedPieceIds = new List<int>();
            }

            collectedPieceIds.Clear();
            uiUnlockedShownOnce = false;
            if (alsoLock)
            {
                unlocked = false;
            }

            Save();
            RaiseChanged();
        }

        public void SetTotalPieces(int newTotalPieces)
        {
            totalPieces = Mathf.Max(1, newTotalPieces);
            Save();
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs
            {
                IsUnlocked = unlocked,
                TotalPieces = TotalPieces,
                CollectedCount = CollectedCount,
            });
        }

        private void Save()
        {
            if (!usePlayerPrefs)
            {
                return;
            }

            SaveData data = new SaveData
            {
                unlocked = unlocked,
                uiUnlockedShownOnce = uiUnlockedShownOnce,
                totalPieces = totalPieces,
                collectedPieceIds = collectedPieceIds ?? new List<int>(),
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(playerPrefsKey, json);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (!usePlayerPrefs)
            {
                return;
            }

            if (!PlayerPrefs.HasKey(playerPrefsKey))
            {
                return;
            }

            string json = PlayerPrefs.GetString(playerPrefsKey, "");
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    return;
                }

                unlocked = data.unlocked;
                uiUnlockedShownOnce = data.uiUnlockedShownOnce;
                totalPieces = Mathf.Max(1, data.totalPieces);
                collectedPieceIds = data.collectedPieceIds ?? new List<int>();
            }
            catch
            {
                // If data is corrupted, ignore and keep defaults.
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Sword Progress/Reset (Lock)")]
        private void EditorResetLock()
        {
            ResetProgress(true);
        }

        [ContextMenu("Sword Progress/Reset (Keep Unlocked)")]
        private void EditorResetKeepUnlocked()
        {
            ResetProgress(false);
        }
#endif
    }
}
