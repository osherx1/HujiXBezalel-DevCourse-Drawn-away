namespace Utilities
{
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DDOLPrinter : MonoBehaviour
{
    // Allows you to trigger this function via right-click in the Inspector during Play Mode
    [ContextMenu("Print DDOL Objects")]
    public void PrintAllDontDestroyOnLoadObjects()
    {
        Debug.Log("--- Starting Search for DontDestroyOnLoad Objects ---");

        List<GameObject> ddolObjects = GetDDOLObjects();

        if (ddolObjects.Count == 0)
        {
            Debug.Log("No DontDestroyOnLoad objects found (or scene is inaccessible).");
        }
        else
        {
            foreach (GameObject obj in ddolObjects)
            {
                Debug.Log($"Found DDOL Object: {obj.name}", obj);
            }
        }

        Debug.Log($"--- Finished. Found {ddolObjects.Count} objects. ---");
    }

    private List<GameObject> GetDDOLObjects()
    {
        List<GameObject> result = new List<GameObject>();

        // 1. Create a temporary GameObject
        GameObject tempObject = new GameObject("TempDDOLProbe");

        try
        {
            // 2. Move it to the DontDestroyOnLoad scene
            DontDestroyOnLoad(tempObject);

            // 3. Get the scene reference from the object
            Scene ddolScene = tempObject.scene;

            // 4. Validate scene handle
            if (ddolScene.IsValid())
            {
                // 5. Get all root GameObjects from that specific scene
                GameObject[] roots = ddolScene.GetRootGameObjects();

                foreach (GameObject obj in roots)
                {
                    // Filter out the temp object itself
                    if (obj != tempObject)
                    {
                        result.Add(obj);
                    }
                }
            }
        }
        finally
        {
            // 6. Cleanup: Destroy the temp object immediately
            if (Application.isPlaying)
            {
                Destroy(tempObject);
            }
            else
            {
                DestroyImmediate(tempObject);
            }
        }

        return result;
    }
}
}