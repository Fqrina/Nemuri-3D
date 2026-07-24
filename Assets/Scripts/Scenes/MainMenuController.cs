using System.Collections;
using Nemuri.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nemuri.Scenes
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Video")]
        [SerializeField] private VideoClip _menuVideo;

        [Header("Music")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.5f;

        [Header("Scene Transition")]
        [SerializeField] private string _nextSceneName = "Mindlit";
        [SerializeField, Min(0f)] private float _fadeDuration = 1.5f;

        [Header("Title Image (MainScreen1)")]
        [SerializeField] private Vector2 _titleAnchoredPosition = new Vector2(0f, 300f);
        [SerializeField] private Vector2 _titleSize = new Vector2(800f, 200f);

        [Header("Play Button Image (MainScreen2)")]
        [SerializeField] private Vector2 _playButtonAnchoredPosition = new Vector2(0f, 60f);
        [SerializeField] private Vector2 _playButtonSize = new Vector2(500f, 120f);
        [SerializeField, Min(1f)] private float _buttonHoverScale = 1.08f;

        [Header("Guide Button Image (MainScreen3)")]
        [SerializeField] private Vector2 _guideButtonAnchoredPosition = new Vector2(0f, -80f);
        [SerializeField] private Vector2 _guideButtonSize = new Vector2(500f, 120f);

        [Header("Guide Panel")]
        [SerializeField, TextArea(3, 20)] private string _guideText = "";
        [SerializeField] private Vector2 _guidePanelAnchoredPosition = Vector2.zero;
        [SerializeField] private Vector2 _guidePanelSize = new Vector2(900f, 600f);
        [SerializeField, Min(8)] private int _guideTextFontSize = 28;
        [SerializeField] private Color _guidePanelColor = new Color(0f, 0f, 0f, 0.7f);

        private const string TitleResourceName = "MainScreen1";
        private const string PlayButtonResourceName = "MainScreen2";
        private const string GuideButtonResourceName = "MainScreen3";

        private VideoScenePlayer _videoPlayer;
        private AudioSource _musicSource;
        private Button _playButton;
        private Button _guideButton;
        private GameObject _guideOverlay;
        private RectTransform _titleRect;
        private RectTransform _playButtonRect;
        private RectTransform _guideButtonRect;
        private RectTransform _guidePanelRect;
        private Text _guidePanelText;
        private bool _isTransitioning;

        private void Awake()
        {
            EnsureEventSystem();
            _videoPlayer = GetComponent<VideoScenePlayer>();
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoScenePlayer>();
            }

            _videoPlayer.Initialize(_menuVideo, loop: true);
            SetupMenuUi();
            SetupMusic();
        }

        private IEnumerator Start()
        {
            ScreenFader.Instance.SetAlphaImmediate(0f);
            yield return _videoPlayer.PrepareRoutine();
            _videoPlayer.Play();
            PlayMenuMusic();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        private void SetupMenuUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform menuRoot = canvas.transform.Find("Menu UI");
            if (menuRoot == null)
            {
                GameObject menuRootGo = new GameObject("Menu UI");
                menuRootGo.transform.SetParent(canvas.transform, false);
                menuRoot = menuRootGo.transform;
            }

            _titleRect = CreateOrUpdateImageButton(
                menuRoot,
                "Title Image",
                TitleResourceName,
                _titleAnchoredPosition,
                _titleSize,
                out _,
                interactable: false);

            _playButtonRect = CreateOrUpdateImageButton(
                menuRoot,
                "Play Button",
                PlayButtonResourceName,
                _playButtonAnchoredPosition,
                _playButtonSize,
                out _playButton,
                interactable: true);
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _playButton.onClick.AddListener(OnPlayClicked);
            AddHoverScaler(_playButtonRect.gameObject, _buttonHoverScale);

            _guideButtonRect = CreateOrUpdateImageButton(
                menuRoot,
                "Guide Button",
                GuideButtonResourceName,
                _guideButtonAnchoredPosition,
                _guideButtonSize,
                out _guideButton,
                interactable: true);
            _guideButton.onClick.RemoveListener(OnGuideClicked);
            _guideButton.onClick.AddListener(OnGuideClicked);
            AddHoverScaler(_guideButtonRect.gameObject, _buttonHoverScale);

            SetupGuideOverlay(canvas.transform);
            ApplyUILayout();
        }

        private void SetupGuideOverlay(Transform canvasTransform)
        {
            Transform existingOverlay = canvasTransform.Find("Guide Overlay");
            if (existingOverlay != null)
            {
                _guideOverlay = existingOverlay.gameObject;
                _guidePanelRect = existingOverlay.Find("Guide Panel") as RectTransform;
                Transform textTransform = existingOverlay.Find("Guide Panel/Scroll View/Viewport/Content/Text");
                if (textTransform != null)
                {
                    _guidePanelText = textTransform.GetComponent<Text>();
                }

                Button closeButton = existingOverlay.GetComponent<Button>();
                if (closeButton != null)
                {
                    closeButton.onClick.RemoveListener(CloseGuideOverlay);
                    closeButton.onClick.AddListener(CloseGuideOverlay);
                }

                _guideOverlay.SetActive(false);
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _guideOverlay = new GameObject("Guide Overlay");
            _guideOverlay.transform.SetParent(canvasTransform, false);

            RectTransform overlayRect = _guideOverlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = _guideOverlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0f);
            overlayImage.raycastTarget = true;

            Button overlayButton = _guideOverlay.AddComponent<Button>();
            ColorBlock overlayColors = overlayButton.colors;
            overlayColors.normalColor = Color.clear;
            overlayColors.highlightedColor = Color.clear;
            overlayColors.pressedColor = Color.clear;
            overlayColors.selectedColor = Color.clear;
            overlayButton.colors = overlayColors;
            overlayButton.targetGraphic = overlayImage;
            overlayButton.onClick.AddListener(CloseGuideOverlay);

            GameObject panelGo = new GameObject("Guide Panel");
            panelGo.transform.SetParent(_guideOverlay.transform, false);
            _guidePanelRect = panelGo.AddComponent<RectTransform>();

            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = _guidePanelColor;
            panelImage.raycastTarget = true;

            GameObject scrollGo = new GameObject("Scroll View");
            scrollGo.transform.SetParent(panelGo.transform, false);

            RectTransform scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(24f, 24f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -24f);

            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            GameObject viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);

            RectTransform viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.clear;

            Mask viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);

            RectTransform contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            ContentSizeFitter contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(contentGo.transform, false);

            _guidePanelText = textGo.AddComponent<Text>();
            _guidePanelText.font = font;
            _guidePanelText.fontSize = _guideTextFontSize;
            _guidePanelText.alignment = TextAnchor.UpperLeft;
            _guidePanelText.color = Color.white;
            _guidePanelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _guidePanelText.verticalOverflow = VerticalWrapMode.Overflow;
            _guidePanelText.raycastTarget = false;

            RectTransform textRect = _guidePanelText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(0f, 0f);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            _guideOverlay.SetActive(false);
        }

        private RectTransform CreateOrUpdateImageButton(
            Transform parent,
            string objectName,
            string resourceName,
            Vector2 anchoredPosition,
            Vector2 size,
            out Button button,
            bool interactable)
        {
            Transform existing = parent.Find(objectName);
            GameObject buttonGo;
            Image image;
            button = null;

            if (existing != null)
            {
                buttonGo = existing.gameObject;
                image = buttonGo.GetComponent<Image>();
                button = buttonGo.GetComponent<Button>();
            }
            else
            {
                buttonGo = new GameObject(objectName);
                buttonGo.transform.SetParent(parent, false);

                image = buttonGo.AddComponent<Image>();
                button = buttonGo.AddComponent<Button>();

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                colors.fadeDuration = 0f;
                button.colors = colors;
                button.transition = Selectable.Transition.None;
            }

            Sprite sprite = LoadResourceSprite(resourceName);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }

            image.raycastTarget = interactable;
            button.targetGraphic = image;
            button.interactable = interactable;

            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            ApplyRect(rect, anchoredPosition, size);
            return rect;
        }

        private static void AddHoverScaler(GameObject target, float hoverScale)
        {
            ButtonHoverScaler scaler = target.GetComponent<ButtonHoverScaler>();
            if (scaler == null)
            {
                scaler = target.AddComponent<ButtonHoverScaler>();
            }

            scaler.Initialize(hoverScale);
        }

        private static Sprite LoadResourceSprite(string resourceName)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourceName);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static void ApplyRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void ApplyUILayout()
        {
            ApplyRect(_titleRect, _titleAnchoredPosition, _titleSize);
            ApplyRect(_playButtonRect, _playButtonAnchoredPosition, _playButtonSize);
            ApplyRect(_guideButtonRect, _guideButtonAnchoredPosition, _guideButtonSize);

            if (_guidePanelRect != null)
            {
                ApplyRect(_guidePanelRect, _guidePanelAnchoredPosition, _guidePanelSize);
            }

            if (_guidePanelText != null)
            {
                _guidePanelText.text = _guideText;
                _guidePanelText.fontSize = _guideTextFontSize;
            }

            if (_guidePanelRect != null)
            {
                Image panelImage = _guidePanelRect.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = _guidePanelColor;
                }
            }
        }

        private void SetupMusic()
        {
            _musicSource = GetComponent<AudioSource>();
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
            }

            _musicSource.clip = _menuMusic;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = _musicVolume;
        }

        private void PlayMenuMusic()
        {
            if (_musicSource == null || _menuMusic == null)
            {
                return;
            }

            _musicSource.volume = _musicVolume;
            if (!_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }

        private void PlayClickSound()
        {
            AudioClip clickClip = Resources.Load<AudioClip>("ButtonClick");
            if (clickClip != null)
            {
                AudioSource.PlayClipAtPoint(clickClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }
        }

        private void OnPlayClicked()
        {
            if (_isTransitioning)
            {
                return;
            }

            PlayClickSound();
            StartCoroutine(TransitionToMindlitRoutine());
        }

        private void OnGuideClicked()
        {
            if (_guideOverlay == null)
            {
                return;
            }

            PlayClickSound();
            _guideOverlay.SetActive(!_guideOverlay.activeSelf);
        }

        private void CloseGuideOverlay()
        {
            PlayClickSound();
            if (_guideOverlay != null)
            {
                _guideOverlay.SetActive(false);
            }
        }

        private IEnumerator TransitionToMindlitRoutine()
        {
            _isTransitioning = true;

            if (_playButton != null)
            {
                _playButton.interactable = false;
            }

            if (_guideButton != null)
            {
                _guideButton.interactable = false;
            }

            CloseGuideOverlay();

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }

            if (_musicSource != null)
            {
                _musicSource.Stop();
            }

            SceneTransitionState.FadeInOnLoad = true;
            SceneTransitionState.FadeInDuration = _fadeDuration;

            yield return ScreenFader.Instance.FadeToBlack(_fadeDuration);

            if (string.IsNullOrWhiteSpace(_nextSceneName))
            {
                Debug.LogError("[MainMenuController] Next scene name is not assigned.", this);
                yield break;
            }

            SceneManager.LoadScene(_nextSceneName);
        }

        private sealed class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private RectTransform _rectTransform;
            private Vector3 _baseScale = Vector3.one;
            private float _hoverScale = 1.08f;
            private Coroutine _scaleRoutine;

            public void Initialize(float hoverScale)
            {
                _rectTransform = transform as RectTransform;
                _baseScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
                _hoverScale = hoverScale;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                AnimateToScale(_baseScale * _hoverScale);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                AnimateToScale(_baseScale);
            }

            private void AnimateToScale(Vector3 targetScale)
            {
                if (_rectTransform == null)
                {
                    return;
                }

                if (_scaleRoutine != null)
                {
                    StopCoroutine(_scaleRoutine);
                }

                _scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
            }

            private IEnumerator ScaleRoutine(Vector3 targetScale)
            {
                Vector3 startScale = _rectTransform.localScale;
                float elapsed = 0f;
                const float duration = 0.12f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                    yield return null;
                }

                _rectTransform.localScale = targetScale;
                _scaleRoutine = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_menuVideo == null)
            {
                _menuVideo = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/Videos/Main Screen Menu.mov");
            }

            if (_menuMusic == null)
            {
                _menuMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Sounds/MainMenuMusic.mp3");
            }

            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume;
            }

            if (Application.isPlaying)
            {
                ApplyUILayout();
            }
        }
#endif
    }
}
