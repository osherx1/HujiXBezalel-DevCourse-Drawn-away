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
        [SerializeField] private Sprite pencilSprite;
        [SerializeField] private CircleCollider2D cursorCollider;
        [SerializeField] private LineManager lineManager;
        
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
            EventManager.Instance.OnConfigButtonSelected += CancelEraser;
        }

        private void OnDisable()
        {
            if (!button) return;
            button.onClick.RemoveListener(OnEraserButtonClicked);
            EventManager.Instance.OnConfigButtonSelected -= CancelEraser;
        }

        private void OnEraserButtonClicked()
        {
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);

            EventManager.Instance.TriggerEraserActive();
            curserSpriteRenderer.sprite = eraserSprite;
            cursorCollider.enabled = true;
            lineManager.enabled = false;
        }
        
        private void CancelEraser(object sender)
        {
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);
            curserSpriteRenderer.sprite = pencilSprite;
            cursorCollider.enabled = false;
            lineManager.enabled = true;
        }
    }
}
