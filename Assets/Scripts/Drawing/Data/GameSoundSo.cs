using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Drawing.Data
{
    /// <summary>
    /// A ScriptableObject that maps AudioTypes to corresponding AudioClips.
    /// Used by the AudioManager to play sounds by logical type.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSoundsSo", menuName = "Scriptable Objects/GameSoundsSo")]
    public class GameSoundsSo : ScriptableObject
    {

        [SerializeField] private List<GameSound> gameSounds = new List<GameSound>();
        private Dictionary<AudioType, GameSound> _soundsDict;

 
        private void OnEnable()
        {
            if (_soundsDict == null || _soundsDict.Count == 0)
            {
                _soundsDict = new Dictionary<AudioType, GameSound>();
                foreach (var gameSound in gameSounds)
                {
                    _soundsDict[gameSound.audioType] = gameSound;
                }
            }
        }


        public AudioClip GetClip(AudioType audioType)
        {
            return _soundsDict.TryGetValue(audioType, out GameSound gameSound) ? gameSound.clip : null;
        }
        [Serializable] public class GameSound
        {
 
            public AudioType audioType;
            public AudioClip clip;
        }


        public enum AudioType
        {
            // General game events
            None = -1,
            GameStart = 0,
            GameOver = 1,
            VictoryScreen = 2,
            ButtonClick = 3,
            LevelComplete = 4,
            
            
            PencilDraw = 10,
            SpringDraw = 11,
            IronDraw = 12,
            BalloonDraw = 13,
            StaticDraw = 14,
            
            PencilCollision = 20,
            SpringCollision = 21,
            IronCollision = 22,
            BalloonCollision = 23,
            
            
            PencilRelease = 30,
            SpringRelease = 31,
            IronRelease = 32,
            BalloonRelease = 33,
            RockHit = 40,
            RockShatter = 41,
            GiantRoar = 50
            
            ,CheckpointReached = 60,
            DoorOpen = 61,
            CaveAmbiance = 70,
            ForestAmbiance = 71,
            BossEvilLaugh = 80,
            BackgroundMusicMainTheme = 90,
            OldManMumbling = 100,
            PickupSound = 110,
            Illanaspeak = 120,
            JonnyWalk = 121,
            Swordpickup = 122,
            SwordUI = 123
        }
    }
}
