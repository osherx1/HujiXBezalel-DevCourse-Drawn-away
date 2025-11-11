using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
namespace TraceableShape
{
    public class TraceableShape : MonoBehaviour
    {
        [Header("Component References")] [SerializeField]
        private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D physicsBody;
        [SerializeField] private ParticleSystem revealEffect;
        
        [Header("Reveal Delay & Effects")]
        [Tooltip("Time in seconds to wait before revealing after threshold is met.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float revealDelay = 0.5f;
        
        // --- DOTween Animation Settings ---
        [Header("DOTween Animation Settings")]
        [Range(0.1f, 5f)]
        [SerializeField] private float animationDuration = 0.5f;

        [SerializeField] private Ease easeType;
    
        [SerializeField] private bool animateScale = true;
        [SerializeField] private Vector3 startScale = Vector3.zero;

        [SerializeField] private bool animateFadeIn = true;
        


        [Tooltip("AudioSource to play the reveal sound from.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip revealSound;
        
        [Header("Reveal Settings")] [SerializeField]
        private Sprite revealedSprite;
        [SerializeField] private GameObject revealedObject; 
        private SpriteRenderer _revealedSpriteRenderer;
        private Vector3 _revealedOriginalScale;
        private Color _revealedOriginalColor;
        private Rigidbody2D _revealedPhysics;
        private Vector3 _revealedOriginalPosition;
        
        [Tooltip("The percentage of points (0.0 to 1.0) that must be traced to reveal the shape.")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float completionThreshold = 0.9f;
        private List<TracePoint> _tracePoints = new List<TracePoint>();
        private int _tracedPointsCount;
        private int _totalPoints;
        private bool _isRevealed;
        
        //For debugging
        [SerializeField] private bool showTracePointsGizmos;
        private Vector3 _originalScale;
        private Color _originalColor;
        private Sequence _revealSequenceTween;


        private void Start()
        {
            InitializeTracePoints();
            InitializeRevealedObject();
        }

        private void InitializeRevealedObject()
        {
            if (revealedObject != null)
            {
                _revealedSpriteRenderer = revealedObject.GetComponent<SpriteRenderer>();
                _revealedOriginalScale = revealedObject.transform.localScale; 
                _revealedPhysics = revealedObject.GetComponent<Rigidbody2D>();

                if (_revealedSpriteRenderer != null)
                {
                    _revealedOriginalColor = _revealedSpriteRenderer.color; 
                }
                else
                {
                    Debug.LogWarning("Revealed object is missing a SpriteRenderer. Fade animation will not work.", this);
                }

                if (_revealedPhysics != null)
                {
                    _revealedPhysics.simulated = false;
                }
                else
                {
                    Debug.LogWarning("Revealed object is missing a Rigidbody2D component.", this);
                }
                _revealedOriginalPosition = revealedObject.transform.position;
                
                revealedObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("No 'revealedObject' has been assigned in the Inspector.", this);
            }
        }

        private void InitializeTracePoints()
        {
            GetComponentsInChildren(true, _tracePoints);
            _totalPoints = _tracePoints.Count;

            if (_totalPoints == 0)
            {
                Debug.LogWarning("No TracePoints found in children. TraceableShape cannot be completed.", this);
                return;
            }

            foreach (var point in _tracePoints)
            {
                point.OnTraced += OnPointTraced;
               // Debug.Log($"TracePoint subscribed: {point.gameObject.name}", this);
                
                point.SetShowGizmos(showTracePointsGizmos);
            }
            // Ensure physics are disabled initially
            /*if (physicsBody != null)
            {
                physicsBody.simulated = false;
            }*/
        }
        private IEnumerator RevealSequence()
        {
            // 1. Wait for the specified delay time
            yield return new WaitForSeconds(revealDelay);
                            
        
            // 2. Execute the reveal logic
            PerformReveal();
        }
        private void PerformReveal()
        {
            revealedObject.SetActive(true);
            // Set initial state for the animation
            if (animateFadeIn && _revealedSpriteRenderer != null)
            {
                Color startColor = _revealedOriginalColor;
                startColor.a = 0f;
                _revealedSpriteRenderer.color = startColor;
            }
            
            if (animateScale)
            {
                revealedObject.transform.localScale = startScale;
            }


            // 4. Create an playthe DOTween sequence
            _revealSequenceTween?.Kill();
            _revealSequenceTween = DOTween.Sequence();

            if (animateScale)
            {
                // Insert scale animation at the beginning (time 0)
                _revealSequenceTween.Insert(0, revealedObject.transform.DOScale(_revealedOriginalScale, animationDuration).SetEase(easeType));
            }

            if (animateFadeIn && spriteRenderer != null)
            {
                // Insert fade-in animation at the beginning (time 0)
                _revealSequenceTween.Insert(0, _revealedSpriteRenderer.DOFade(_revealedOriginalColor.a, animationDuration).SetEase(easeType));
            }
            

            _revealSequenceTween.OnComplete(() =>
            {       
                if (_revealedPhysics != null)
                {
                    //TODO change to reaveled object's physics body if needed
                    
                    // 5. Activate physics
                    _revealedPhysics.simulated = true;
                }
                CameraShaker.Instance.Shake(0.3f, 0.2f);
            });
            _revealSequenceTween.Play();
        }


        private void OnDisable()
        {
            UnsubscribePointEvent();
            _revealSequenceTween?.Kill();
        }
        private void OnDestroy()
        {
            UnsubscribePointEvent();
            _revealSequenceTween?.Kill();
        }
        private void UnsubscribePointEvent()
        {
            // Unsubscribe to prevent memory leaks
            foreach (var point in _tracePoints)
            {
                if (point != null)
                {
                    point.OnTraced -= OnPointTraced;
                }
            }
        }
        private void OnPointTraced()
        {
            if (_isRevealed) return;
            _tracedPointsCount++;
            float currentProgress = (float)_tracedPointsCount / _totalPoints;

            if (currentProgress >= completionThreshold)
            {
                _isRevealed = true;
                foreach (var point in _tracePoints)
                {
                    if (point != null)
                    {
                        point.OnTraced -= OnPointTraced;
                    }
                }
                if (audioSource != null && revealSound != null)
                {
                    audioSource.PlayOneShot(revealSound);
                }
                // 4. Play particle effect
                if (revealEffect != null) 
                { 
                    revealEffect.Play();
                }       
                StartCoroutine(RevealSequence());
                
            }
        }
        //TODO fix the reset function 
        [ContextMenu("ResetTraceableShape")]
        public void ResetShape()
        {
            StopAllCoroutines();
            _revealSequenceTween?.Kill();

            _isRevealed = false;
            _tracedPointsCount = 0;

            if (revealedObject != null)
            {
                revealedObject.SetActive(false);
                
                if (_revealedPhysics != null)
                {
                    _revealedPhysics.simulated = false;
                    _revealedPhysics.velocity = Vector2.zero;
                    _revealedPhysics.angularVelocity = 0f;
                }
                revealedObject.transform.position = _revealedOriginalPosition;

                if (_revealedSpriteRenderer != null)
                {
                    _revealedSpriteRenderer.color = _revealedOriginalColor;
                }

                revealedObject.transform.localScale = _revealedOriginalScale;
            }

            UnsubscribePointEvent();

            foreach (var point in _tracePoints)
            {
                if (point != null)
                {
                    point.ResetPoint();
                    point.OnTraced += OnPointTraced;
                    point.SetShowGizmos(showTracePointsGizmos);
                }
            }
        }
    }
    
}

//TODO: Add animator support later
        
//[Tooltip("Animator to trigger when revealing.")]
//[SerializeField] private Animator animator;
//[SerializeField] private string revealTriggerName = "Reveal";
/*// 2. Trigger animation
if (animator != null && !string.IsNullOrEmpty(revealTriggerName))
{
animator.SetTrigger(revealTriggerName);
}*/