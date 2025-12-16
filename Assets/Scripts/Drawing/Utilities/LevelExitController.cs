using Drawing.Data;
using Drawing.Managers.Core.Managers;
using UnityEngine;
namespace Drawing.Utilities
{
    public class LevelExitController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string nextSceneName;
        [SerializeField] private string playerTag = "Player";
    
        [Header("Audio & Visuals")]
        [Tooltip("The sound type to play via AudioManager when triggering the exit.")]
        [SerializeField] private GameSoundsSo.AudioType successSoundType = GameSoundsSo.AudioType.LevelComplete;
    
        [Tooltip("Optional: Particle system to play on exit.")]
        [SerializeField] private ParticleSystem activationParticles;

        [Header("Dependencies")]
        [SerializeField] private SceneTransitionManager transitionManager;

        private bool _hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;

            if (IsPlayer(other))
            {
                _hasTriggered = true;
            
                // 1. Play Visuals
                PlayActivationParticles();

                // 2. Play Sound via Singleton
                PlaySuccessSound();

                // 3. Start Transition
                TriggerLevelTransition();
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            return other.CompareTag(playerTag);
        }

        private void PlayActivationParticles()
        {
            if (activationParticles != null)
            {
                activationParticles.Play();
            }
        }

        private void PlaySuccessSound()
        {
            if (AudioManager.Instance != null)
            {
                // Using your existing method structure
                AudioManager.Instance.PlaySoundByAudioType(successSoundType);
            }
            else
            {
                Debug.LogWarning("[LevelExitController] AudioManager Instance is null!");
            }
        }

        private void TriggerLevelTransition()
        {
            if (transitionManager != null && !string.IsNullOrEmpty(nextSceneName))
            {
                transitionManager.TransitionToScene(nextSceneName);
            }
            else
            {
                Debug.LogError($"[LevelExitController] Missing TransitionManager or SceneName on: {gameObject.name}");
            }
        }
    }
}