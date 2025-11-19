using System;
using Drawing.Data;
using Drawing.LineControl;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Drawing.Buttons
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
        private Image _buttonImage;
        private Color normalColor;
        [SerializeField] private Color selectedColor = Color.green;

        private void Awake()
        {
            _buttonImage = GetComponent<Image>();
            normalColor = _buttonImage.color;
        }

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
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);

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
