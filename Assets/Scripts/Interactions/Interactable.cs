using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using Nemuri.Dialogue;

namespace Nemuri.Interactions
{
    public class Interactable : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private const string InteractActionName = "Interact";

        private static InteractionPrompt _prompt;

        [Header("Interaction Settings")]
        [SerializeField, Min(0.1f)] private float _interactionRange = 6f;
        [SerializeField, Min(0f)] private float _holdSeconds = 3f;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private string _promptText = "Hold E to interact";

        public UnityEvent OnInteract = new UnityEvent();

        [Header("Interaction Prompt UI Configuration")]
        [SerializeField] private Vector2 _uiImageSize = new Vector2(750f, 275.13f);
        [SerializeField] private Vector2 _uiImagePosition = new Vector2(0f, 0f);
        [SerializeField] private int _uiFontSize = 64;
        [SerializeField] private float _uiFontYOffset = -20f;

        private Transform _player;
        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private float _holdTimer;
        private bool _waitingForRelease;

        private Bounds _interactionBounds;
        private bool _hasInteractionBounds;

        private string _overrideText = null;
        private float _overrideTimer = 0f;

        public void SetOverridePromptText(string text, float duration)
        {
            _overrideText = text;
            _overrideTimer = duration;
        }

        public string PromptText
        {
            get => _promptText;
            set => _promptText = value;
        }

        public float InteractionRange
        {
            get => _interactionRange;
            set => _interactionRange = Mathf.Max(0.1f, value);
        }
        public float HoldSeconds
        {
            get => _holdSeconds;
            set => _holdSeconds = Mathf.Max(0f, value);
        }
        public Transform InteractionPoint => _interactionPoint;

        private void Awake()
        {
            CacheInteractionBounds();
        }

        private void OnEnable()
        {
            FindPlayer();
        }

        private void OnDisable()
        {
            if (_prompt != null && _prompt.Owner == this)
            {
                _prompt.Hide(this);
            }
        }

        private void Update()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsConversationActive)
            {
                HidePromptAndReset();
                return;
            }
            if (_player == null)
            {
                FindPlayer();
                if (_player == null)
                {
                    HidePromptAndReset();
                    return;
                }
            }

            if (!IsPlayerInRange())
            {
                HidePromptAndReset();
                return;
            }

            if (_prompt == null)
            {
                _prompt = InteractionPrompt.Create();
            }

            if (!_prompt.CanShow(this, _player.position))
            {
                _holdTimer = 0f;
                _waitingForRelease = false;
                return;
            }

            bool isPressed = IsInteractPressed();
            if (!isPressed)
            {
                _waitingForRelease = false;
                _holdTimer = 0f;
            }
            else if (!_waitingForRelease)
            {
                if (_holdSeconds <= 0f)
                {
                    OnInteract?.Invoke();
                    _waitingForRelease = true;
                    if (!enabled) return;
                }
                else
                {
                    _holdTimer += Time.deltaTime;
                    if (_holdTimer >= _holdSeconds)
                    {
                        OnInteract?.Invoke();
                        _holdTimer = 0f;
                        _waitingForRelease = true;
                        if (!enabled) return;
                    }
                }
            }

            ShowPrompt();
        }

        private void ShowPrompt()
        {
            float progress = _holdSeconds > 0f ? Mathf.Clamp01(_holdTimer / _holdSeconds) : 0f;
            if (_overrideTimer > 0f && !string.IsNullOrEmpty(_overrideText))
            {
                _overrideTimer -= Time.deltaTime;
                _prompt.Show(this, _overrideText, 0f, _uiImageSize, _uiImagePosition, _uiFontSize, _uiFontYOffset);
            }
            else
            {
                _overrideText = null;
                _prompt.Show(this, _promptText, progress, _uiImageSize, _uiImagePosition, _uiFontSize, _uiFontYOffset);
            }
        }

        private void HidePromptAndReset()
        {
            _holdTimer = 0f;
            _waitingForRelease = false;

            if (_prompt != null && _prompt.Owner == this)
            {
                _prompt.Hide(this);
            }
        }

        public void DisplayInteraction(string promptText, float normalizedProgress)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsConversationActive)
            {
                DismissInteraction();
                return;
            }

            if (_prompt == null)
            {
                _prompt = InteractionPrompt.Create();
            }
            _prompt.Show(this, promptText, Mathf.Clamp01(normalizedProgress), _uiImageSize, _uiImagePosition, _uiFontSize, _uiFontYOffset);
        }

        public void DismissInteraction()
        {
            if (_prompt != null && _prompt.Owner == this)
            {
                _prompt.Hide(this);
            }
        }

        public static void ForceHidePrompt()
        {
            if (_prompt != null)
            {
                _prompt.ForceHide();
            }
            GameObject existing = GameObject.Find("Interaction Prompt");
            if (existing != null)
            {
                existing.SetActive(false);
            }
        }

        public bool IsPlayerInRange()
        {
            if (_player == null) return false;
            return Vector3.Distance(GetClosestInteractionPoint(_player.position), _player.position) <= _interactionRange;
        }

        private bool IsInteractPressed()
        {
            if (_interactAction != null)
            {
                return _interactAction.IsPressed();
            }

            return Keyboard.current != null && Keyboard.current.eKey.isPressed;
        }

        private void FindPlayer()
        {
            GameObject playerObject = FindPlayerObject();
            if (playerObject == null)
            {
                return;
            }

            _player = playerObject.transform;
            _playerInput = playerObject.GetComponent<PlayerInput>();
            _interactAction = _playerInput != null ? _playerInput.actions.FindAction(InteractActionName) : null;
        }

        private void CacheInteractionBounds()
        {
            _hasInteractionBounds = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer doorRenderer in renderers)
            {
                if (!doorRenderer.enabled)
                {
                    continue;
                }

                if (!_hasInteractionBounds)
                {
                    _interactionBounds = doorRenderer.bounds;
                    _hasInteractionBounds = true;
                }
                else
                {
                    _interactionBounds.Encapsulate(doorRenderer.bounds);
                }
            }

            if (_hasInteractionBounds)
            {
                return;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();

            foreach (Collider doorCollider in colliders)
            {
                if (!doorCollider.enabled)
                {
                    continue;
                }

                if (!_hasInteractionBounds)
                {
                    _interactionBounds = doorCollider.bounds;
                    _hasInteractionBounds = true;
                }
                else
                {
                    _interactionBounds.Encapsulate(doorCollider.bounds);
                }
            }
        }

        public Vector3 GetClosestInteractionPoint(Vector3 targetPosition)
        {
            if (_interactionPoint != null)
            {
                return _interactionPoint.position;
            }

            if (_hasInteractionBounds)
            {
                return _interactionBounds.ClosestPoint(targetPosition);
            }

            return transform.position;
        }

        private GameObject FindPlayerObject()
        {
            try
            {
                return GameObject.FindWithTag(PlayerTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[Interactable] Tag '{PlayerTag}' is not defined in the project.", this);
                return null;
            }
        }

        private sealed class InteractionPrompt
        {
            private readonly GameObject _root;
            private readonly Text _label;
            private readonly RectTransform _progressFillRect;
            private readonly Image _panelImage;
            private readonly RectTransform _barBackRect;

            public Interactable Owner { get; private set; }

            private InteractionPrompt(GameObject root, Text label, RectTransform progressFillRect, Image panelImage, RectTransform barBackRect)
            {
                _root = root;
                _label = label;
                _progressFillRect = progressFillRect;
                _panelImage = panelImage;
                _barBackRect = barBackRect;
                _root.SetActive(false);
            }

            public static InteractionPrompt Create()
            {
                GameObject existing = GameObject.Find("Interaction Prompt");
                if (existing != null)
                {
                    Object.Destroy(existing);
                }

                GameObject canvasObject = new GameObject("Interaction Prompt");
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                canvasObject.AddComponent<GraphicRaycaster>();
                Object.DontDestroyOnLoad(canvasObject);

                GameObject panel = new GameObject("Prompt Panel");
                panel.transform.SetParent(canvasObject.transform, false);
                Image panelImage = panel.AddComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.72f);

                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.12f);
                panelRect.anchorMax = new Vector2(0.5f, 0.12f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(750f, 275.13f);

                GameObject labelObject = new GameObject("Prompt Text");
                labelObject.transform.SetParent(panel.transform, false);
                Text label = labelObject.AddComponent<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                
                label.font = Resources.Load<Font>("Spinnenkop DEMO");
                if (label.font == null)
                {
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                label.fontSize = 64; // enlarged font size for large bubble

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0f); // fill completely
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = new Vector2(16f, 0f);
                labelRect.offsetMax = new Vector2(-16f, 0f);

                GameObject barBack = new GameObject("Hold Progress Back");
                barBack.transform.SetParent(panel.transform, false);
                Image backImage = barBack.AddComponent<Image>();
                backImage.color = new Color(1f, 1f, 1f, 0.18f);

                RectTransform backRect = barBack.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0f, 0f);
                backRect.anchorMax = new Vector2(1f, 0f);
                backRect.pivot = new Vector2(0.5f, 0f);
                backRect.offsetMin = new Vector2(60f, 60f); // positioned inside the bubble
                backRect.offsetMax = new Vector2(-60f, 75f);

                GameObject barFill = new GameObject("Hold Progress Fill");
                barFill.transform.SetParent(barBack.transform, false);
                Image fillImage = barFill.AddComponent<Image>();
                fillImage.color = new Color(0.92f, 0.84f, 0.38f, 1f);

                RectTransform fillRect = barFill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                return new InteractionPrompt(panel, label, fillRect, panelImage, backRect);
            }

            public void Show(Interactable owner, string text, float progress, Vector2 uiSize, Vector2 uiPosition, int fontSize, float fontYOffset)
            {
                Owner = owner;

                // Fallback / Auto-upgrade old serialized defaults (450x90) to the new dimensions
                if (uiSize == new Vector2(450f, 90f) || uiSize == Vector2.zero)
                {
                    uiSize = new Vector2(750f, 275.13f);
                }

                // Dynamically apply size and position from inspector configuration
                RectTransform panelRect = _root.GetComponent<RectTransform>();
                panelRect.sizeDelta = uiSize;
                panelRect.anchoredPosition = uiPosition;
                
                bool isHoldEPrompt = text.ToLower().Contains("hold e") || 
                                     text.ToLower().Contains("press e") || 
                                     text.ToLower().Contains("e to") ||
                                     text.Contains("(E)") ||
                                     text.Contains("(e)");

                Sprite promptBg = null;
                if (isHoldEPrompt)
                {
                    promptBg = Resources.Load<Sprite>("Ebutton");
                }
                
                if (promptBg == null)
                {
                    promptBg = Resources.Load<Sprite>("bottomUI");
                }

                if (promptBg != null)
                {
                    _panelImage.sprite = promptBg;
                    _panelImage.color = Color.white;
                }
                else
                {
                    _panelImage.sprite = null;
                    _panelImage.color = new Color(0f, 0f, 0f, 0.72f);
                }

                // Set dynamic font size
                _label.fontSize = fontSize;

                // Adjust positioning of label and center vertically based on whether progress bar is needed
                if (owner.HoldSeconds > 0f)
                {
                    _label.rectTransform.anchorMin = new Vector2(0f, 0.15f); // Shifted up slightly to accommodate bar
                    _label.rectTransform.anchorMax = new Vector2(1f, 1f);
                }
                else
                {
                    _label.rectTransform.anchorMin = new Vector2(0f, 0f); // Fully centered vertically
                    _label.rectTransform.anchorMax = new Vector2(1f, 1f);
                }

                if (isHoldEPrompt && promptBg != null && promptBg.name == "Ebutton")
                {
                    _label.rectTransform.offsetMin = new Vector2(180f, 0f); // Offset to the right to clear the big "E" icon
                    _label.rectTransform.offsetMax = new Vector2(-40f, 0f);
                }
                else
                {
                    _label.rectTransform.offsetMin = new Vector2(40f, 0f); // Centered horizontally
                    _label.rectTransform.offsetMax = new Vector2(-40f, 0f);
                }

                // Apply custom Y offset for font centering
                _label.rectTransform.anchoredPosition = new Vector2(_label.rectTransform.anchoredPosition.x, fontYOffset);

                // Keep progress bar fully symmetric and centered (doesn't shift with Ebutton)
                if (_barBackRect != null)
                {
                    _barBackRect.offsetMin = new Vector2(60f, 60f);
                    _barBackRect.offsetMax = new Vector2(-60f, 75f);
                    
                    // Show progress bar only when the interactable requires holding the key
                    _barBackRect.gameObject.SetActive(owner.HoldSeconds > 0f);
                }

                // Clean instructions to fit with the visual E button icon
                string processedText = text;
                
                // Auto-shorten "You must use [Name] as player to interact" -> "Use [Name]"
                var characterMatch = System.Text.RegularExpressions.Regex.Match(processedText, @"must\s+use\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (characterMatch.Success)
                {
                    processedText = "Use " + characterMatch.Groups[1].Value;
                }
                else
                {
                    processedText = processedText.Replace("(E)", "").Replace("(e)", "").Trim();
                    processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"^(Hold\s+E\s+to\s+|Press\s+E\s+to\s+|E\s+to\s+|Hold\s+E\s+|Press\s+E\s+)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (isHoldEPrompt)
                    {
                        if (!processedText.StartsWith("to ", System.StringComparison.OrdinalIgnoreCase))
                        {
                            processedText = "to " + processedText;
                        }
                    }
                }

                _label.text = processedText;
                _progressFillRect.anchorMax = new Vector2(progress, 1f);
                
                if (_root.transform.parent != null)
                {
                    _root.transform.parent.gameObject.SetActive(true);
                }
                _root.SetActive(true);
            }

            public bool CanShow(Interactable owner, Vector3 playerPosition)
            {
                if (Owner == null || Owner == owner || !Owner.isActiveAndEnabled || Owner._player == null || !Owner.IsPlayerInRange())
                {
                    return true;
                }

                float ownerDistance = (Owner.GetClosestInteractionPoint(playerPosition) - playerPosition).sqrMagnitude;
                float contenderDistance = (owner.GetClosestInteractionPoint(playerPosition) - playerPosition).sqrMagnitude;
                return contenderDistance < ownerDistance;
            }

            public void Hide(Interactable owner)
            {
                if (Owner != owner)
                {
                    return;
                }

                Owner = null;
                _progressFillRect.anchorMax = new Vector2(0f, 1f);
                _root.SetActive(false);
            }

            public void ForceHide()
            {
                Owner = null;
                _progressFillRect.anchorMax = new Vector2(0f, 1f);
                _root.SetActive(false);
            }
        }
    }
}
