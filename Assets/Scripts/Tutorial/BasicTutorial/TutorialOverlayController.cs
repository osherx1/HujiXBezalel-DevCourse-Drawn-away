using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlayController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The fullscreen dark background sprite.")]
    [SerializeField] private Image darkVeil;
    [Tooltip("The image that shows the specific instruction sprite.")]
    [SerializeField] private Image instructionImage;
    [Tooltip("The arrow pointing at buttons.")]
    [SerializeField] private RectTransform arrowTransform;
    [Tooltip("The 'Press Enter' prompt.")]
    [SerializeField] private CanvasGroup pressEnterPrompt;

    [Header("Clone Container")]
    [Tooltip("An Empty GameObject (RectTransform) inside this Canvas to hold the cloned buttons.")]
    [SerializeField] private RectTransform cloneContainer;

    [Header("Settings")]
    [Tooltip("The camera that renders the World Space Toolbar. Defaults to Main Camera.")]
    [SerializeField] private Camera worldUiCamera; 
    [SerializeField] private float blinkSpeed = 2.0f;
    [SerializeField] private float veilFadeDuration = 1.0f;
    [Tooltip("Offset from the button center (X, Y in pixels).")]
    [SerializeField] private Vector2 arrowOffset = new Vector2(0, 80);

    private Coroutine _blinkCoroutine;
    private List<GameObject> _activeClones = new List<GameObject>();

    public void Initialize()
    {
        if (worldUiCamera == null) worldUiCamera = Camera.main;

        // Hide everything at start
        if (darkVeil) { darkVeil.gameObject.SetActive(false); darkVeil.color = new Color(1,1,1,0); }
        if (instructionImage) instructionImage.gameObject.SetActive(false);
        if (arrowTransform) arrowTransform.gameObject.SetActive(false);
        if (pressEnterPrompt) pressEnterPrompt.gameObject.SetActive(false);
        ClearHighlight();
    }

    public void ShowSimpleSprite(Sprite sprite)
    {
        if (instructionImage && sprite)
        {
            instructionImage.sprite = sprite;
            instructionImage.preserveAspect = true;
            instructionImage.gameObject.SetActive(true);
        }
    }

    public void HideSimpleSprite()
    {
        if (instructionImage) instructionImage.gameObject.SetActive(false);
    }

    public void ShowVeilAndPrompt(bool show)
    {
        if (darkVeil)
        {
            darkVeil.gameObject.SetActive(show);
            darkVeil.color = show ? Color.white : new Color(1,1,1,0);
        }
        
        if (pressEnterPrompt)
        {
            pressEnterPrompt.gameObject.SetActive(show);
            if (show) StartBlinking();
            else StopBlinking();
        }
    }

    public void HighlightButton(RectTransform targetButtonRoot, Sprite instructionSprite)
    {
        ClearHighlight();
        ShowSimpleSprite(instructionSprite);

        if (targetButtonRoot == null || worldUiCamera == null) return;

        // 1. RECURSIVE CLONE: Find all images in the button (Background, Icon, Border, etc.)
        Image[] childImages = targetButtonRoot.GetComponentsInChildren<Image>(true); // true = include inactive

        foreach (Image sourceImg in childImages)
        {
            // Skip the clone if the source is invisible
            if (sourceImg.sprite == null || sourceImg.color.a == 0) continue;

            // Create a new UI Object
            GameObject ghostObj = new GameObject("Ghost_" + sourceImg.name);
            ghostObj.transform.SetParent(cloneContainer, false);
            
            // Add Image and copy properties
            Image ghostImg = ghostObj.AddComponent<Image>();
            ghostImg.sprite = sourceImg.sprite;
            ghostImg.color = sourceImg.color;
            ghostImg.preserveAspect = sourceImg.preserveAspect;
            ghostImg.raycastTarget = false; // Just visual

            RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
            RectTransform sourceRect = sourceImg.rectTransform;

            // 2. MATH: Convert World Size/Pos to Screen Size/Pos
            // Position:
            Vector3 screenPos = worldUiCamera.WorldToScreenPoint(sourceRect.position);
            ghostRect.position = screenPos;

            // Size & Rotation:
            // We project the local rect corners to screen space to get the true width/height
            // This handles World Space scaling automatically.
            Vector3 worldCornerTopRight = sourceRect.TransformPoint(sourceRect.rect.max);
            Vector3 worldCornerBottomLeft = sourceRect.TransformPoint(sourceRect.rect.min);
            
            Vector2 screenCornerTR = worldUiCamera.WorldToScreenPoint(worldCornerTopRight);
            Vector2 screenCornerBL = worldUiCamera.WorldToScreenPoint(worldCornerBottomLeft);

            float width = Mathf.Abs(screenCornerTR.x - screenCornerBL.x);
            float height = Mathf.Abs(screenCornerTR.y - screenCornerBL.y);

            ghostRect.sizeDelta = new Vector2(width, height);
            
            // Copy rotation (Z-axis only for 2D UI)
            Vector3 screenRot = sourceRect.eulerAngles;
            ghostRect.rotation = Quaternion.Euler(0, 0, screenRot.z);

            _activeClones.Add(ghostObj);
        }

        // 3. Move Arrow
        if (arrowTransform)
        {
            arrowTransform.gameObject.SetActive(true);
            arrowTransform.pivot = new Vector2(0.5f, 0f); // Pivot at bottom-center of arrow sprite
            
            // Arrow position is based on the ROOT button position
            Vector3 rootScreenPos = worldUiCamera.WorldToScreenPoint(targetButtonRoot.position);
            arrowTransform.position = rootScreenPos + (Vector3)arrowOffset;
        }
    }

    public void ClearHighlight()
    {
        if (arrowTransform) arrowTransform.gameObject.SetActive(false);

        // Destroy all temporary ghosts
        foreach (var clone in _activeClones)
        {
            if (clone != null) Destroy(clone);
        }
        _activeClones.Clear();
    }

    public IEnumerator FadeOutVeil()
    {
        HideSimpleSprite();
        ClearHighlight();
        if (pressEnterPrompt) pressEnterPrompt.gameObject.SetActive(false);

        if (darkVeil)
        {
            Color start = darkVeil.color;
            float t = 0f;
            while(t < veilFadeDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(start.a, 0f, t / veilFadeDuration);
                darkVeil.color = new Color(start.r, start.g, start.b, a);
                yield return null;
            }
            darkVeil.gameObject.SetActive(false);
        }
    }

    private void StartBlinking()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlinking()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        if (pressEnterPrompt) pressEnterPrompt.alpha = 1f;
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);
            pressEnterPrompt.alpha = Mathf.Lerp(0.3f, 1.0f, t);
            yield return null;
        }
    }
}