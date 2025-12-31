using System;
using Drawing.LineControl;
using UnityEngine;

// Required to access the Line script

namespace PhysicsObjects
{

    public class Cloud : MonoBehaviour
    {
        /*
            private bool _isInitialized = false;
            private bool _isPlayerOnCloud = false;

            [Tooltip("Assign the Line component here, or leave empty if it's on the same GameObject")]
            [SerializeField] private Line targetLine;
            private Collision2D _playerCollision2D;


            /*private void Awake()
            {
                // Auto-assign the Line component if not set
                if (targetLine == null)
                {
                    targetLine = GetComponent<Line>();
                }

                if (targetLine == null)
                {
                    Debug.LogError($"[{nameof(Cloud)}] Missing Line component. Please assign it in the inspector or add it to the same GameObject.");
                    enabled = false;
                    return;
                }

                _isInitialized = true;
            }#1#


            // --- Player Carrying Logic ---

            void OnCollisionEnter2D(Collision2D collision)
            {
                // Parent the player to the cloud so they move together
                if (collision.gameObject.CompareTag("Player")&& _isInitialized && !_isPlayerOnCloud)
                {
                    _isPlayerOnCloud = true;
                    _playerCollision2D = collision;
                    collision.transform.SetParent(this.transform);
                }
            }

            void OnCollisionExit2D(Collision2D collision)
            {
                // Unparent the player when they leave the cloud
                if (collision.gameObject.CompareTag("Player")&& _isInitialized && _isPlayerOnCloud)
                {
                    _isPlayerOnCloud = false;
                    _playerCollision2D = null;
                    collision.transform.SetParent(null);
                }
            }

            private void OnEnable()
            {
                targetLine.OnLineFinalized += HandleLineFinalized;
            }
            private void OnDisable()
            {
                targetLine.OnLineFinalized -= HandleLineFinalized;
                _isPlayerOnCloud = false;
                if(_playerCollision2D != null)
                    _playerCollision2D.transform.SetParent(null);
                _playerCollision2D = null;

            }


            private void HandleLineFinalized()
            {
                _isInitialized = true;

            }
        */
    }
}