using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItaiPrototype
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Game State")]
        public int currentLevelIndex = 0;
        public GameObject storedDrawing; // The clumped object we carry around

        [System.Serializable]
        public struct LevelData
        {
            public string name;             // Just for your reference (e.g., "Bridge Level")
            [TextArea] public string prompt; // The text shown to player (e.g., "Build a bridge")
            public string actionSceneName;   // The name of the scene file (e.g., "Situation_1")
            public float timeLimit;          // How long they have to solve it
        }
        
        public LevelData[] levels;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // --- STEP 1: PLAYER FINISHES DRAWING ---
        public void SubmitDrawing(GameObject drawing)
        {
            // 1. Save the drawing
            storedDrawing = drawing;
            
            // 2. Make it persistent
            DontDestroyOnLoad(storedDrawing);
            
            // 3. Hide it and pause physics while loading
            storedDrawing.SetActive(false); 

            // 4. Load the Action Scene defined in the current level data
            string sceneToLoad = levels[currentLevelIndex].actionSceneName;
            SceneManager.LoadScene(sceneToLoad);
        }

        // --- STEP 2: PLAYER WINS (Target Reached) ---
        public void LevelComplete()
        {
            Debug.Log("Level Complete!");

            // 1. Cleanup: Destroy the drawing we used
            if (storedDrawing != null) Destroy(storedDrawing);

            // 2. Increment Level
            currentLevelIndex++;

            // 3. Check if game is beaten
            if (currentLevelIndex >= levels.Length)
            {
                Debug.Log("YOU BEAT THE WHOLE GAME!");
                currentLevelIndex = 0; // Loop back to start (or load a 'Credits' scene)
            }

            // 4. Return to Drawing Board
            SceneManager.LoadScene("DrawingScene");
        }

        // --- STEP 3: PLAYER FAILS (Time Up) ---
        public void LevelFailed()
        {
            Debug.Log("Level Failed. Retrying...");

            // 1. Cleanup: Destroy the failed drawing
            if (storedDrawing != null) Destroy(storedDrawing);

            // 2. DO NOT Increment Level (Retry same index)

            // 3. Return to Drawing Board
            SceneManager.LoadScene("DrawingScene");
        }

        // Helper to get current level data
        public LevelData GetCurrentLevelData()
        {
            // Safety check to prevent crash if array is empty
            if (levels.Length == 0 || currentLevelIndex >= levels.Length) 
                return new LevelData { prompt = "No Level Data" };
                
            return levels[currentLevelIndex];
        }
    }
}