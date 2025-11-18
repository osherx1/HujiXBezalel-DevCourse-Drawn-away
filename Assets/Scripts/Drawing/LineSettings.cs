using UnityEngine;

namespace Drawing
{
    [System.Serializable]
    public class LineSettings
    {
        public Gradient lineColor;
        public float lineWidth = 0.2f;
        public bool usePhysics = true;
        public PhysicsMaterial2D physicsMaterial;
        public float gravityScaleOverride = 1f;
        public float massMult = 1f;
        public Material material;
    }
}