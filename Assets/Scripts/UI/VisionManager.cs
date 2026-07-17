using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nemuri.Player;
using Nemuri.Inventory;
using Nemuri.Dialogue;

namespace Nemuri.UI
{
    public class VisionManager : MonoBehaviour
    {
        public static VisionManager Instance { get; private set; }

        [Header("Animation Settings")]
        [SerializeField] private float _transitionDuration = 0.5f;
        [SerializeField] private float _startOffsetY = 80f;

        private Canvas _canvas;
        private Image _visionImage;
        private GameObject _overlayPanel;

        private bool _isVisionActive = false;
        private readonly HashSet<string> _triggeredVisions = new HashSet<string>();
        private Coroutine _animationRoutine;
        private Vector3 _baseImagePosition;

        public bool IsVisionActive => _isVisionActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeVisionUI();
        }

        private void InitializeVisionUI()
        {
            GameObject canvasObj = GameObject.Find("Vision Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Vision Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 400; // right behind Dialogues but in front of Hotbars/Inspectors

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

            // create dark background backdrop tint
            _overlayPanel = new GameObject("VisionOverlayPanel");
            _overlayPanel.transform.SetParent(canvasObj.transform, false);
            Image bg = _overlayPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            RectTransform bgRect = _overlayPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // create vision center image (90% of screen size, almost fullscreen)
            GameObject imageObj = new GameObject("VisionImage");
            imageObj.transform.SetParent(_overlayPanel.transform, false);
            _visionImage = imageObj.AddComponent<Image>();

            RectTransform imageRect = _visionImage.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.05f, 0.05f); // 5% borders
            imageRect.anchorMax = new Vector2(0.95f, 0.95f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            _baseImagePosition = imageRect.anchoredPosition;

            // hide overlay by default
            _overlayPanel.SetActive(false);
        }

        public void PlayVision(string itemName, Sprite visionSprite, TextAsset dialogueJson)
        {
            if (_triggeredVisions.Contains(itemName)) return;
            if (dialogueJson == null)
            {
                Debug.LogWarning("[VisionManager] No dialogue JSON assigned for vision item: " + itemName);
                return;
            }

            _triggeredVisions.Add(itemName);
            _isVisionActive = true;

            // lock player movement and selection
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(false);
            }
            if (HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.IsLocked = true;
            }

            // setup sprite image or solid black placeholder
            if (visionSprite != null)
            {
                _visionImage.sprite = visionSprite;
                _visionImage.color = new Color(1f, 1f, 1f, 0f); // fade in
            }
            else
            {
                _visionImage.sprite = null;
                _visionImage.color = new Color(0f, 0f, 0f, 0f); // fade in black placeholder
            }

            _overlayPanel.SetActive(true);

            // trigger sliding and fading animation
            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
            }
            _animationRoutine = StartCoroutine(OpenVisionRoutine(dialogueJson));
        }

        private IEnumerator OpenVisionRoutine(TextAsset dialogueJson)
        {
            RectTransform imgRt = _visionImage.GetComponent<RectTransform>();
            imgRt.anchoredPosition = _baseImagePosition + new Vector3(0f, _startOffsetY, 0f);

            float elapsed = 0f;
            Color baseColor = _visionImage.sprite != null ? Color.white : Color.black;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _transitionDuration);

                // slide down
                imgRt.anchoredPosition = Vector3.Lerp(_baseImagePosition + new Vector3(0f, _startOffsetY, 0f), _baseImagePosition, progress);

                // fade in
                _visionImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, progress);

                yield return null;
            }

            imgRt.anchoredPosition = _baseImagePosition;
            _visionImage.color = baseColor;

            // load dialogue sequence
            DialogueSequence sequence = JsonUtility.FromJson<DialogueSequence>(dialogueJson.text);
            if (sequence != null && DialogueManager.Instance != null)
            {
                DialogueManager.OnConversationEnd += OnVisionDialogueEnd;
                DialogueManager.Instance.StartConversation(sequence.nodes);
            }
            else
            {
                Debug.LogError("[VisionManager] Failed to start narrative dialogue!");
                OnVisionDialogueEnd();
            }
        }

        private void OnVisionDialogueEnd()
        {
            DialogueManager.OnConversationEnd -= OnVisionDialogueEnd;

            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
            }
            _animationRoutine = StartCoroutine(CloseVisionRoutine());
        }

        private IEnumerator CloseVisionRoutine()
        {
            RectTransform imgRt = _visionImage.GetComponent<RectTransform>();
            Vector3 startPos = imgRt.anchoredPosition;
            Color startColor = _visionImage.color;

            float elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _transitionDuration);

                // fade out
                _visionImage.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);

                yield return null;
            }

            _visionImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
            _overlayPanel.SetActive(false);
            _isVisionActive = false;

            // restore player control and selection
            if (HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.IsLocked = false;
            }
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(true);
            }
        }
    }
}
