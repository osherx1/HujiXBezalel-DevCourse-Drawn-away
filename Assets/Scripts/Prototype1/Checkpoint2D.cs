using UnityEngine;

namespace Prototype1
{
    /// <summary>
    /// Place this on a checkpoint object (with a Collider2D marked as Trigger).
    /// When the player enters, it updates the player's respawn checkpoint.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint2D : MonoBehaviour
    {
        [Header("Filter")]
        [SerializeField] private string requiredTag = "Player";

        [Header("Checkpoint Position")]
        [Tooltip("If set, this transform's position will be saved as the checkpoint. If empty, uses this object's transform.")]
        [SerializeField] private Transform respawnPoint;

        [Header("One-shot")]
        [Tooltip("If true, this checkpoint will only trigger once.")]
        [SerializeField] private bool triggerOnce = true;

        [Header("Activation Animation")]
        [Tooltip("If set, the Animator will be used for the checkpoint activation animation.")]
        [SerializeField] private Animator checkpointAnimator;

        [Tooltip("If set, the legacy Animation component will be used for the checkpoint activation animation.")]
        [SerializeField] private Animation checkpointAnimation;

        [Tooltip("If true, the animation is paused on start and will only play after the player activates the checkpoint.")]
        [SerializeField] private bool playAnimationOnlyAfterActivation = true;

        [Tooltip("Optional Animator trigger parameter to fire on activation (e.g. 'Activate'). Leave empty to just unpause the Animator.")]
        [SerializeField] private string animatorActivateTrigger = "Activate";

        [Tooltip("If false and triggerOnce is false, the animation will only play the first time the checkpoint is activated.")]
        [SerializeField] private bool replayAnimationOnReactivation = true;

        private bool _used;
        private bool _animationPlayed;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (checkpointAnimator == null)
            {
                checkpointAnimator = GetComponent<Animator>();
            }

            if (checkpointAnimation == null)
            {
                checkpointAnimation = GetComponent<Animation>();
            }
        }

        private void Awake()
        {
            if (!playAnimationOnlyAfterActivation)
            {
                return;
            }

            // Prefer Animator if both are present.
            // We disable the animation driver so the object's original SpriteRenderer state remains visible.
            if (checkpointAnimator != null)
            {
                checkpointAnimator.enabled = false;
            }
            else if (checkpointAnimation != null)
            {
                checkpointAnimation.playAutomatically = false;
                checkpointAnimation.Stop();
                checkpointAnimation.enabled = false;
            }
        }

        private void PlayActivationAnimationIfNeeded()
        {
            if (!playAnimationOnlyAfterActivation)
            {
                return;
            }

            if (_animationPlayed && !replayAnimationOnReactivation)
            {
                return;
            }

            if (checkpointAnimator != null)
            {
                if (!checkpointAnimator.enabled)
                {
                    checkpointAnimator.enabled = true;
                }

                // Optionally fire a trigger to start a specific transition.
                if (!string.IsNullOrWhiteSpace(animatorActivateTrigger))
                {
                    checkpointAnimator.ResetTrigger(animatorActivateTrigger);
                    checkpointAnimator.SetTrigger(animatorActivateTrigger);
                }
            }
            else if (checkpointAnimation != null)
            {
                if (!checkpointAnimation.enabled)
                {
                    checkpointAnimation.enabled = true;
                }
                checkpointAnimation.Play();
            }

            _animationPlayed = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used && triggerOnce)
            {
                return;
            }

            if (other == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            var hurt = other.GetComponent<characterHurt>() ?? other.GetComponentInParent<characterHurt>();
            if (hurt == null)
            {
                return;
            }

            Vector3 checkpointPosition = (respawnPoint != null ? respawnPoint.position : transform.position);
            hurt.newCheckpoint(checkpointPosition);

            PlayActivationAnimationIfNeeded();

            _used = true;
        }
    }
}
