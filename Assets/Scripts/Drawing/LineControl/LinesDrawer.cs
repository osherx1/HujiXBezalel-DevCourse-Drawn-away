using UnityEngine;
using UnityEngine.EventSystems;
namespace Drawing.LineControl
{
    
// Manages drawing of multiple Line instances using the New Input System via EventSystem pointer callbacks.
public class LinesDrawer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Setup")]
    public GameObject linePrefab;           // Prefab containing Line + required components
    public LayerMask cantDrawOverLayer;     // Layer(s) we cannot draw over

    [Header("Line Config")]
    public Gradient lineColor;
    public float linePointsMinDistance = 0.1f;
    public float lineWidth = 0.2f;

    private int cantDrawOverLayerIndex;
    private Line currentLine;
    private Camera cam;
    private bool isDrawing;

    void Awake()
    {
        cam = Camera.main;
        cantDrawOverLayerIndex = LayerMask.NameToLayer("CantDrawOver");
        if (cam == null)
        {
            Debug.LogWarning("LinesDrawer: Main Camera not found. Drawing will not work correctly.");
        }
    }

    // New Input System pointer events
    public void OnPointerDown(PointerEventData eventData)
    {
        BeginDraw();
        isDrawing = true;
        DrawAt(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;
        DrawAt(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
        EndDraw();
    }

    private void BeginDraw()
    {
        if (!linePrefab)
        {
            Debug.LogError("LinesDrawer: linePrefab not set.");
            return;
        }

        currentLine = Instantiate(linePrefab, transform).GetComponent<Line>();
        if (!currentLine)
        {
            Debug.LogError("LinesDrawer: Prefab does not contain a Line component.");
            return;
        }

        currentLine.UsePhysics(false); // start static while drawing
        currentLine.SetLineColor(lineColor);
        currentLine.SetPointsMinDistance(linePointsMinDistance);
        currentLine.SetLineWidth(lineWidth);
    }

    private void DrawAt(Vector2 screenPos)
    {
        if (currentLine == null) return;
        Vector2 world = cam.ScreenToWorldPoint(screenPos);
        // Use half the line width for a closer match to the visual thickness
        RaycastHit2D hit = Physics2D.CircleCast(world, lineWidth / 2f, Vector2.zero, 0f, cantDrawOverLayer);
        if (hit) EndDraw();
        else currentLine.AddWorldPoint(world);
    }

    private void EndDraw()
    {
        //TODO check with Osher and maybe need to change 
        var conf = DrawingConfigController.Instance.currentSettings;

        if (currentLine == null) return;

        if (currentLine.pointsCount < 2)
        {
            // Not enough points to form a usable line
            Destroy(currentLine.gameObject);
        }
        else
        {
            // Prevent future drawing over this line
            if (cantDrawOverLayerIndex >= 0)
            {
                currentLine.gameObject.layer = cantDrawOverLayerIndex;
            }
            // Build polygon + enable physics so line can interact / fall
            //was true before
            //currentLine.FinalizeLine(true);
            currentLine.FinalizeLine(conf);
        }

        currentLine = null;
    }
}

}
