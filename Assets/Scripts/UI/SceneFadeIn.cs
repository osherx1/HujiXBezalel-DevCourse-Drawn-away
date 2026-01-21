using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 2.0f;
    [SerializeField] private Image fadePanel; // Assign a full-screen white image here

    private void Start()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float t = 0f;
        Color c = fadePanel.color;
        c.a = 1f; // Start Opaque White
        fadePanel.color = c;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            c.a = Mathf.Lerp(1f, 0f, t);
            fadePanel.color = c;
            yield return null;
        }

        fadePanel.gameObject.SetActive(false);
    }
}