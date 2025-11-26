using System.Collections;
using UnityEngine;

namespace Drawing.Utilities.Pool
{
        [RequireComponent(typeof(AudioSource))]
        public class AudioObject : MonoBehaviour, IPoolable
        {
            private AudioSource _audioSource;
            private Coroutine _playingCoroutine;
            private float _lengthAdditionBuffer = 0.01f;

            private void Awake()
            {
                _audioSource = GetComponent<AudioSource>();
            }

            public void Play(AudioClip clip, float volume, float pitch = 1f)
            {
                if (clip == null) return;
                
                _audioSource.clip = clip;
                _audioSource.volume = volume;
                _audioSource.pitch = pitch;
                _audioSource.Play();
                if (_playingCoroutine != null) StopCoroutine(_playingCoroutine);
                _playingCoroutine = StartCoroutine(WaitForSoundToEnd());
            }

            private IEnumerator WaitForSoundToEnd()
            {
                yield return new WaitForSeconds(_audioSource.clip.length + _lengthAdditionBuffer);
                ReturnToPool();
            }

            private void ReturnToPool()
            {

                AudioPool.Instance.Return(this);
            }

            public void Reset()
            {
                _audioSource.Stop();
                _audioSource.clip = null;
                _audioSource.volume = 1f;
                _audioSource.pitch = 1f;
                _audioSource.loop = false;
            }
        }
    
    }
    
