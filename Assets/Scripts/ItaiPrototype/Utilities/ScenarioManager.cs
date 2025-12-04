using TMPro;
using UnityEngine;

namespace ItaiPrototype.Utilities
{
    public class ScenarioManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Transform spawnPoint; // Where the drawing appears
        [SerializeField] private TextMeshProUGUI timerText;       // UI for timer
        [SerializeField] private float drawingScale = 0.5f; // Default to half size
    
        private float _timeLeft;
        private bool _levelActive = true;

        private void Start()
        {
            if (GameManager.Instance == null) return;

            // 1. Setup Timer based on Level Data
            GameManager.LevelData data = GameManager.Instance.GetCurrentLevelData();
            _timeLeft = data.timeLimit;

            // 2. Spawn the Drawing
            GameObject drawing = GameManager.Instance.storedDrawing;
            if (drawing == null) return;
            // Move it to the spawn point
            drawing.transform.position = spawnPoint.position;
            // Apply the custom scale immediately
            drawing.transform.localScale = Vector3.one * drawingScale;
            
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
            GameManager.Instance.LevelComplete();
        }

        public void Lose()
        {
            if (!_levelActive) return;
            _levelActive = false;
            GameManager.Instance.LevelFailed();
        }
    }
}