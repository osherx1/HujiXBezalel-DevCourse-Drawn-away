using System.Collections;
using UnityEngine;

namespace Utilities.Camera
{
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }
        UnityEngine.Camera mainCamera;
        private Vector3 _originalPos;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            Instance = this;
            mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null) _originalPos = mainCamera.transform.localPosition;
        }

        public void Shake(float duration, float magnitude)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                
            }

            if (mainCamera != null)
            {
                _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
            }
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                if (mainCamera != null)
                {
                    mainCamera.transform.localPosition = _originalPos + new Vector3(x, y, 0f);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (mainCamera != null)
            {
                mainCamera.transform.localPosition = _originalPos;
            }

            _shakeCoroutine = null;
        }
    }
}