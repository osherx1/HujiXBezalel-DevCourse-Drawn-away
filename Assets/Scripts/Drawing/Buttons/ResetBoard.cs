using Drawing.Data;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Drawing.Buttons
{
    public class ResetBoard : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject lineroot;
        [SerializeField] private string tagToIgnore;

        private void OnEnable()
        {
            if (!button) return;
            button.onClick.AddListener(OnResetButtonClicked);
        }

        private void OnDisable()
        {
            if (!button) return;
            button.onClick.RemoveListener(OnResetButtonClicked);
        }

        private void OnResetButtonClicked()
        {
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);
            foreach (Transform child in lineroot.transform)
            {
                if (!string.IsNullOrEmpty(tagToIgnore) && child.CompareTag(tagToIgnore))
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }
    }
}