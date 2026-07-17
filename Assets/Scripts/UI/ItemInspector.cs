using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Nemuri.Inventory;
using Nemuri.Player;

namespace Nemuri.UI
{
    public class ItemInspector : MonoBehaviour
    {
        public static ItemInspector Instance { get; private set; }

        [Header("Animation Settings")]
        [SerializeField] private float _transitionDuration = 0.4f;
        [SerializeField] private float _spinCount = 2f;
        [SerializeField] private float _floatSpeed = 3f;
        [SerializeField] private float _floatAmplitude = 15f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _inspectSound;
        [SerializeField, Min(0f)] private float _soundVolume = 1f;

        private AudioSource _audioSource;
        private Canvas _canvas;
        private Image _itemDisplayImage;
        private Text _descriptionLabel;
        private GameObject _overlayPanel;

        private bool _isInspecting = false;
        private bool _isAnimating = false;
        private Coroutine _activeAnimationRoutine;
        private Vector3 _baseImagePosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            InitializeInspectUI();
        }

        private void Update()
        {
            if (Keyboard.current == null || _isAnimating) return;

            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                if (_isInspecting)
                {
                    CloseInspection();
                }
                else
                {
                    OpenInspection();
                }
            }
        }

        private void InitializeInspectUI()
        {
            GameObject canvasObj = GameObject.Find("Item Inspect Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Item Inspect Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 500;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                canvasObj.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasObj);
            }
            else
            {
                _canvas = canvasObj.GetComponent<Canvas>();
            }

            // create dark background overlay tint
            _overlayPanel = new GameObject("OverlayPanel");
            _overlayPanel.transform.SetParent(canvasObj.transform, false);
            Image bg = _overlayPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            RectTransform bgRect = _overlayPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // create item render display image
            GameObject imageObj = new GameObject("ItemDisplay");
            imageObj.transform.SetParent(_overlayPanel.transform, false);
            _itemDisplayImage = imageObj.AddComponent<Image>();

            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.55f);
            imageRect.anchorMax = new Vector2(0.5f, 0.55f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = new Vector2(300f, 300f);
            _baseImagePosition = imageRect.anchoredPosition;

            // create 2-line description label
            GameObject labelObj = new GameObject("DescriptionLabel");
            labelObj.transform.SetParent(_overlayPanel.transform, false);
            _descriptionLabel = labelObj.AddComponent<Text>();
            _descriptionLabel.alignment = TextAnchor.MiddleCenter;
            _descriptionLabel.color = Color.white;
            _descriptionLabel.fontSize = 28;
            _descriptionLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_descriptionLabel.font == null)
            {
                _descriptionLabel.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
            }

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.3f);
            labelRect.anchorMax = new Vector2(0.5f, 0.3f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(800f, 100f);

            // hide by default
            _overlayPanel.SetActive(false);
        }

        private void OpenInspection()
        {
            if (HotbarInventory.Instance == null) return;

            HotbarItem item = HotbarInventory.Instance.GetSelectedItem();
            if (item == null) return; // do not open if slot is empty

            // lock player movement and selection
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(false);
            }
            HotbarInventory.Instance.IsLocked = true;

            // play sound if available
            if (_inspectSound != null)
            {
                _audioSource.PlayOneShot(_inspectSound, _soundVolume);
            }

            // setup visuals
            if (item.icon != null)
            {
                _itemDisplayImage.sprite = item.icon;
                _itemDisplayImage.color = Color.white;
            }
            else
            {
                // fallback for no sprite icon
                _itemDisplayImage.sprite = null;
                _itemDisplayImage.color = new Color(0.9f, 0.9f, 0.9f, 0.4f);
            }

            _descriptionLabel.text = item.description;
            _descriptionLabel.color = new Color(1f, 1f, 1f, 0f); // fade in later

            _isInspecting = true;
            _overlayPanel.SetActive(true);

            if (_activeAnimationRoutine != null)
            {
                StopCoroutine(_activeAnimationRoutine);
            }
            _activeAnimationRoutine = StartCoroutine(OpenAnimationRoutine());
        }

        private void CloseInspection()
        {
            if (_activeAnimationRoutine != null)
            {
                StopCoroutine(_activeAnimationRoutine);
            }
            _activeAnimationRoutine = StartCoroutine(CloseAnimationRoutine());
        }

        private IEnumerator OpenAnimationRoutine()
        {
            _isAnimating = true;
            RectTransform imgRt = _itemDisplayImage.GetComponent<RectTransform>();
            imgRt.anchoredPosition = _baseImagePosition;

            float elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _transitionDuration);

                // scale up
                imgRt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);

                // Y-axis spin
                float spinAngle = progress * _spinCount * 360f;
                imgRt.localRotation = Quaternion.Euler(0f, spinAngle, 0f);

                yield return null;
            }

            imgRt.localScale = Vector3.one;
            imgRt.localRotation = Quaternion.identity;

            // fade in description text
            elapsed = 0f;
            float textFadeDuration = 0.2f;
            while (elapsed < textFadeDuration)
            {
                elapsed += Time.deltaTime;
                _descriptionLabel.color = new Color(1f, 1f, 1f, Mathf.Clamp01(elapsed / textFadeDuration));
                yield return null;
            }
            _descriptionLabel.color = Color.white;

            _isAnimating = false;

            // floating loop state while waiting for close input
            while (_isInspecting)
            {
                float floatOffset = Mathf.Sin(Time.time * _floatSpeed) * _floatAmplitude;
                imgRt.anchoredPosition = _baseImagePosition + new Vector3(0f, floatOffset, 0f);
                yield return null;
            }
        }

        private IEnumerator CloseAnimationRoutine()
        {
            _isAnimating = true;

            // fade out description text
            float elapsed = 0f;
            float textFadeDuration = 0.15f;
            Color textBaseColor = _descriptionLabel.color;
            while (elapsed < textFadeDuration)
            {
                elapsed += Time.deltaTime;
                _descriptionLabel.color = Color.Lerp(textBaseColor, Color.clear, Mathf.Clamp01(elapsed / textFadeDuration));
                yield return null;
            }
            _descriptionLabel.color = Color.clear;

            RectTransform imgRt = _itemDisplayImage.GetComponent<RectTransform>();
            Vector3 inspectEndPosition = imgRt.anchoredPosition;

            elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _transitionDuration);

                // scale down
                imgRt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, progress);

                // Y-axis reverse spin
                float spinAngle = (1f - progress) * _spinCount * 360f;
                imgRt.localRotation = Quaternion.Euler(0f, spinAngle, 0f);

                // return to base height
                imgRt.anchoredPosition = Vector3.Lerp(inspectEndPosition, _baseImagePosition, progress);

                yield return null;
            }

            imgRt.localScale = Vector3.zero;
            imgRt.localRotation = Quaternion.identity;
            imgRt.anchoredPosition = _baseImagePosition;

            _overlayPanel.SetActive(false);
            _isInspecting = false;
            _isAnimating = false;

            // restore movement and selection control
            HotbarInventory.Instance.IsLocked = false;
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(true);
            }
        }
    }
}
