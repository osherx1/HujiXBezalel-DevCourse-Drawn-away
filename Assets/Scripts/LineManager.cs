using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple input manager to draw physics-enabled lines using the Line component.
/// Supports mouse (editor/standalone) and touch (mobile).
/// Drop this on an empty GameObject (e.g., "LineManager") and set materials in the inspector.
/// </summary>
public class LineManager : MonoBehaviour
{
    [Header("Appearance")]
    public Material lineMaterial;
    public float lineWidth = 0.2f;
    public float minDistance = 0.05f;

    [Header("Physics")]
    public PhysicsMaterial2D physicsMaterial2D;
    public bool usePolygonCollider = true;

    [Header("Optional Prefab")]
    public GameObject linePrefab; // optional prefab with Line component already
    [Header("World Parenting")]
    public Transform linesRoot; // parent for lines (null = world root, recommended to keep lines out of Canvas)

    private Line currentLine;
    private bool isDrawing;

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Camera.main == null) return;
        var mouse = Mouse.current;
        var touch = Touchscreen.current;

        // Mouse
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 wp = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
                StartLine(wp);
                isDrawing = true;
            }
            if (mouse.leftButton.isPressed && currentLine != null)
            {
                Vector2 wp = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
                currentLine.AddWorldPoint(wp);
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                FinishLine();
            }
        }

        // Touch (primary)
        if (touch != null && touch.touches.Count > 0)
        {
            var primary = touch.touches[0];
            if (primary.press.wasPressedThisFrame)
            {
                Vector2 wp = Camera.main.ScreenToWorldPoint(primary.position.ReadValue());
                StartLine(wp);
                isDrawing = true;
            }
            if (primary.press.isPressed && currentLine != null)
            {
                Vector2 wp = Camera.main.ScreenToWorldPoint(primary.position.ReadValue());
                currentLine.AddWorldPoint(wp);
            }
            if (primary.press.wasReleasedThisFrame)
            {
                FinishLine();
            }
        }
#endif
    }

    void StartLine(Vector2 worldPos)
    {
        GameObject go;
        if (linePrefab != null)
        {
            go = linesRoot ? Instantiate(linePrefab, linesRoot) : Instantiate(linePrefab);
        }
        else
        {
            go = new GameObject("Line");
            if (linesRoot) go.transform.SetParent(linesRoot, false);
        }

        Line ln = go.GetComponent<Line>();
        if (ln == null) ln = go.AddComponent<Line>();

        // Initialize so Awake-created components get proper settings
    ln.Initialize(lineMaterial, lineWidth, minDistance, physicsMaterial2D, usePolygonCollider);

        currentLine = ln;
        currentLine.AddWorldPoint(worldPos);
    }

    void FinishLine()
    {
        isDrawing = false;
        if (currentLine == null) return;
        // If too short, discard
        if (currentLine.pointsCount < 2)
        {
            Destroy(currentLine.gameObject);
        }
        else
        {
            // Build a solid polygon (optional) and activate physics so it will fall/interact in world space
            currentLine.FinalizeLine(true);
        }
        currentLine = null;
    }
}
