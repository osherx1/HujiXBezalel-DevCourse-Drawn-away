using UnityEngine;
using UnityEngine.UI;

namespace ItaiPrototype
{
    public class ScenarioManager : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint; // Where should the drawing appear?
        [SerializeField] private Text timerText;
    
        private float timeLeft;
        private bool levelActive = true;

        private void Start()
        {
            // 1. Get Level Data
            int levelIdx = GameManager.instance.currentLevelIndex;
            timeLeft = GameManager.instance.levels[levelIdx].timeLimit;

            // 2. Retrieve the Drawing
            GameObject drawing = GameManager.instance.storedDrawing;

            if (drawing == null) return;
            // Position it
            drawing.transform.position = spawnPoint.position;
            drawing.transform.rotation = Quaternion.identity;
            
            // Re-enable it
            drawing.SetActive(true);
            
            // Optional: You might want to nudge it or set velocity to 0
        }

        private void Update()
        {
            if (!levelActive) return;

            // Timer Logic
            timeLeft -= Time.deltaTime;
            if (timerText != null) timerText.text = Mathf.Ceil(timeLeft).ToString();

            if (timeLeft <= 0)
            {
                LoseLevel();
            }
        }

        // Call this when the player hits the target
        public void WinLevel()
        {
            if (!levelActive) return;
            levelActive = false;
            Debug.Log("Success!");
            GameManager.instance.LevelComplete();
        }

        public void LoseLevel()
        {
            if (!levelActive) return;
            levelActive = false;
            Debug.Log("Time's up!");
            GameManager.instance.LevelFailed();
        }
    }
}