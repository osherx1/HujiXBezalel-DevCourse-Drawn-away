using System;
using UnityEngine;
using System.Collections;
using Drawing.Data;
using Drawing.Utilities;
using Drawing.Utilities.Pool; // Required for Coroutines
using Drawing.Managers;

namespace Drawing.Managers
{
    using System.Collections;
    using UnityEngine;

    namespace Core.Managers
    {
        /// <summary>
        /// Manages sound effects and background music for the game.
        /// Singleton pattern ensures only one instance exists.
        /// </summary>
        public class AudioManager : MonoSingleton<AudioManager>
        {
            /// <summary>
            /// Reference to the ScriptableObject containing all audio clips.
            /// </summary>
            [SerializeField] private GameSoundsSo gameSoundsSo;

            /// <summary>
            /// Audio source for playing short sound effects.
            /// </summary>
            [SerializeField] private AudioSource audioSource;

            /// <summary>
            /// Audio source dedicated to background music playback.
            /// </summary>
            [SerializeField] private AudioSource backgroundMusic;

            /// <summary>
            /// Whether to automatically start background music on Awake.
            /// </summary>
            [SerializeField] private bool startWithBackgroundMusic;

            [SerializeField] private bool muteOnGameFinished = true;

            private bool _isMutedFromGameEnd;
            private float _volMult;
            private bool _isPaused;
            private float _musicVolumeSetting = 1f;
            private float _sfxVolumeSetting = 1f;

            // 1. Add these constants at the top of the class
            //private const string BACKGROUND_VOLUME_KEY = "MusicVolume";
            //private const string SFX_VOLUME_KEY = "SFXVolume";
            private const string VOLUME_MULT_KEY = "VolumeMult";

            private void Awake()
            {
                _volMult = PlayerPrefs.GetFloat(VOLUME_MULT_KEY, 1.0f);
            }

            // 2. Update the Start method to load saved values
            void Start()
            {
                // Load saved volumes (default to 1.0f if not found)
                //float savedMusicVol = PlayerPrefs.GetFloat(BACKGROUND_VOLUME_KEY, 1.0f);
               // float savedSFXVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f);
                
                

                // Apply the loaded values
                //SetBackgroundMusicVolume(_volMult);
                //SetSFXVolume(_volMult);

                if (startWithBackgroundMusic)
                {
                    PlayBackgroundMusic();
                }
            }

            // 3. Update SetBackgroundMusicVolume to save the value
            public void SetBackgroundMusicVolume(float volume)
            {
                _musicVolumeSetting = Mathf.Clamp01(volume);
                UpdateMusicOutput();
                
            }

            private void UpdateMusicOutput()
            {
                if (backgroundMusic != null)
                {
                   
                    backgroundMusic.volume = _musicVolumeSetting * _volMult;
                }
               // Debug.Log($"[AudioManager] Music Setting Changed: {_musicVolumeSetting} | Output Volume: {backgroundMusic.volume}");
                
            }

            public float GetVolumeMult()
            {
                return PlayerPrefs.GetFloat(VOLUME_MULT_KEY, 1f);
            }

            public void SetVolumeMult(float volume)
            {
                float oldVol = _volMult;
                _volMult = Mathf.Clamp01(volume);
                PlayerPrefs.SetFloat(VOLUME_MULT_KEY, _volMult);
            
                UpdateMusicOutput();
            
               // Debug.Log($"[AudioManager] Master Volume Changed: {oldVol} -> {_volMult}");
            }

            

            // 4. Update SetSFXVolume to save the value
            public void SetSFXVolume(float volume)
            {
                if (audioSource != null)
                {
                    volume = Mathf.Clamp01(volume);
                    audioSource.volume = volume;

                    /*// Save to PlayerPrefs
                    PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);*/
                }
            }

// 5. Add these Getter methods (needed for the UI Slider)
            public float GetMusicVolume()
            {
                return _musicVolumeSetting;
            }

            public float GetSFXVolume()
            {
                return audioSource != null ? audioSource.volume : 1f;
            }


            private void OnEnable()
            {
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.OnGameFinished += HandleGameFinished;
                    EventManager.Instance.OnGamePausedChanged += HandleGamePaused;
                }
            }

        

            private void OnDisable()
            {
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.OnGameFinished -= HandleGameFinished;
                    EventManager.Instance.OnGamePausedChanged -= HandleGamePaused;
                }
            }

       

            /// <summary>
            /// Plays the background music on a loop.
            /// </summary>
            public void PlayBackgroundMusic()
            {
                if (backgroundMusic != null)
                {
                    backgroundMusic.loop = true;
                    backgroundMusic.Play();
                }
            }

            public AudioSource GetBackgroundMusicAudioSource()
            {
                return backgroundMusic;
            }

            public void SetBackgroundMusic(GameSoundsSo.AudioType audioType)
            {
                if (audioType == GameSoundsSo.AudioType.None)
                {
                    StopBackgroundMusic();
                    return;
                }

                AudioClip clip = gameSoundsSo.GetClip(audioType);

                if (clip != null)
                {
                    backgroundMusic.clip = clip;
                    backgroundMusic.Play();
                }
                else
                {
                   // Debug.LogWarning($"Background music {audioType} not found!");
                }
            }

            /// <summary>
            /// Plays a sound effect based on the audio type defined in the GameSoundsSO.
            /// </summary>
            /// <param name="audioType">The type of sound to play.</param>
            public void PlaySoundByAudioType(GameSoundsSo.AudioType audioType, float volumeScale = 1.0f)
            {
                if (audioType == GameSoundsSo.AudioType.None) return;
                if (_isMutedFromGameEnd||_isPaused) return;

                AudioClip clip = gameSoundsSo.GetClip(audioType);
                if (clip != null)
                {
                    AudioObject sound = AudioPool.Instance.Get();
                    if (sound != null)
                    {
                        sound.Play(clip, volumeScale*_volMult);
                    }
                    else
                    {
                        // PlayOneShot without pooling fallback
                        audioSource.PlayOneShot(clip, volumeScale*_volMult);
                    }
                }
                else
                {
                    // Optional: Reduce log noise if needed
                    // Debug.LogWarning($"Sound {audioType} not found!");
                }
            }


            public void SetMusicPitch(float pitch)
            {
                if (backgroundMusic != null)
                {
                    backgroundMusic.pitch = pitch;
                }
            }

            /// <summary>
            /// Plays a given AudioSource with optional volume control.
            /// </summary>
            /// <param name="audioSource">The AudioSource to play.</param>
            /// <param name="volume">The volume (default is 0.8).</param>
            public void PlaySound(AudioSource audioSource, float volume = 0.8f)
            {
                if (_isMutedFromGameEnd||_isPaused) return;
                audioSource.volume = volume*_volMult;
                audioSource.Play();
            }


            /// <summary>
            /// Resumes the background music if it was paused.
            /// </summary>
            public void ResumeBackgroundMusic()
            {
                if (_isMutedFromGameEnd) return;
                if (!backgroundMusic.isPlaying)
                {
                    backgroundMusic.UnPause();
                }
            }

            /// <summary>
            /// Stops the background music completely.
            /// </summary>
            public void StopBackgroundMusic()
            {
                if (backgroundMusic.isPlaying)
                {
                    backgroundMusic.Stop();
                }
            }

            /// <summary>
            /// Fades out the background music over the given duration.
            /// </summary>
            /// <param name="duration">Duration of the fade in seconds.</param>
            public void FadeOutBackground(float duration)
            {
                StartCoroutine(FadeOutCoroutine(duration));
            }

            /// <summary>
            /// Coroutine for fading out the background music.
            /// </summary>
            /// <param name="duration">Duration of the fade.</param>
            private IEnumerator FadeOutCoroutine(float duration)
            {
                float startVolume = backgroundMusic.volume;

                while (backgroundMusic.volume > 0)
                {
                    backgroundMusic.volume -= startVolume * Time.deltaTime / duration;
                    yield return null;
                }

                backgroundMusic.Stop();
                backgroundMusic.volume = startVolume;
            }

            /// <summary>
            /// Changes the background music to a new clip and plays it.
            /// </summary>
            /// <param name="newClip">The new AudioClip to play.</param>
            public void SetBackgroundMusic(AudioClip newClip)
            {
                if (_isMutedFromGameEnd) return;
                if (backgroundMusic.clip != newClip)
                {
                    backgroundMusic.clip = newClip;
                    backgroundMusic.Play();
                }
            }

            private void HandleGameFinished()
            {
                if (!muteOnGameFinished || _isMutedFromGameEnd)
                {
                    return;
                }

                _isMutedFromGameEnd = true;
                StopBackgroundMusic();
                if (audioSource != null)
                {
                    audioSource.Stop();
                }

                if (backgroundMusic != null)
                {
                    backgroundMusic.loop = false;
                }
            }
            private void HandleGamePaused(bool isPaused)
            {
                _isPaused = isPaused;
                //Debug.Log($"[AudioManager] Pause State Changed: {isPaused}");
                if (isPaused)
                {
                    if (backgroundMusic != null) backgroundMusic.Pause();
                    if(audioSource!= null) audioSource.Pause();
                }
                else
                {
                    ResumeBackgroundMusic();
                }
            }
        

            public AudioClip GetClip(GameSoundsSo.AudioType drawSound)
            {
                return gameSoundsSo.GetClip(drawSound);
            }
        }
    }
}