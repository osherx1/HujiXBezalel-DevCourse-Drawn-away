using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text;

namespace Core.Editor
{
    public class MissingScriptsFinder : EditorWindow
    {
        /// <summary>
        /// Editor window that finds and removes missing script references in the scene hierarchy
        /// </summary>

        // Window state
        private Vector2 scrollPosition;

        private bool scanComplete = false;
        private List<GameObject> objectsWithMissingScripts = new List<GameObject>();
        private bool showSceneObjectsOnly = true;

        // Add menu item to open this window
        [MenuItem("Tools/Missing Script Finder")]
        public static void ShowWindow()
        {
            // Get existing or create new window
            GetWindow<MissingScriptsFinder>("Missing Script Finder");
        }


        /// <summary>
        /// Draw the editor GUI
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("Find Objects with Missing Script References", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Option to include prefabs in Project or just scan scene objects
            showSceneObjectsOnly = EditorGUILayout.Toggle("Scene Objects Only", showSceneObjectsOnly);

            if (GUILayout.Button("Scan for Missing Scripts"))
            {
                scanComplete = false;
                FindMissingScripts();
                scanComplete = true;
            }

            EditorGUILayout.Space();

            // Display results if scan has been completed
            if (scanComplete)
            {
                DisplayResults();
            }
        }

        /// <summary>
        /// Finds all objects with missing script references
        /// </summary>
        private void FindMissingScripts()
        {
            objectsWithMissingScripts.Clear();

            if (showSceneObjectsOnly)
            {
                // Find all root GameObjects in the current scene
                GameObject[] rootObjects =
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

                foreach (GameObject rootObject in rootObjects)
                {
                    // Check the root object and all its children recursively
                    CheckGameObjectAndChildren(rootObject);
                }
            }
            else
            {
                // Get all GameObjects in the project, including those in scenes and prefabs
                string[] guids = AssetDatabase.FindAssets("t:GameObject");

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null)
                    {
                        CheckGameObjectForMissingScripts(prefab);
                    }
                }

                // Also check scene objects
                GameObject[] rootObjects =
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

                foreach (GameObject rootObject in rootObjects)
                {
                    CheckGameObjectAndChildren(rootObject);
                }
            }

            Debug.Log($"Found {objectsWithMissingScripts.Count} objects with missing script references");
        }

        static void FindMissingScriptsInScene()
        {
            GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int go_count = 0, components_count = 0, missing_count = 0;

            StringBuilder sb = new StringBuilder();

            foreach (GameObject g in gameObjects)
            {
                if (EditorUtility.IsPersistent(g) || g.hideFlags != HideFlags.None)
                    continue;

                go_count++;
                Component[] components = g.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    components_count++;
                    if (components[i] == null)
                    {
                        missing_count++;
                        sb.AppendLine($"{g.name} has a missing script at position: {i}");
                    }
                }
            }

            Debug.Log(sb.ToString());
            Debug.Log(
                $"Searched {go_count} GameObjects, {components_count} components, found {missing_count} missing scripts.");
        }

        /// <summary>
        /// Recursively checks a GameObject and all its children for missing scripts
        /// </summary>
        /// <param name="gameObject">GameObject to check</param>
        private void CheckGameObjectAndChildren(GameObject gameObject)
        {
            // Check the current GameObject
            CheckGameObjectForMissingScripts(gameObject);

            // Check all children
            foreach (Transform child in gameObject.transform)
            {
                CheckGameObjectAndChildren(child.gameObject);
            }
        }

        /// <summary>
        /// Checks if a GameObject has any missing script references
        /// </summary>
        /// <param name="gameObject">GameObject to check</param>
        /*private void CheckGameObjectForMissingScripts(GameObject gameObject)
        {
            // Get all components on the GameObject
            Component[] components = gameObject.GetComponents<Component>();

            // Check if any of the components are null (missing script)
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    // If this is the first missing script on this GameObject, add it to the list
                    if (!objectsWithMissingScripts.Contains(gameObject))
                    {
                        objectsWithMissingScripts.Add(gameObject);
                    }

                    break;
                }
            }
        }*/
        /*
        private void CheckGameObjectForMissingScripts(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];

                // בדיקה רגילה - סקריפט חסר
                if (component == null)
                {
                    if (!objectsWithMissingScripts.Contains(gameObject))
                        objectsWithMissingScripts.Add(gameObject);

                    Debug.LogWarning($"Missing script on GameObject: {gameObject.name}, at index {i}", gameObject);
                    continue;
                }

                // בדיקה בטוחה: ניסיון לגשת ל-SerializedObject
                try
                {
                    var so = new SerializedObject(component);
                    var dummy = so.targetObject; // גישה שיכולה לזרוק חריגה
                }
                catch (System.Exception ex)
                {
                    if (!objectsWithMissingScripts.Contains(gameObject))
                        objectsWithMissingScripts.Add(gameObject);

                    Debug.LogWarning($"Broken component on GameObject: {gameObject.name} (Component: {component.GetType().Name})\n" +
                                     $"Exception: {ex.GetType().Name} - {ex.Message}", gameObject);
                }
            }
        }
        */

        private void CheckGameObjectForMissingScripts(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];

                if (component == null)
                {
                    if (!objectsWithMissingScripts.Contains(gameObject))
                        objectsWithMissingScripts.Add(gameObject);

                    Debug.LogWarning($"Missing script on GameObject: {gameObject.name}, at index {i}", gameObject);
                    continue;
                }

                try
                {
                    UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(component);
                    if (editor != null)
                    {
                        _ = editor.serializedObject;
                    }
                }
                catch (System.Exception ex)
                {
                    if (!objectsWithMissingScripts.Contains(gameObject))
                        objectsWithMissingScripts.Add(gameObject);

                    Debug.LogError($"Editor creation failed for {gameObject.name} (Component: {component.GetType().Name})\n" +
                                   $"Exception: {ex.GetType().Name} - {ex.Message}", gameObject);
                }
            }
        }

        /// <summary>
        /// Displays the scan results and provides options to fix missing scripts
        /// </summary>
        private void DisplayResults()
        {
            EditorGUILayout.LabelField($"Found {objectsWithMissingScripts.Count} objects with missing scripts",
                EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Group actions
            if (objectsWithMissingScripts.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Remove All Missing Scripts"))
                {
                    RemoveAllMissingScripts();
                }

                if (GUILayout.Button("Select All Objects"))
                {
                    Selection.objects = objectsWithMissingScripts.ToArray();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Display the list of GameObjects with missing scripts
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Create a copy of the list to avoid modification during iteration
            List<GameObject> objectsToDisplay = new List<GameObject>(objectsWithMissingScripts);

            foreach (GameObject obj in objectsToDisplay)
            {
                if (obj == null)
                {
                    // Skip null objects (might have been deleted)
                    continue;
                }

                EditorGUILayout.BeginHorizontal();

                // Allow selecting the GameObject
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = obj;
                }

                // Show object name and path
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);

                // Allow removing missing scripts from this specific GameObject
                if (GUILayout.Button("Remove Scripts", GUILayout.Width(120)))
                {
                    RemoveMissingScriptsFromObject(obj);
                    objectsWithMissingScripts.Remove(obj);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Removes all missing script references from all identified GameObjects
        /// </summary>
        private void RemoveAllMissingScripts()
        {
            int totalRemoved = 0;

            // Create a copy of the list to avoid modification during iteration
            List<GameObject> objectsToProcess = new List<GameObject>(objectsWithMissingScripts);

            foreach (GameObject obj in objectsToProcess)
            {
                if (obj != null)
                {
                    int removed = RemoveMissingScriptsFromObject(obj);
                    totalRemoved += removed;
                }
            }

            // Clear the list since we've processed everything
            objectsWithMissingScripts.Clear();

            Debug.Log($"Removed {totalRemoved} missing script references");

            // Re-scan to ensure we got everything
            FindMissingScripts();
        }

        /// <summary>
        /// Removes all missing script references from a specific GameObject
        /// </summary>
        /// <param name="gameObject">GameObject to clean up</param>
        /// <returns>Number of missing scripts removed</returns>
        private int RemoveMissingScriptsFromObject(GameObject gameObject)
        {
            // We need to use SerializedObject to remove missing scripts
            SerializedObject serializedObject = new SerializedObject(gameObject);
            SerializedProperty componentsProperty = serializedObject.FindProperty("m_Component");

            int removedCount = 0;
            int componentCount = componentsProperty.arraySize;

            // Iterate backwards through the components to safely remove elements
            for (int i = componentCount - 1; i >= 0; i--)
            {
                SerializedProperty componentProperty = componentsProperty.GetArrayElementAtIndex(i);
                SerializedProperty componentReference = componentProperty.FindPropertyRelative("component");

                // If the component reference is null (missing script), remove it
                if (componentReference.objectReferenceValue == null)
                {
                    componentsProperty.DeleteArrayElementAtIndex(i);
                    removedCount++;
                }
            }

            // Apply changes if any scripts were removed
            if (removedCount > 0)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(gameObject);
            }

            return removedCount;
        }
    }
}