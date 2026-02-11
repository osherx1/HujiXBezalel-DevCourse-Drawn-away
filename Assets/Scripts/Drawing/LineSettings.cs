using System;
using UnityEngine;
using CustomInspector;
using Drawing.Data;
using Drawing.LineControl; // Required for the new attributes

namespace Drawing
{
    [System.Serializable]
    public class LineSettings
    {
        [HideInInspector]public string SettingID;
        public int fillMult = 1;



        
        [Tooltip("If true, use a specific Line prefab. If false, configure manually.")]
        public bool usePrefab;

        [SerializeField] [AssetsOnly] [ShowIf(nameof(usePrefab))]
        public GameObject linePrefab; // Added to avoid errors in LineDrawer when using prefab mode.

        // ----------------- TAB: APPEARANCE -----------------
        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Appearance")]
        [HorizontalLine("Render Mode")]
        [Tooltip("The color gradient of the line over its lifetime.")]
        public Gradient lineColor;

        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Appearance")] [Range(0.1f, 5f)]
        // Kept Unity's Range, could use [DynamicSlider] if you want adjustable limits
        public float lineWidth = 0.2f;
        /*
        [Tab("Appearance")]
        [Tooltip("Controls the width of the line along its length.")]
        public AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
        */


        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Appearance")]
        [ForceFill(errorMessage = "Line Material is required!")] // Alert if empty
        [AssetsOnly]
        // Ensures you don't accidentally drag a scene material
        public Material material;

        [Tab("Appearance")] public bool useDifferentMaterialBeforePhysics;
        [ShowIf(nameof(useDifferentMaterialBeforePhysics))]
        [Tab("Appearance")]
        [AssetsOnly]
        // Ensures you don't accidentally drag a scene material
        public Material materialBeforePhysics;



        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Appearance")]
        [HorizontalLine("Vertex Settings", 1, FixedColor.Gray)] // Visual separator
        [Range(0, 90)]
        public int endCapVertices = 0;

        //[ShowIfNot(nameof(usePrefab))] 
        [Tab("Appearance")] [Range(0, 90)] public int cornerVertices = 0;

        //[ShowIfNot(nameof(usePrefab))] 
        [Tab("Appearance")]
        //change the line rendering mode
        [Tooltip("The texture mode of the line.")]
        //public TextureWrapMode textureMode = TextureWrapMode.Repeat;
        public LineTextureMode lineTextureMode = LineTextureMode.Stretch;

        /*public Texture textureMode = TextureMode.Stretch;*/


        // ----------------- TAB: PHYSICS -----------------
        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Physics")] [MessageBox("Disable physics to improve performance on static lines.", MessageBoxType.Info)]
        public bool usePhysics = true;

        // The following fields only show if 'usePhysics' is TRUE
        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Physics")] [ShowIf(nameof(usePhysics))] [Indent(1)] [AssetsOnly]
        // Indent to show hierarchy visually
        public PhysicsMaterial2D physicsMaterial;

        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Physics")] [ShowIf(nameof(usePhysics))] [Indent(1)]
        public float gravityScaleOverride = 1f;

        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Physics")] [ShowIf(nameof(usePhysics))] [Indent(1)]
        public float massMult = 1f;


        // ----------------- TAB: SOUND -----------------

        [Tab("Sound")] [Range(0, 1)]
        //[ShowIfNot(nameof(usePrefab))]  [Indent(0)]
        public float baseVolume = 1.0f;
        // 1. Draw Sound
        [Hook(nameof(OnDrawSoundBoolChanged))]
        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Sound")]
        [HorizontalLine("Draw Audio")]
        public bool useDrawSound;

        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Sound")] [ShowIf(nameof(useDrawSound))] [Indent(1)]
        public GameSoundsSo.AudioType drawSound = GameSoundsSo.AudioType.None;
        [Tab("Sound")][ShowIf(nameof(useDrawSound))] [Range(0, 1)] [Indent(1)]
        //[ShowIfNot(nameof(usePrefab))]  [Indent(0)]
        public float baseDrawSoundVolume = 1.0f;


        // 2. Collision Sound
        [Hook(nameof(OnCollisionSoundBoolChanged))]
        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Sound")]
        [HorizontalLine("Collision Audio")]
        public bool useCollisionSound;

        //[ShowIfNot(nameof(usePrefab))] 

        [Tab("Sound")] [ShowIf(nameof(useCollisionSound))] [Indent(1)]
        public GameSoundsSo.AudioType collisionSound = GameSoundsSo.AudioType.None;
        [Tab("Sound")][ShowIf(nameof(useCollisionSound))] [Range(0, 1)] [Indent(1)]
        //[ShowIfNot(nameof(usePrefab))]  [Indent(0)]
        public float baseCollisionSoundVolume = 1.0f;

        // 3. Release Sound
        [Hook(nameof(OnReleaseSoundBoolChanged))]
        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Sound")]
        [HorizontalLine("Release Audio")]
        public bool useReleaseSound;

        //[ShowIfNot(nameof(usePrefab))]
        [Tab("Sound")] [ShowIf(nameof(useReleaseSound))] [Indent(1)]
        public GameSoundsSo.AudioType releaseSound = GameSoundsSo.AudioType.None;
        [Tab("Sound")][ShowIf(nameof(useReleaseSound))] [Range(0, 1)] [Indent(1)]
        //[ShowIfNot(nameof(usePrefab))]  [Indent(0)]
        public float baseReleaseSoundVolume = 1.0f;



        [Tab("Sound")]
        //[ShowIfNot(nameof(usePrefab))]  [Indent(0)]
        public bool useCameraShake = false;

        
        // ----------------- TAB: DRAWING -----------------
        [Tab("Drawing")]
        [Tooltip("If true, this tool ignores LineManager.cantDrawOverLayer while drawing (useful for Glue so it can be drawn on 'non-drawable' surfaces).")]
        public bool ignoreCantDrawOverLayer = false;

        public void SetLineSetting(LineSettings otherSettings)
        {
            usePrefab = otherSettings.usePrefab;
            linePrefab = otherSettings.linePrefab;
            fillMult = otherSettings.fillMult;
            SettingID = otherSettings.SettingID;
            ignoreCantDrawOverLayer = otherSettings.ignoreCantDrawOverLayer;
            SetAppearance(otherSettings);
            SetPhysics(otherSettings);
            SetSound(otherSettings);
        }

        public void OnDrawSoundBoolChanged()
        {
            if (!useDrawSound)
            {
                // If the boolean is set to false, reset the corresponding sound to None
                drawSound = GameSoundsSo.AudioType.None;
            }
        }

        public void OnCollisionSoundBoolChanged()
        {
            if (!useCollisionSound)
            {
                // If the boolean is set to false, reset the corresponding sound to None
                collisionSound = GameSoundsSo.AudioType.None;
            }
        }

        public void OnReleaseSoundBoolChanged()
        {
            if (!useReleaseSound)
            {
                // If the boolean is set to false, reset the corresponding sound to None
                releaseSound = GameSoundsSo.AudioType.None;
            }
        }


        private void SetSound(LineSettings otherSettings)
        {
            // Sound - Draw
            useDrawSound = otherSettings.useDrawSound;
            drawSound = useDrawSound ? otherSettings.drawSound : GameSoundsSo.AudioType.None;

            // Sound - Collision
            useCollisionSound = otherSettings.useCollisionSound;
            collisionSound = useCollisionSound ? otherSettings.collisionSound : GameSoundsSo.AudioType.None;

            // Sound - Release
            useReleaseSound = otherSettings.useReleaseSound;
            releaseSound = useReleaseSound ? otherSettings.releaseSound : GameSoundsSo.AudioType.None;

            baseVolume = otherSettings.baseVolume;
            baseDrawSoundVolume = otherSettings.baseDrawSoundVolume;
            baseCollisionSoundVolume = otherSettings.baseCollisionSoundVolume;
            baseReleaseSoundVolume = otherSettings.baseReleaseSoundVolume;

            useCameraShake = otherSettings.useCameraShake;
        }

        private void SetPhysics(LineSettings otherSettings)
        {
            // Physics
            usePhysics = otherSettings.usePhysics;
            physicsMaterial = otherSettings.physicsMaterial;
            gravityScaleOverride = otherSettings.gravityScaleOverride;
            massMult = otherSettings.massMult;
        }

        private void SetAppearance(LineSettings otherSettings)
        {
            // Appearance
            lineWidth = otherSettings.lineWidth;
            material = otherSettings.material;
            useDifferentMaterialBeforePhysics = otherSettings.useDifferentMaterialBeforePhysics;
            if (useDifferentMaterialBeforePhysics&& otherSettings.materialBeforePhysics != null)
            {
                materialBeforePhysics = otherSettings.materialBeforePhysics;
            }
            else
            {
                materialBeforePhysics = otherSettings.material;
            }
            
            endCapVertices = otherSettings.endCapVertices;
            cornerVertices = otherSettings.cornerVertices;
            if (otherSettings.lineColor != null)
            {
                lineColor = new Gradient();
                lineColor.SetKeys(otherSettings.lineColor.colorKeys, otherSettings.lineColor.alphaKeys);
                lineColor.mode = otherSettings.lineColor.mode;
            }
            /*if (otherSettings.widthCurve != null)
            {
                // Create a new curve using the keys from the other one to avoid reference linking
                widthCurve = new AnimationCurve(otherSettings.widthCurve.keys);
                
                // Copy wrapping mode settings (Loop, PingPong, etc.)
                widthCurve.preWrapMode = otherSettings.widthCurve.preWrapMode;
                widthCurve.postWrapMode = otherSettings.widthCurve.postWrapMode;
            }
            else
            {
                // Reset to default linear if source is null
                widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
            }*/

            lineTextureMode = otherSettings.lineTextureMode;
        }
    }
}
/*using Drawing.Data;
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
}*/