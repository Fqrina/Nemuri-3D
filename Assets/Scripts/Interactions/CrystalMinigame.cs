using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using Nemuri.Player;

namespace Nemuri.Interactions
{
    public class CrystalMinigame : MonoBehaviour
    {
        [Header("Minigame Settings")]
        [SerializeField, Range(0.1f, 0.5f)] private float _targetWidth = 0.25f;
        [SerializeField, Min(0.1f)] private float _targetSpeed = 1f;
        [SerializeField, Min(0.1f)] private float _climbSpeed = 1.2f;
        [SerializeField, Min(0.1f)] private float _fallSpeed = 0.9f;
        [SerializeField, Min(0.01f)] private float _progressGainSpeed = 0.25f;
        [SerializeField, Min(0.01f)] private float _progressLossSpeed = 0.15f;

        private Interactable _interactable;
        private CrystalPickup _crystalPickup;

        private bool _isPlaying;
        private float _playerPos;
        private float _targetPos;
        private float _lastTargetPos;
        private bool _wasMovingRight;
        private float _widthModifier;
        private float _widthModifierTarget;
        private float _progress;
        private float _sourceT;
        private float _destinationT;
        private bool _isWaiting;
        private float _waitTime;
        private float _movementTimer;
        private float _moveProgress;
        private float _moveSpeed;

        private GameObject _uiCanvasRoot;
        private Image _targetZoneImage;
        private Image _indicatorImage;
        private RectTransform _targetZoneRect;
        private RectTransform _playerIndicatorRect;
        private RectTransform _progressFillRect;
        
        private const float BarWidth = 560f;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _crystalPickup = GetComponent<CrystalPickup>();
        }

        private void Start()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.AddListener(StartMinigame);
                _interactable.HoldSeconds = 0f;
            }
        }

        private void OnDisable()
        {
            if (_isPlaying)
            {
                EndMinigame(false);
            }
        }

        private void StartMinigame()
        {
            if (_isPlaying) return;

            _isPlaying = true;
            _playerPos = 0.2f;
            _targetPos = 0f;
            _lastTargetPos = 0f;
            _wasMovingRight = false;
            _widthModifier = 0f;
            _widthModifierTarget = 0f;
            _progress = 0f;
            _sourceT = Random.Range(0f, 1f);
            _destinationT = Random.Range(0f, 1f);
            _isWaiting = false;
            _waitTime = 0f;
            _movementTimer = 0f;
            _moveProgress = 0f;
            _moveSpeed = Random.Range(1.5f, 2.5f);

            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(false);
            }

            if (_interactable != null)
            {
                _interactable.DismissInteraction();
                _interactable.enabled = false;
            }

            CreateUI();
        }

        private void Update()
        {
            if (!_isPlaying) return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                EndMinigame(false);
                return;
            }

            bool isQPressed = Keyboard.current != null && Keyboard.current.qKey.isPressed;
            if (isQPressed)
            {
                _playerPos = Mathf.MoveTowards(_playerPos, 1f, _climbSpeed * Time.deltaTime);
            }
            else
            {
                _playerPos = Mathf.MoveTowards(_playerPos, 0f, _fallSpeed * Time.deltaTime);
            }

            // update target width modifier based on movement direction change
            float currentVelocity = _targetPos - _lastTargetPos;
            if (Mathf.Abs(currentVelocity) > 0.0001f)
            {
                bool isMovingRight = currentVelocity > 0f;
                if (isMovingRight != _wasMovingRight)
                {
                    _wasMovingRight = isMovingRight;
                    _widthModifierTarget = Random.Range(-0.1f, 0.1f);
                }
            }
            _widthModifier = Mathf.MoveTowards(_widthModifier, _widthModifierTarget, Time.deltaTime * 0.8f);
            float activeWidth = _targetWidth * (1f + _widthModifier);

            // discrete smooth movement with 0.3-0.8 seconds pause at destination
            _lastTargetPos = _targetPos;
            float maxTargetPos = 1f - activeWidth;

            if (_isWaiting)
            {
                _movementTimer += Time.deltaTime;
                if (_movementTimer >= _waitTime)
                {
                    _isWaiting = false;
                    _sourceT = _targetPos / maxTargetPos;
                    
                    float newDest = Random.Range(0f, 1f);
                    while (Mathf.Abs(newDest - _sourceT) < 0.25f)
                    {
                        newDest = Random.Range(0f, 1f);
                    }
                    _destinationT = newDest;
                    _moveProgress = 0f;
                    _moveSpeed = Random.Range(1.5f, 2.5f);
                }
            }
            else
            {
                _moveProgress += Time.deltaTime * _moveSpeed;
                if (_moveProgress >= 1f)
                {
                    _moveProgress = 1f;
                    _isWaiting = true;
                    _movementTimer = 0f;
                    _waitTime = Random.Range(0.3f, 0.8f); // Wait 0.3 to 0.8 seconds at destination
                }

                float smoothT = Mathf.SmoothStep(0f, 1f, _moveProgress);
                float currentT = Mathf.Lerp(_sourceT, _destinationT, smoothT);
                _targetPos = currentT * maxTargetPos;
            }

            bool isInZone = _playerPos >= _targetPos && _playerPos <= _targetPos + activeWidth;
            if (isInZone)
            {
                _progress = Mathf.MoveTowards(_progress, 1f, _progressGainSpeed * Time.deltaTime);
            }
            else
            {
                _progress = Mathf.MoveTowards(_progress, 0f, _progressLossSpeed * Time.deltaTime);
            }

            // animate target zone color (purple-pink shifting gradient style)
            if (_targetZoneImage != null)
            {
                float colorT = Mathf.PingPong(Time.time * 1.2f, 1f);
                Color purple = new Color(0.63f, 0.12f, 0.94f, 0.8f);
                Color pink = new Color(1.0f, 0.08f, 0.58f, 0.8f);
                _targetZoneImage.color = Color.Lerp(purple, pink, colorT);
            }

            // visual feedback on the player indicator line based on success
            if (_indicatorImage != null)
            {
                if (isInZone)
                {
                    _indicatorImage.color = new Color(0.92f, 0.84f, 0.38f, 1f);
                    _playerIndicatorRect.sizeDelta = new Vector2(8f, 44f);
                }
                else
                {
                    _indicatorImage.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                    _playerIndicatorRect.sizeDelta = new Vector2(4f, 36f);
                }
            }

            UpdateUI();

            if (_progress >= 1f)
            {
                EndMinigame(true);
            }
        }

        private void EndMinigame(bool completed)
        {
            _isPlaying = false;

            if (_uiCanvasRoot != null)
            {
                Destroy(_uiCanvasRoot);
            }

            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(true);
            }

            if (completed)
            {
                if (_crystalPickup != null)
                {
                    _crystalPickup.Collect();
                }
            }
            else
            {
                if (_interactable != null)
                {
                    _interactable.enabled = true;
                }
            }
        }

        private void CreateUI()
        {
            _uiCanvasRoot = new GameObject("Crystal Minigame UI");
            Canvas canvas = _uiCanvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;

            CanvasScaler scaler = _uiCanvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _uiCanvasRoot.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(_uiCanvasRoot);

            // sleek outer border panel for premium glassmorphism outline
            GameObject borderPanel = new GameObject("Border Panel");
            borderPanel.transform.SetParent(_uiCanvasRoot.transform, false);
            Image borderImage = borderPanel.AddComponent<Image>();
            borderImage.color = new Color(0.35f, 0.35f, 0.35f, 0.5f);

            RectTransform borderRect = borderPanel.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(624f, 224f);

            GameObject panel = new GameObject("Main Panel");
            panel.transform.SetParent(borderPanel.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.06f, 0.06f, 0.94f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 220f);

            GameObject titleGo = new GameObject("Title Text");
            titleGo.transform.SetParent(panel.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "STABILIZING CRYSTAL";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.92f, 0.84f, 0.38f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = ResolveUiFont();

            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);
            titleRect.sizeDelta = new Vector2(0f, 40f);

            GameObject barBack = new GameObject("Bar Background");
            barBack.transform.SetParent(panel.transform, false);
            Image backImage = barBack.AddComponent<Image>();
            backImage.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            RectTransform backRect = barBack.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0.5f);
            backRect.anchorMax = new Vector2(0.5f, 0.5f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = new Vector2(0f, 10f);
            backRect.sizeDelta = new Vector2(BarWidth, 24f);

            GameObject targetZone = new GameObject("Target Zone");
            targetZone.transform.SetParent(barBack.transform, false);
            _targetZoneImage = targetZone.AddComponent<Image>();
            _targetZoneImage.color = new Color(0.63f, 0.12f, 0.94f, 0.8f);

            _targetZoneRect = targetZone.GetComponent<RectTransform>();
            _targetZoneRect.anchorMin = Vector2.zero;
            _targetZoneRect.anchorMax = Vector2.zero;
            _targetZoneRect.pivot = new Vector2(0f, 0.5f);
            _targetZoneRect.anchoredPosition = Vector2.zero;
            _targetZoneRect.sizeDelta = new Vector2(BarWidth * _targetWidth, 32f);

            GameObject indicator = new GameObject("Player Indicator");
            indicator.transform.SetParent(barBack.transform, false);
            _indicatorImage = indicator.AddComponent<Image>();
            _indicatorImage.color = new Color(0.95f, 0.77f, 0.06f, 1f);

            _playerIndicatorRect = indicator.GetComponent<RectTransform>();
            _playerIndicatorRect.anchorMin = Vector2.zero;
            _playerIndicatorRect.anchorMax = Vector2.zero;
            _playerIndicatorRect.pivot = new Vector2(0.5f, 0.5f);
            _playerIndicatorRect.anchoredPosition = Vector2.zero;
            _playerIndicatorRect.sizeDelta = new Vector2(6f, 40f);

            GameObject progressBack = new GameObject("Progress Background");
            progressBack.transform.SetParent(panel.transform, false);
            Image progressBackImage = progressBack.AddComponent<Image>();
            progressBackImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            RectTransform progBackRect = progressBack.GetComponent<RectTransform>();
            progBackRect.anchorMin = new Vector2(0.5f, 0.5f);
            progBackRect.anchorMax = new Vector2(0.5f, 0.5f);
            progBackRect.pivot = new Vector2(0.5f, 0.5f);
            progBackRect.anchoredPosition = new Vector2(0f, -25f);
            progBackRect.sizeDelta = new Vector2(BarWidth, 12f);

            GameObject progressFill = new GameObject("Progress Fill");
            progressFill.transform.SetParent(progressBack.transform, false);
            Image progressFillImage = progressFill.AddComponent<Image>();
            progressFillImage.color = new Color(0.16f, 0.5f, 0.73f, 1f);

            _progressFillRect = progressFill.GetComponent<RectTransform>();
            _progressFillRect.anchorMin = Vector2.zero;
            _progressFillRect.anchorMax = new Vector2(0f, 1f);
            _progressFillRect.pivot = new Vector2(0f, 0.5f);
            _progressFillRect.offsetMin = Vector2.zero;
            _progressFillRect.offsetMax = Vector2.zero;

            GameObject helpGo = new GameObject("Help Text");
            helpGo.transform.SetParent(panel.transform, false);
            Text helpText = helpGo.AddComponent<Text>();
            helpText.text = "Hold Q to move right. Release to fall left.\nKeep yellow line inside moving zone!\n[E] Abort interaction";
            helpText.fontSize = 13;
            helpText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            helpText.alignment = TextAnchor.MiddleCenter;
            helpText.font = ResolveUiFont();
            helpText.lineSpacing = 1.1f;

            RectTransform helpRect = helpGo.GetComponent<RectTransform>();
            helpRect.anchorMin = new Vector2(0f, 0f);
            helpRect.anchorMax = new Vector2(1f, 0f);
            helpRect.pivot = new Vector2(0.5f, 0f);
            helpRect.anchoredPosition = new Vector2(0f, 12f);
            helpRect.sizeDelta = new Vector2(0f, 54f);
        }

        private void UpdateUI()
        {
            if (_targetZoneRect == null || _playerIndicatorRect == null || _progressFillRect == null) return;

            float activeWidth = _targetWidth * (1f + _widthModifier);
            _targetZoneRect.sizeDelta = new Vector2(BarWidth * activeWidth, 32f);

            float targetPixelPos = _targetPos * BarWidth;
            _targetZoneRect.anchoredPosition = new Vector2(targetPixelPos, 0f);

            float playerPixelPos = _playerPos * BarWidth;
            _playerIndicatorRect.anchoredPosition = new Vector2(playerPixelPos, 0f);

            _progressFillRect.anchorMax = new Vector2(_progress, 1f);
        }

        private Font ResolveUiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            return font;
        }
    }
}
