using ItaiPrototype.Utilities;
using UnityEngine;

namespace ItaiPrototype.Scenario1
{
    public class ElderRacer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float timeToFinish = 15f; 
        [SerializeField] private string finishLineTag = "Finish"; // Tag your finish line object!

        private float _speed;
        private bool _isStopped = false;

        // We calculate speed based on where the finish line IS right now
        // This assumes the finish line is to the RIGHT of the elder
        private Transform _finishLineTransform;

        private void Start()
        {
            // Find the finish line automatically by Tag (easier setup)
            GameObject finishObj = GameObject.FindGameObjectWithTag(finishLineTag);
        
            if (finishObj != null)
            {
                _finishLineTransform = finishObj.transform;
            
                // Distance = End - Start
                float distance = _finishLineTransform.position.x - transform.position.x;
            
                // Speed = Distance / Time
                if (timeToFinish > 0)
                {
                    _speed = distance / timeToFinish;
                }
            }
            else
            {
                Debug.LogError("ElderRacer could not find an object tagged 'Finish'!");
            }
        }

        private void Update()
        {
            if (_isStopped || !_finishLineTransform) return;

            // Move in a straight line to the right
            // We use transform.Translate, which ignores physics forces completely
            transform.Translate(Vector3.right * (_speed * Time.deltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Did we hit the finish line?
            if (!other.CompareTag(finishLineTag)) return;
            _isStopped = true;
            Debug.Log("Elder won! Player loses.");

            // Find the ScenarioManager in the scene to trigger the loss
            ScenarioManager manager = FindObjectOfType<ScenarioManager>();
            if (manager != null)
            {
                manager.Lose(); 
            }
        }
    }
}