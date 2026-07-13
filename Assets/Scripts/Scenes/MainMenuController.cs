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

        [Header("Scene Transition")]
        [SerializeField] private string _nextSceneName = "Mindlit";
        [SerializeField, Min(0f)] private float _fadeDuration = 1.5f;

        [Header("Play Button")]
        [SerializeField] private string _playButtonLabel = "Play";
        [SerializeField] private Vector2 _playButtonAnchoredPosition = new Vector2(0f, -80f);
        [SerializeField] private Vector2 _playButtonSize = new Vector2(280f, 72f);
        [SerializeField] private Color _buttonNormalColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        [SerializeField] private Color _buttonHoverColor = new Color(0.55f, 0.42f, 0.12f, 0.95f);
        [SerializeField] private Color _buttonPressedColor = new Color(0.35f, 0.28f, 0.08f, 1f);
        [SerializeField] private Color _buttonTextColor = new Color(0.95f, 0.88f, 0.55f, 1f);
        [SerializeField, Min(8)] private int _buttonFontSize = 36;

        private VideoScenePlayer _videoPlayer;
        private Button _playButton;
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
            SetupPlayButton();
        }

        private void Start()
        {
            ScreenFader.Instance.SetAlphaImmediate(0f);
            _videoPlayer.Play();
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

        private void SetupPlayButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform existingButton = canvas.transform.Find("Play Button");
            if (existingButton != null)
            {
                _playButton = existingButton.GetComponent<Button>();
                _playButton.onClick.AddListener(OnPlayClicked);
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject buttonGo = new GameObject("Play Button");
            buttonGo.transform.SetParent(canvas.transform, false);

            Image buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = _buttonNormalColor;

            _playButton = buttonGo.AddComponent<Button>();
            ColorBlock colors = _playButton.colors;
            colors.normalColor = _buttonNormalColor;
            colors.highlightedColor = _buttonHoverColor;
            colors.pressedColor = _buttonPressedColor;
            colors.selectedColor = _buttonHoverColor;
            colors.fadeDuration = 0.12f;
            _playButton.colors = colors;
            _playButton.targetGraphic = buttonImage;
            _playButton.onClick.AddListener(OnPlayClicked);

            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = _playButtonAnchoredPosition;
            buttonRect.sizeDelta = _playButtonSize;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(buttonGo.transform, false);

            Text label = labelGo.AddComponent<Text>();
            label.text = _playButtonLabel;
            label.font = font;
            label.fontSize = _buttonFontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = _buttonTextColor;
            label.raycastTarget = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void OnPlayClicked()
        {
            if (_isTransitioning)
            {
                return;
            }

            StartCoroutine(TransitionToMindlitRoutine());
        }

        private IEnumerator TransitionToMindlitRoutine()
        {
            _isTransitioning = true;

            if (_playButton != null)
            {
                _playButton.interactable = false;
            }

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_menuVideo == null)
            {
                _menuVideo = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/Videos/Main Screen Menu.mov");
            }
        }
#endif
    }
}
