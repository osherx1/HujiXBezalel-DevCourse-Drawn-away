// Or 'using Cinemachine;' depending on version
using UnityEngine;
using Unity.Cinemachine;
using Drawing.Managers;
using UnityEngine.Serialization; // Required to access your EventManager


namespace Utilities.Camera.CameraShake
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeManager : MonoBehaviour
    {
      [SerializeField] private CinemachineImpulseSource impulseSource;
        

        private void Awake()
        {
            // Cache the component to avoid GetComponent calls during runtime
            if (impulseSource == null)
            {
                impulseSource = GetComponent<CinemachineImpulseSource>();
            }
        }

        private void OnEnable()
        {
            // Subscribe to your Singleton
            EventManager.Instance.OnCameraShakeRequested += ExecuteShake;
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            EventManager.Instance.OnCameraShakeRequested -= ExecuteShake;
        }

        private void ExecuteShake(ShakeProfile profile)
        {
            if (profile == null)
            {
                Debug.Log("Shake Profile is null");
                return;
            }

            SetupImpulseDefinition(profile);
        
            // Fire the impulse with default velocity (Vector3.one is standard for non-directional shakes)
            impulseSource.GenerateImpulse(Vector3.one);
        }

        private void SetupImpulseDefinition(ShakeProfile profile)
        {
            // Apply settings from the ScriptableObject to the Cinemachine component
            impulseSource.ImpulseDefinition.RawSignal = profile.ImpulseSignal;
            impulseSource.ImpulseDefinition.AmplitudeGain = profile.AmplitudeGain;
            impulseSource.ImpulseDefinition.FrequencyGain = profile.FrequencyGain;

            impulseSource.ImpulseDefinition.TimeEnvelope.AttackTime = profile.AttackTime;
            impulseSource.ImpulseDefinition.TimeEnvelope.SustainTime = profile.SustainTime;
            impulseSource.ImpulseDefinition.TimeEnvelope.DecayTime = profile.DecayTime;
        }
 

        
    }
}