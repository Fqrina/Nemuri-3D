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

        public UnityEvent OnInteract;

        private Transform _player;
        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private float _holdTimer;
        private bool _waitingForRelease;

        private Bounds _interactionBounds;
        private bool _hasInteractionBounds;

        public string PromptText
        {
            get => _promptText;
            set => _promptText = value;
        }

        public float InteractionRange => _interactionRange;
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
            _prompt.Show(this, _promptText, progress);
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
            _prompt.Show(this, promptText, Mathf.Clamp01(normalizedProgress));
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

            public Interactable Owner { get; private set; }

            private InteractionPrompt(GameObject root, Text label, RectTransform progressFillRect)
            {
                _root = root;
                _label = label;
                _progressFillRect = progressFillRect;
                _root.SetActive(false);
            }

            public static InteractionPrompt Create()
            {
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
                panelRect.sizeDelta = new Vector2(320f, 76f);

                GameObject labelObject = new GameObject("Prompt Text");
                labelObject.transform.SetParent(panel.transform, false);
                Text label = labelObject.AddComponent<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (label.font == null)
                {
                    label.font = Font.CreateDynamicFontFromOSFont("Arial", 26);
                }

                label.fontSize = 26;

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0.28f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = new Vector2(16f, 0f);
                labelRect.offsetMax = new Vector2(-16f, -4f);

                GameObject barBack = new GameObject("Hold Progress Back");
                barBack.transform.SetParent(panel.transform, false);
                Image backImage = barBack.AddComponent<Image>();
                backImage.color = new Color(1f, 1f, 1f, 0.18f);

                RectTransform backRect = barBack.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0f, 0f);
                backRect.anchorMax = new Vector2(1f, 0f);
                backRect.pivot = new Vector2(0.5f, 0f);
                backRect.offsetMin = new Vector2(18f, 14f);
                backRect.offsetMax = new Vector2(-18f, 22f);

                GameObject barFill = new GameObject("Hold Progress Fill");
                barFill.transform.SetParent(barBack.transform, false);
                Image fillImage = barFill.AddComponent<Image>();
                fillImage.color = new Color(0.92f, 0.84f, 0.38f, 1f);

                RectTransform fillRect = barFill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                return new InteractionPrompt(panel, label, fillRect);
            }

            public void Show(Interactable owner, string text, float progress)
            {
                Owner = owner;
                _label.text = text;
                _progressFillRect.anchorMax = new Vector2(progress, 1f);
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
