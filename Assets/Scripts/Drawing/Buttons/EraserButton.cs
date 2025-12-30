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
        [SerializeField] private Image selectedButtonImage;

        private Image _buttonImage;
        private Color normalColor;
        [SerializeField] private Color selectedColor = Color.green;
        private bool _isEraserActive;
        [SerializeField] private bool stayActiveOnDisable = true;


        private void Awake()
        {
            _buttonImage = GetComponent<Image>();
            normalColor = _buttonImage.color;
        }

        private void OnEnable()
        {
            if (button)
            {
                button.onClick.AddListener(OnEraserButtonClicked);
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnConfigButtonSelected += CancelEraser;
            }

            if (stayActiveOnDisable && DrawingConfigController.Instance != null)
            {
                bool temp = _isEraserActive;
                _isEraserActive = DrawingConfigController.Instance.IsEraserActive();
                if (_isEraserActive != temp)
                {
                    //Debug.Log("Eraser state change from " + _isEraserActive + " to " + temp);
                    if (_isEraserActive)
                    {
                        ActivateEraserMode();
                    }
                    else
                    {
                        CancelEraser(this);
                    }
                }
                
            }
        }

        private void OnDisable()
        {
            if (button)
            {
                button.onClick.RemoveListener(OnEraserButtonClicked);
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnConfigButtonSelected -= CancelEraser;
            }

            if (_isEraserActive && !stayActiveOnDisable)
            {
                Cursor.visible = false;
                _isEraserActive = false;
                EventManager.Instance?.TriggerEraserInactive();
            }
        }

        private void OnEraserButtonClicked()
        {
            if (_isEraserActive)
            {
                CancelEraser(this);
                return;
            }

            ActivateEraserMode();
        }

        private void CancelEraser(object sender)
        {
            if (!_isEraserActive) return;

            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);
            if (curserSpriteRenderer) curserSpriteRenderer.sprite = pencilSprite;
            if (cursorCollider) cursorCollider.enabled = false;
            if (lineManager) lineManager.enabled = true;
            RestoreCursorVisibility();
            _isEraserActive = false;
            SetButtonVisual(false);

            EventManager.Instance?.TriggerEraserInactive();
        }

        private void ActivateEraserMode()
        {
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);
            EventManager.Instance?.TriggerEraserActive();

            if (curserSpriteRenderer) curserSpriteRenderer.sprite = eraserSprite;
            if (cursorCollider) cursorCollider.enabled = true;
            if (lineManager) lineManager.enabled = false;

            _isEraserActive = true;
            SetButtonVisual(true);
        }

        private void RestoreCursorVisibility()
        {
            Cursor.visible = true;
        }

        private void SetButtonVisual(bool isSelected)
        {
            if (_buttonImage == null) return;
            _buttonImage.color = isSelected ? selectedColor : normalColor;
            if (selectedButtonImage != null)
            {
                selectedButtonImage.enabled = isSelected;
            }
        }

        // Add this new method to EraserButton.cs
        public void ForceStopEraser()
        {
            // We pass 'this' or 'null' just to satisfy the CancelEraser signature
            CancelEraser(this);
        }
    }
}