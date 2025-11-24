using System.Collections.Generic;
using CustomInspector;
using UnityEngine;

namespace Drawing.Data
{
    /// <summary>
    /// A helper struct to allow dictionary-like serialization in the Unity Inspector.
    /// Unity cannot serialize Dictionaries directly, so we use a List of this struct.
    /// </summary>
    [System.Serializable]
    public class NamedLineSetting
    {
        [Tooltip("The unique ID/Name for this setting.")]
        public string id;


        [Tooltip("The settings configuration associated with this ID.")]
        public LineSettings settings;
    }

    /// <summary>
    /// A ScriptableObject that holds a collection of LineSettings mapped by string IDs.
    /// </summary>
    [CreateAssetMenu(fileName = "LineSettingsCollection", menuName = "Drawing/Line Settings Collection")]
    public class LineSettingsCollection : ScriptableObject
    {
        [Header("Configuration")]
        [SerializeField] private List<NamedLineSetting> settingsList;

        // Runtime lookup dictionary for performance (O(1) access)
        private Dictionary<string, LineSettings> _lookupTable;

        /// <summary>
        /// Retrieves a LineSettings object by its ID.
        /// Initializes the dictionary on the first call.
        /// </summary>
        /// <param name="id">The unique string ID of the setting.</param>
        /// <returns>The matching LineSettings, or null if not found.</returns>
        public LineSettings GetSettingsByID(string id)
        {
            // Lazy initialization: Build the dictionary only when first needed
            if (_lookupTable == null)
            {
                BuildLookupTable();
            }

            if (_lookupTable.TryGetValue(id, out LineSettings result))
            {
                return result;
            }

            Debug.LogWarning($"LineSettingsCollection: Setting with ID '{id}' was not found.", this);
            return null; 
        }

        /// <summary>
        /// Converts the inspector list into a Dictionary for fast runtime lookup.
        /// </summary>
        private void BuildLookupTable()
        {
            _lookupTable = new Dictionary<string, LineSettings>();

            foreach (var item in settingsList)
            {
                // Prevent duplicates or empty keys
                if (string.IsNullOrEmpty(item.id)) continue;

                if (!_lookupTable.ContainsKey(item.id))
                {
                    _lookupTable.Add(item.id, item.settings);
                }
                else
                {
                    Debug.LogWarning($"LineSettingsCollection: Duplicate ID '{item.id}' detected. Skipping.", this);
                }
            }
        }
        
  
    }
}