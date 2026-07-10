using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nemuri.UI
{
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader _instance;
        public static ScreenFader Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("Screen Fader");
                    _instance = go.AddComponent<ScreenFader>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Canvas _canvas;
        private Image _fadeImage;
        private float _currentAlpha = 0f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SetupUi();
        }

        private void SetupUi()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999; // Render on top of everything

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject imageGo = new GameObject("Fade Image");
            imageGo.transform.SetParent(transform, false);
            _fadeImage = imageGo.AddComponent<Image>();
            _fadeImage.color = new Color(0f, 0f, 0f, _currentAlpha);

            RectTransform rect = _fadeImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _fadeImage.raycastTarget = false;
        }

        public Coroutine FadeToBlack(float duration)
        {
            StopAllCoroutines();
            return StartCoroutine(FadeRoutine(1f, duration));
        }

        public Coroutine FadeToClear(float duration)
        {
            StopAllCoroutines();
            return StartCoroutine(FadeRoutine(0f, duration));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            float startAlpha = _currentAlpha;
            float elapsed = 0f;

            _fadeImage.raycastTarget = targetAlpha > 0.01f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                _fadeImage.color = new Color(0f, 0f, 0f, _currentAlpha);
                yield return null;
            }

            _currentAlpha = targetAlpha;
            _fadeImage.color = new Color(0f, 0f, 0f, _currentAlpha);
            _fadeImage.raycastTarget = targetAlpha > 0.01f;
        }
    }
}
