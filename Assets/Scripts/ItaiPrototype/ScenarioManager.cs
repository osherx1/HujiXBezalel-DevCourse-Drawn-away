using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItaiPrototype
{
    public class ScenarioManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Transform spawnPoint; // Where the drawing appears
        [SerializeField] private TextMeshProUGUI timerText;       // UI for timer
    
        private float _timeLeft;
        private bool _levelActive = true;

        private void Start()
        {
            if (GameManager.instance == null) return;

            // 1. Setup Timer based on Level Data
            GameManager.LevelData data = GameManager.instance.GetCurrentLevelData();
            _timeLeft = data.timeLimit;

            // 2. Spawn the Drawing
            GameObject drawing = GameManager.instance.storedDrawing;
            if (drawing == null) return;
            // Move it to the spawn point
            drawing.transform.position = spawnPoint.position;
            
            // Re-enable it (this turns physics back on)
            drawing.SetActive(true);
        }

        private void Update()
        {
            if (!_levelActive) return;

            // Count down
            _timeLeft -= Time.deltaTime;
        
            // Update UI (Optional)
            if (timerText != null) 
                timerText.text = Mathf.Ceil(_timeLeft).ToString();

            // Check for Loss
            if (_timeLeft <= 0)
            {
                Lose();
            }
        }

        // Call this specifically when the WIN condition is met
        public void Win()
        {
            if (!_levelActive) return;
            _levelActive = false;
            GameManager.instance.LevelComplete();
        }

        private void Lose()
        {
            if (!_levelActive) return;
            _levelActive = false;
            GameManager.instance.LevelFailed();
        }
    }
}