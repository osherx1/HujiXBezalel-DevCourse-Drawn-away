using Drawing.Data;
using UnityEngine;

namespace Drawing
{
    [System.Serializable]
    public class LineSettings
    {
        [Header("Line Appearance")]
        public Gradient lineColor;
        public float lineWidth = 0.2f;
        public Material material;
        [Range(0,90)]
        public int endCapVertices = 0;
        [Range(0,90)]
        public int cornerVertices = 0;
        [Space(5)]
        [Header("Line Physics")]
        public bool usePhysics = true;
        public PhysicsMaterial2D physicsMaterial;
        public float gravityScaleOverride = 1f;
        public float massMult = 1f;
        [Space(5)]
        [Header("Sound Settings")]
        public GameSoundsSo.AudioType drawSound = GameSoundsSo.AudioType.None ;
        public GameSoundsSo.AudioType collisionSound = GameSoundsSo.AudioType.None ;
        public GameSoundsSo.AudioType releaseSound = GameSoundsSo.AudioType.None ;
        
                
        
    }
}