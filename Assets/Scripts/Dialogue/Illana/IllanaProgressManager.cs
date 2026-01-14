using System;
using UnityEngine;

/// <summary>
/// Persistent progress for Illana's mini-quest (meet -> objective -> escort end).
/// Optional PlayerPrefs persistence so it survives scene changes and play sessions.
/// </summary>
public class IllanaProgressManager : MonoBehaviour
{
    [Serializable]
    public class StateChangedEventArgs : EventArgs
    {
        public bool HasMet;
        public bool ObjectiveCompleted;
        public bool EscortCompleted;
    }

    private static IllanaProgressManager _instance;

    public static IllanaProgressManager Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindObjectOfType<IllanaProgressManager>(true);
            if (_instance != null)
            {
                return _instance;
            }

            GameObject go = new GameObject(nameof(IllanaProgressManager));
            _instance = go.AddComponent<IllanaProgressManager>();
            return _instance;
        }
    }

    [Serializable]
    private class SaveData
    {
        public bool hasMet;
        public bool objectiveCompleted;
        public bool escortCompleted;
    }

    [Header("Persistence")]
    [SerializeField] private bool usePlayerPrefs = true;
    [SerializeField] private string playerPrefsKey = "Dialogue.IllanaProgress";

    [Header("State (Read Only)")]
    [SerializeField] private bool hasMet;
    [SerializeField] private bool objectiveCompleted;
    [SerializeField] private bool escortCompleted;

    public event EventHandler<StateChangedEventArgs> StateChanged;

    public bool HasMet => hasMet;
    public bool ObjectiveCompleted => objectiveCompleted;
    public bool EscortCompleted => escortCompleted;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        RaiseChanged();
    }

    public void MarkMet()
    {
        if (hasMet)
        {
            return;
        }

        hasMet = true;
        Save();
        RaiseChanged();
    }

    public void MarkObjectiveCompleted()
    {
        if (objectiveCompleted)
        {
            return;
        }

        objectiveCompleted = true;
        Save();
        RaiseChanged();
    }

    public void MarkEscortCompleted()
    {
        if (escortCompleted)
        {
            return;
        }

        escortCompleted = true;
        Save();
        RaiseChanged();
    }

    public void ResetAll()
    {
        hasMet = false;
        objectiveCompleted = false;
        escortCompleted = false;

        Save();
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        StateChanged?.Invoke(this, new StateChangedEventArgs
        {
            HasMet = hasMet,
            ObjectiveCompleted = objectiveCompleted,
            EscortCompleted = escortCompleted,
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
            hasMet = hasMet,
            objectiveCompleted = objectiveCompleted,
            escortCompleted = escortCompleted,
        };

        PlayerPrefs.SetString(playerPrefsKey, JsonUtility.ToJson(data));
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

            hasMet = data.hasMet;
            objectiveCompleted = data.objectiveCompleted;
            escortCompleted = data.escortCompleted;
        }
        catch
        {
            // Ignore corrupted save.
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Illana/Reset Progress")]
    private void EditorReset()
    {
        ResetAll();
    }
#endif
}
