using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Game.Core.Tutorial
{
    public class TutorialTextRenderer : MonoBehaviour
    {
        [Header("Tutorial UI Settings")] [SerializeField]
        private float blurDuration = 0.5f;

        [SerializeField] private float imageFadeDuration = 0.5f;

        private Coroutine blurCoroutine;
        private Coroutine imageCoroutine;

        private void SetPanelAlpha(Image blurPanel, float alpha)
        {
            if (blurPanel != null)
            {
                var color = blurPanel.color;
                color.a = alpha;
                blurPanel.color = color;
            }
        }

        private void SetImagesAlpha(Image[] images, float alpha)
        {
            if (images == null) return;
            foreach (var img in images)
            {
                if (img == null) continue;
                var c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }

        public void ShowBlurAndImages(Image blurPanel, Image[] imagesToShow)
        {
            ShowBlurAndImages(blurPanel, imagesToShow, null);
        }

        public void ShowBlurAndImages(Image blurPanel, Image[] imagesToShow, System.Action onComplete)
        {
            if (blurCoroutine != null) StopCoroutine(blurCoroutine);
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            blurCoroutine = StartCoroutine(BlurInCoroutine(blurPanel, 1f, blurDuration,
                () =>
                {
                    imageCoroutine = StartCoroutine(ShowImagesCoroutine(imagesToShow, imageFadeDuration, onComplete));
                }));
        }

        public void HideBlurAndImages(Image blurPanel, Image[] imagesToHide)
        {
            HideBlurAndImages(blurPanel, imagesToHide, null);
        }

        public void HideBlurAndImages(Image blurPanel, Image[] imagesToHide, System.Action onComplete)
        {
            if (blurCoroutine != null) StopCoroutine(blurCoroutine);
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            StartCoroutine(BlurOutAndHideImagesCoroutine(blurPanel, imagesToHide, onComplete));
        }

        public void TransitionImages(Image blurPanelToHide, Image[] imagesToHide, Image blurPanelToShow, Image[] imagesToShow, System.Action onComplete = null)
        {
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            imageCoroutine = StartCoroutine(TransitionImagesCoroutine(blurPanelToHide, imagesToHide, blurPanelToShow, imagesToShow, onComplete));
        }

        private IEnumerator BlurInCoroutine(Image blurPanel, float targetAlpha, float duration,
            System.Action onComplete)
        {
            if (blurPanel == null) yield break;
            blurPanel.gameObject.SetActive(true);
            SetPanelAlpha(blurPanel, 0F);
            float startAlpha = blurPanel.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                SetPanelAlpha(blurPanel, lerpAlpha);
                yield return null;
            }

            SetPanelAlpha(blurPanel, targetAlpha);
            onComplete?.Invoke();
        }

        private IEnumerator ShowImagesCoroutine(Image[] images, float duration, System.Action onComplete = null)
        {
            if (images == null) yield break;
            foreach (var img in images)
            {
                if (img != null) img.gameObject.SetActive(true);
            }

            float startAlpha = 0f;
            float targetAlpha = 1f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                SetImagesAlpha(images, lerpAlpha);
                yield return null;
            }

            SetImagesAlpha(images, targetAlpha);
            onComplete?.Invoke();
        }

        private IEnumerator BlurOutAndHideImagesCoroutine(Image blurPanel, Image[] images,
            System.Action onComplete = null)
        {
            // Fade out images
            float startAlpha = 1f;
            float t = 0f;
            while (t < imageFadeDuration)
            {
                t += Time.deltaTime;
                float lerpAlpha = Mathf.Lerp(startAlpha, 0f, t / imageFadeDuration);
                SetImagesAlpha(images, lerpAlpha);
                yield return null;
            }

            SetImagesAlpha(images, 0f);
            if (images != null)
            {
                foreach (var img in images)
                {
                    if (img != null) img.gameObject.SetActive(false);
                }
            }

            // Fade out blur
            if (blurPanel != null)
            {
                float panelStartAlpha = blurPanel.color.a;
                t = 0f;
                while (t < blurDuration)
                {
                    t += Time.deltaTime;
                    float lerpAlpha = Mathf.Lerp(panelStartAlpha, 0f, t / blurDuration);
                    SetPanelAlpha(blurPanel, lerpAlpha);
                    yield return null;
                }

                SetPanelAlpha(blurPanel, 0f);
                blurPanel.gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        }

        private IEnumerator TransitionImagesCoroutine(Image blurPanelToHide, Image[] imagesToHide, Image blurPanelToShow, Image[] imagesToShow, System.Action onComplete)
        {
            // Hide current blur and images
            yield return StartCoroutine(BlurOutAndHideImagesCoroutine(blurPanelToHide, imagesToHide));
            // Show new blur and images
            yield return StartCoroutine(BlurInCoroutine(blurPanelToShow, 1f, blurDuration, () =>
            {
                imageCoroutine = StartCoroutine(ShowImagesCoroutine(imagesToShow, imageFadeDuration, onComplete));
            }));
        }
    }
}