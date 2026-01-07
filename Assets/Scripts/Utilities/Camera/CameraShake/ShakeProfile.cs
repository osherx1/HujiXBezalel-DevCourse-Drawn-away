using Unity.Cinemachine;
using UnityEngine;

namespace Utilities.Camera.CameraShake
{
    [CreateAssetMenu(fileName = "NewShakeProfile", menuName = "GameFeel/Shake Profile")]
    public class ShakeProfile : ScriptableObject
    {
        [Header("Impulse Settings")] [SerializeField]
        SignalSourceAsset impulseSignal;

        [Header("Impact")] [SerializeField] float amplitudeGain = 1f;
        [SerializeField] float frequencyGain = 1f;

        [Header("Time Envelope")] [SerializeField]
        float attackTime = 0f;

        [SerializeField] float sustainTime = 0.2f;
        [SerializeField] float decayTime = 0.5f;

        public SignalSourceAsset ImpulseSignal => impulseSignal;
        public float AmplitudeGain => amplitudeGain;
        public float FrequencyGain => frequencyGain;
        public float AttackTime => attackTime;
        public float SustainTime => sustainTime;
        public float DecayTime => decayTime;
    }
}