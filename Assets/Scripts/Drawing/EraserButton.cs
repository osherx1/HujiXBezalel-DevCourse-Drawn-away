using UnityEngine;
using UnityEngine.UI;

namespace Drawing
{
    public class EraserButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Sprite eraserSprite;
        [SerializeField] private SpriteRenderer curserSpriteRenderer;
        [SerializeField] private CircleCollider2D cursorCollider;
        [SerializeField] private LineManager lineManager;
        
        private Sprite pencilSprite;
        private bool isEraserActive = false;
        

        private void OnEnable()
        {
            if (!button) return;
            button.onClick.AddListener(OnEraserButtonClicked);
        }

        private void OnDisable()
        {
            if (!button) return;
            button.onClick.RemoveListener(OnEraserButtonClicked);
        }

        private void OnEraserButtonClicked()
        {
            isEraserActive = !isEraserActive;

            if (isEraserActive)
            {
                EventManager.Instance.TriggerEraserActive();
                pencilSprite = curserSpriteRenderer.sprite;
                curserSpriteRenderer.sprite = eraserSprite;
                cursorCollider.enabled = true;
                lineManager.enabled = false;
            }
            else
            {
                EventManager.Instance.TriggerEraserInactive();
                curserSpriteRenderer.sprite = pencilSprite;
                cursorCollider.enabled = false;
                lineManager.enabled = true;
            }
        }
    }
}
