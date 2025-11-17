using UnityEngine;

namespace Drawing
{
    public class DrawingConfigController : MonoBehaviour
    {
        public static DrawingConfigController Instance;

        public LineSettings currentSettings = new LineSettings();

        void Awake() => Instance = this;

        public void SetWidth(float w) => currentSettings.lineWidth = w;
        public void SetColor(Gradient g) => currentSettings.lineColor = g;
        public void SetUsePhysics(bool v) => currentSettings.usePhysics = v;
        public void SetPhysicsMaterial(PhysicsMaterial2D mat) => currentSettings.physicsMaterial = mat;
        /*
        public void SetUsePolygon(bool v) => currentSettings.usePolygonCollider = v;
        */
        
        public void SetGravity(float g) => currentSettings.gravityScaleOverride = g;
        public void SetGravityActivate(bool b) => currentSettings.changeGravityActivate = b;
        
        public LineSettings GetCurrentSettings() => currentSettings;
        
    }


    [System.Serializable]
    public class LineSettings
    {
        //public Gradient lineColor;

        public float lineWidth = 0.2f;
        /*
        public float minDistance = 0.05f;
        */
        public bool usePhysics = true;

        public PhysicsMaterial2D physicsMaterial;

        public float gravityScaleOverride;

        public bool changeGravityActivate;

        public Gradient lineColor;

        public bool changeColor;

        public bool changeGravityScale;
        /*public bool dynamicOnRelease = true;
        public bool usePolygonCollider = true;*/
        /*public float colliderSimplifyTolerance = 0.03f;
        public int maxColliderPoints = 128;*/
    }
    
    
}