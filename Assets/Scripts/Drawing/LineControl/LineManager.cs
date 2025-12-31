using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using Drawing.Data;
using Drawing.Managers.Core.Managers;
using Drawing.Managers;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.Controls;
#endif

namespace Drawing.LineControl
{
    /// <summary>
    /// Simple input manager to draw physics-enabled lines using the Line component.
    /// Supports mouse (editor/standalone) and touch (mobile).
    /// Drop this on an empty GameObject (e.g., "LineManager") and set materials in the inspector.
    /// </summary>
    public class LineManager : MonoBehaviour
    {
        public event Action<Line> OnLineStarted;
        public event Action<Line> OnLineFinished;
        [Header("Appearance")] public float lineWidth = 0.2f;
        public float minDistance = 0.05f;

        [Header("Physics")] public PhysicsMaterial2D physicsMaterial2D;
        public bool usePolygonCollider = true;

        [Tooltip(
            "If true, maintain an EdgeCollider2D while drawing (more expensive). If false, create collider only at finalize.")]
        public bool collideWhileDrawing = false;

        [Header("Optimization")]
        [Tooltip(
            "Simplification tolerance (world units) used before baking colliders. Higher = fewer points, faster physics.")]
        public float colliderSimplifyTolerance = 0.03f;

        [Tooltip("Clamp the number of points used to bake the collider. Lower = faster. 128 is a good default.")]
        public int maxColliderPoints = 128;

        [Header("Collision")] [Tooltip("Lines on these layers will block drawing (finish line) while drawing.")]
        public LayerMask cantDrawOverLayer;

        [Tooltip("Multiplier applied to the line width when checking for overlap.")] [Range(0.05f, 1f)]
        public float drawOverlapPadding = 0.3f;

        [Tooltip("Seconds to wait before moving a finished line to the CantDrawOver layer.")] [Min(0f)]
        public float cantDrawLayerDelay = 0.1f;

        [Header("Draw Area Limit")] [Tooltip("Restrict drawing to an area around the selected center.")]
        public bool limitDrawingArea = false;

        [Tooltip("Center point used when limiting drawing (e.g., the player).")]
        public Transform drawAreaCenter;

        public DrawAreaShape drawAreaShape = DrawAreaShape.Circle;
        [Min(0.1f)] public float circleRadius = 5f;
        public Vector2 rectangleSize = new Vector2(8f, 4f);

        [Tooltip("Render a visible area in Game view to preview the drawing limit.")]
        public bool showDrawAreaVisual = true;

        [Tooltip(
            "Optional LineRenderer used to display the drawing limit. If empty, one will be created automatically.")]
        public LineRenderer drawAreaRenderer;

        [Tooltip("Color used by the runtime draw-area visualization.")]
        public Color drawAreaLineColor = new Color(0.1f, 1f, 1f, 0.6f);

        [Min(0.001f)] public float drawAreaLineWidth = 0.05f;
        [Range(8, 128)] public int circleSegments = 48;

        [Header("Input")] [Tooltip("If tip isn't reported by the pen, use pressure >= this to treat as pressed.")]
        public float penPressureThreshold = 0.15f;

        [Tooltip("Log pen press state changes for debugging.")]
        public bool logPenDebug = false;

        [Tooltip("Log mouse press state changes for debugging.")]
        public bool logMouseDebug = false;

        [Tooltip("Log touch press state changes for debugging.")]
        public bool logTouchDebug = false;

        [Header("Optional Prefab")] public GameObject linePrefab; // optional prefab with Line component already

        [Header("World Parenting")]
        public Transform linesRoot; // parent for lines (null = world root, recommended to keep lines out of Canvas)

        [FormerlySerializedAs("creationEffect")]
        [Header("Visual Effects")]
        [Tooltip("Particle system to play when the line is finished.")]
        [SerializeField]
        private ParticleSystem releaseEffect;

        [SerializeField] private ParticleSystem drawEffect;

        private Line currentLine;
        private bool isDrawing;
        private bool _penPressed;
        private bool _warnedCantDrawMaskOnce;
        private bool _warnedMissingDrawCenter;
        private bool _isGameFinished;
        private Material _drawAreaMaterial;
        private float _currentFillAmount;
        private float _previousLineLength;
        private float _inkBuffer;

        void Awake()
        {
            if (showDrawAreaVisual)
            {
                EnsureDrawAreaRenderer();
            }
        }

        void OnEnable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished += HandleGameFinished;
            }
        }

        void OnDisable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished -= HandleGameFinished;
            }
        }

        void Update()
        {
            if (_isGameFinished)
            {
                UpdateDrawAreaVisualization();
                return;
            }
#if ENABLE_INPUT_SYSTEM
            if (Camera.main == null) return;
            var pen = Pen.current;
            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            // Pen (graphics tablet) support
            if (pen != null)
            {
                bool tip = false;
                float pressure = 0f;
                Vector2 penPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                ButtonControl barrel1 = null;
                ButtonControl barrel2 = null;
                ButtonControl eraserBtn = null;
#endif
                try
                {
                    tip = pen.tip.isPressed;
                }
                catch
                {
                    tip = false;
                }

                try
                {
                    pressure = pen.pressure.ReadValue();
                }
                catch
                {
                    pressure = 0f;
                }

                try
                {
                    penPos = pen.position.ReadValue();
                }
                catch
                {
                    penPos = Vector2.zero;
                }
#if ENABLE_INPUT_SYSTEM
                try
                {
                    barrel1 = pen.TryGetChildControl<ButtonControl>("firstBarrelButton") ??
                              pen.TryGetChildControl<ButtonControl>("barrelButton") ??
                              pen.TryGetChildControl<ButtonControl>("barrel");
                }
                catch
                {
                    barrel1 = null;
                }

                try
                {
                    barrel2 = pen.TryGetChildControl<ButtonControl>("secondBarrelButton") ??
                              pen.TryGetChildControl<ButtonControl>("barrelButton2");
                }
                catch
                {
                    barrel2 = null;
                }

                try
                {
                    eraserBtn = pen.TryGetChildControl<ButtonControl>("eraser");
                }
                catch
                {
                    eraserBtn = null;
                }
#endif
                bool pressed = tip || pressure >= penPressureThreshold
#if ENABLE_INPUT_SYSTEM
                                   || (barrel1 != null && barrel1.isPressed)
                                   || (barrel2 != null && barrel2.isPressed)
                                   || (eraserBtn != null && eraserBtn.isPressed)
#endif
                    ;

                if (pressed && !_penPressed)
                {
                    if (IsPointerOverUI())
                    {
                        return;
                    }
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(penPos));
                    StartLine(wp);
                    _penPressed = true;
                    // if (logPenDebug) Debug.Log($"LineManager: Pen down (tip={tip}, pressure={pressure:F2}).", this);
                    return; // Prefer pen over others this frame
                }

                if (pressed && currentLine != null)
                {
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(penPos));
                    if (IsBlocked(wp))
                    {
                        FinishLine(wp);
                        _penPressed = false;
                        // if (logPenDebug) Debug.Log("LineManager: Pen blocked by overlap; finishing line.", this);
                        return;
                    }

                    TryAddPoint(wp);
                    //currentLine.AddWorldPoint(wp);
                    return;
                }

                if (!pressed && _penPressed)
                {
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(penPos));
                    FinishLine(wp);
                    _penPressed = false;
                    // if (logPenDebug) Debug.Log("LineManager: Pen up.", this);
                    return;
                }
            }

            // Mouse
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (IsPointerOverUI())
                    {
                        return;
                    }
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(mouse.position.ReadValue()));
                    StartLine(wp);
                    // if (logMouseDebug) Debug.Log("LineManager: Mouse down.", this);
                }

                if (mouse.leftButton.isPressed && currentLine != null)
                {
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(mouse.position.ReadValue()));
                    // Prevent drawing over existing finalized lines
                    if (IsBlocked(wp))
                    {
                        FinishLine(wp);
                        // if (logMouseDebug) Debug.Log("LineManager: Mouse blocked by overlap; finishing line.", this);
                    }
                    else
                    {
                        TryAddPoint(wp);
                        //currentLine.AddWorldPoint(wp);
                    }
                }

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(mouse.position.ReadValue()));

                    FinishLine(wp);
                    // if (logMouseDebug) Debug.Log("LineManager: Mouse up.", this);
                }
            }

            // Touch (primary)
            if (touch != null && touch.touches.Count > 0)
            {
                var primary = touch.touches[0];

                if (primary.press.wasPressedThisFrame)
                {
                    if (IsPointerOverUI(primary.touchId.ReadValue()))
                    {
                        return;
                    }
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(primary.position.ReadValue()));

                    StartLine(wp);

                    // if (logTouchDebug) Debug.Log("LineManager: Touch down.", this);
                }

                if (primary.press.isPressed && currentLine != null)
                {
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(primary.position.ReadValue()));
                    UpdateDrawEffectPos(wp);
                    // Prevent drawing over existing finalized lines
                    if (IsBlocked(wp))
                    {
                        FinishLine(wp);
                        // if (logTouchDebug) Debug.Log("LineManager: Touch blocked by overlap; finishing line.", this);
                    }
                    else
                    {
                        currentLine.AddWorldPoint(wp);
                    }
                }

                if (primary.press.wasReleasedThisFrame)
                {
                    Vector2 wp = ClampToDrawArea(Camera.main.ScreenToWorldPoint(primary.position.ReadValue()));

                    FinishLine(wp);
                    // if (logTouchDebug) Debug.Log("LineManager: Touch up.", this);
                }
            }
#endif

            UpdateDrawAreaVisualization();
        }

        void StartLine(Vector2 worldPos)
        {
            if (_isGameFinished)
            {
                return;
            }

            var drawing = DrawingConfigController.Instance;

            if (drawing.CheckInk() <= 1)
            {
                // Not enough ink to start a line
                Debug.Log("LineManager: Not enough ink to start a new line.");
                return;
            }

            var conf = drawing.currentSettings;
            GameObject lineToInstantiate;
            if (conf.usePrefab && conf.linePrefab != null)
            {
                lineToInstantiate = conf.linePrefab;
            }
            else
            {
                lineToInstantiate = linePrefab;
            }

            GameObject go;
            if (lineToInstantiate != null)
            {
                go = linesRoot ? Instantiate(lineToInstantiate, linesRoot) : Instantiate(lineToInstantiate);
            }
            else
            {
                go = new GameObject("Line");
                if (linesRoot) go.transform.SetParent(linesRoot, false);
            }

            Line ln = go.GetComponent<Line>();
            if (ln == null) ln = go.AddComponent<Line>();

            // Initialize so Awake-created components get proper settings
            ln.Initialize(
                conf.SettingID, conf.lineWidth, minDistance, conf.physicsMaterial, usePolygonCollider,
                collideWhileDrawing,
                colliderSimplifyTolerance, maxColliderPoints,
                conf.lineColor, conf.material, conf.endCapVertices, conf.cornerVertices, conf.lineTextureMode);
            ln.InitializeSound(conf.collisionSound, conf.baseVolume, conf.useCameraShake);

            currentLine = ln;

            TryAddPoint(worldPos);


            isDrawing = true;
            //currentLine.AddWorldPoint(worldPos);
        }

        void FinishLine(Vector2 wp)
        {
            if (drawEffect != null && drawEffect.isPlaying)
            {
                drawEffect.Stop();
            }

            if (currentLine == null)
            {
                return;
            }


            var conf = DrawingConfigController.Instance.currentSettings;
            isDrawing = false;
            // If too short, discard
            if (currentLine.pointsCount < 2)
            {
                
                Destroy(currentLine.gameObject);
            }
            else
            {
                ScheduleCantDrawLayerSwitch(currentLine.gameObject);

                // Build a solid polygon (optional) and activate physics so it will fall/interact in world space
                currentLine.FinalizeLine(conf);
                if (conf.releaseSound != GameSoundsSo.AudioType.None)
                    AudioManager.Instance.PlaySoundByAudioType(conf.releaseSound);
                // Trigger Particle System at the final position
                if (releaseEffect != null)
                {
                    releaseEffect.transform.position = wp;
                    releaseEffect.Play();
                }

                int finishInkCost = DrawingConfigController.Instance.FinishLineInkCost();
                if (!DrawingConfigController.Instance.TryConsumeInk(finishInkCost))
                {
                    DrawingConfigController.Instance.ResetInk();
                }

                currentLine.AddInkCost(finishInkCost);
            }


            currentLine = null;
        }

        // Compute the overlap radius used to stop drawing when we hit existing lines
        float GetOverlapRadius()
        {
            var conf = DrawingConfigController.Instance != null
                ? DrawingConfigController.Instance.currentSettings
                : null;
            float width = conf != null ? conf.lineWidth : lineWidth;
            float padding = Mathf.Clamp(drawOverlapPadding, 0.01f, 1f);
            return Mathf.Max(0.001f, width * padding);
        }

        bool IsBlocked(Vector2 worldPoint)
        {
            if (cantDrawOverLayer.value == 0)
            {
                if (!_warnedCantDrawMaskOnce)
                {
                    // Debug.LogWarning("LineManager: 'cantDrawOverLayer' is not set. Overlap blocking will not work.", this);
                    _warnedCantDrawMaskOnce = true;
                }

                return false;
            }

            return Physics2D.OverlapCircle(worldPoint, GetOverlapRadius(), cantDrawOverLayer);
        }

        void ScheduleCantDrawLayerSwitch(GameObject lineObject)
        {
            if (lineObject == null || lineObject.layer != 0)
            {
                return;
            }

            if (cantDrawLayerDelay <= 0f)
            {
                AssignCantDrawLayer(lineObject);
            }
            else
            {
                StartCoroutine(ApplyCantDrawLayerDelayed(lineObject, cantDrawLayerDelay));
            }
        }

        void AssignCantDrawLayer(GameObject lineObject)
        {
            if (lineObject == null)
            {
                return;
            }

            int cantDrawIdx = LayerMask.NameToLayer("CantDrawOver");
            if (cantDrawIdx >= 0)
            {
                lineObject.layer = cantDrawIdx;
            }
        }

        IEnumerator ApplyCantDrawLayerDelayed(GameObject lineObject, float delay)
        {
            if (lineObject == null)
            {
                yield break;
            }

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (lineObject != null && lineObject.layer == 0)
            {
                AssignCantDrawLayer(lineObject);
            }
        }

        Vector2 ClampToDrawArea(Vector2 worldPos)
        {
            if (!limitDrawingArea)
            {
                return worldPos;
            }

            if (drawAreaCenter == null)
            {
                if (!_warnedMissingDrawCenter)
                {
                    Debug.LogWarning("LineManager: limitDrawingArea is enabled but no drawAreaCenter is assigned.",
                        this);
                    _warnedMissingDrawCenter = true;
                }

                return worldPos;
            }

            Vector2 center = drawAreaCenter.position;
            switch (drawAreaShape)
            {
                case DrawAreaShape.Rectangle:
                    Vector2 halfSize = rectangleSize * 0.5f;
                    return new Vector2(
                        Mathf.Clamp(worldPos.x, center.x - halfSize.x, center.x + halfSize.x),
                        Mathf.Clamp(worldPos.y, center.y - halfSize.y, center.y + halfSize.y));
                case DrawAreaShape.Circle:
                default:
                    float maxDistance = Mathf.Max(0.01f, circleRadius);
                    Vector2 offset = worldPos - center;
                    if (offset.sqrMagnitude <= maxDistance * maxDistance)
                    {
                        return worldPos;
                    }

                    return center + offset.normalized * maxDistance;
            }
        }

        void HandleGameFinished()
        {
            _isGameFinished = true;
            isDrawing = false;
            _penPressed = false;
            if (currentLine != null)
            {
                Destroy(currentLine.gameObject);
                currentLine = null;
            }

            if (drawAreaRenderer != null)
            {
                drawAreaRenderer.enabled = true; // keep visible but ensure latest size
            }
        }

        void EnsureDrawAreaRenderer()
        {
            if (drawAreaRenderer == null)
            {
                drawAreaRenderer = GetComponent<LineRenderer>();
                if (drawAreaRenderer == null)
                {
                    drawAreaRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            if (_drawAreaMaterial == null)
            {
                Shader spriteShader = Shader.Find("Sprites/Default");
                _drawAreaMaterial = spriteShader != null
                    ? new Material(spriteShader)
                    : new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            }

            drawAreaRenderer.material = _drawAreaMaterial;
            drawAreaRenderer.useWorldSpace = true;
            drawAreaRenderer.loop = true;
            drawAreaRenderer.startWidth = drawAreaLineWidth;
            drawAreaRenderer.endWidth = drawAreaLineWidth;
            drawAreaRenderer.startColor = drawAreaLineColor;
            drawAreaRenderer.endColor = drawAreaLineColor;
            drawAreaRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            drawAreaRenderer.receiveShadows = false;
        }

        void UpdateDrawAreaVisualization()
        {
            if (!showDrawAreaVisual || !limitDrawingArea)
            {
                if (drawAreaRenderer != null)
                {
                    drawAreaRenderer.enabled = false;
                }

                return;
            }

            if (drawAreaCenter == null)
            {
                if (drawAreaRenderer != null)
                {
                    drawAreaRenderer.enabled = false;
                }

                return;
            }

            EnsureDrawAreaRenderer();
            drawAreaRenderer.enabled = true;
            drawAreaRenderer.startColor = drawAreaLineColor;
            drawAreaRenderer.endColor = drawAreaLineColor;
            drawAreaRenderer.startWidth = drawAreaLineWidth;
            drawAreaRenderer.endWidth = drawAreaLineWidth;

            Vector3 center = drawAreaCenter.position;
            if (drawAreaShape == DrawAreaShape.Rectangle)
            {
                Vector2 half = rectangleSize * 0.5f;
                Vector3[] points = new Vector3[5];
                points[0] = new Vector3(center.x - half.x, center.y - half.y, center.z);
                points[1] = new Vector3(center.x - half.x, center.y + half.y, center.z);
                points[2] = new Vector3(center.x + half.x, center.y + half.y, center.z);
                points[3] = new Vector3(center.x + half.x, center.y - half.y, center.z);
                points[4] = points[0];
                drawAreaRenderer.loop = false;
                drawAreaRenderer.positionCount = points.Length;
                drawAreaRenderer.SetPositions(points);
            }
            else
            {
                int segments = Mathf.Max(8, circleSegments);
                drawAreaRenderer.loop = true;
                drawAreaRenderer.positionCount = segments;
                float radius = Mathf.Max(0.01f, circleRadius);
                for (int i = 0; i < segments; i++)
                {
                    float t = (float)i / segments * Mathf.PI * 2f;
                    float x = Mathf.Cos(t) * radius;
                    float y = Mathf.Sin(t) * radius;
                    drawAreaRenderer.SetPosition(i, new Vector3(center.x + x, center.y + y, center.z));
                }
            }
        }

        void TryAddPoint(Vector2 worldPos)
        {
            if (currentLine == null) return;

            Vector2 lastPointWorld = currentLine.transform.TransformPoint(currentLine.GetLastPoint());
            float dist = Vector2.Distance(lastPointWorld, worldPos);

            if (dist < minDistance) return;
            var drawingConfigController = DrawingConfigController.Instance;
            float inkMultiplier =
                drawingConfigController != null ? drawingConfigController.currentSettings.fillMult : 1f;
            // 2. Ask Controller to consume ink from the Active Button

            float ink = (currentLine.LastSegmentLength * inkMultiplier);
            int inkCost;
            _inkBuffer += ink;
            if (_inkBuffer > 1f)
            {
                inkCost = Mathf.FloorToInt(_inkBuffer);
                _inkBuffer = 0f;
            }
            else
            {
                inkCost = 0;
            }

            if (drawingConfigController.TryConsumeInk(inkCost))
            {
                // Success: Button updated its UI, we update the line
                currentLine.AddWorldPoint(worldPos);
                currentLine.AddInkCost(inkCost);
                // Debug.Log("LineManager: Consumed " + inkCost + " ink for line segment. Total line length: " +
                //        currentLine.LineLength);
                if (drawEffect != null&& !drawEffect.isPlaying)
                {
                    drawEffect.transform.position = worldPos;
                    drawEffect.Play();
                }
                UpdateDrawEffectPos(worldPos);
     
            }
            else
            {
                // Fail: Not enough ink
                FinishLine(worldPos);
            }
        }

        void UpdateDrawEffectPos(Vector2 pos)
        {
            if (drawEffect != null&& drawEffect.isPlaying)
            {
                drawEffect.transform.position = pos;
            }
        }
        private bool IsPointerOverUI(int pointerId = -1)
        {
            if (EventSystem.current == null) return false;

            
            if (pointerId == -1)
            {
                return EventSystem.current.IsPointerOverGameObject();
            }
            
           
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }

    public enum DrawAreaShape
    {
        Circle,
        Rectangle
    }
}