using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drawing.Utilities
{

    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 1.0f;
        [SerializeField] private float waitBeforeLoad = 0.5f;

        public void TransitionToScene(string sceneName)
        {
            StartCoroutine(FadeAndLoadRoutine(sceneName));
        }

        private IEnumerator FadeAndLoadRoutine(string sceneName)
        {
            yield return StartCoroutine(Fade(1f)); 

            yield return new WaitForSeconds(waitBeforeLoad);

            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator Fade(float targetAlpha)
        {
            float startAlpha = fadeCanvasGroup.alpha;
            float time = 0;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
                fadeCanvasGroup.alpha = newAlpha;
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
        }
    }
}