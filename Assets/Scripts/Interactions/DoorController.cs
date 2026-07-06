using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

namespace Nemuri.Interactions
{
    public class DoorController : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private const string InteractActionName = "Interact";

        private static DoorInteractionPrompt _prompt;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float _interactionRange = 6f;
        [SerializeField, Min(0.1f)] private float _holdSeconds = 3f;
        [SerializeField] private Transform _interactionPoint;

        [Header("Collision")]
        [SerializeField] private bool _disableCollisionWhenOpen = true;
        [SerializeField] private bool _createClosedDoorBlocker = true;
        [SerializeField, Min(0.05f)] private float _minimumBlockerThickness = 0.45f;

        [Header("Door Rotation")]
        [SerializeField] private Transform _doorPivot;
        [SerializeField] private float _openAngle = 90f;
        [SerializeField] private float _rotationSpeed = 3f;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _isAnimating;

        private Transform _player;
        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private float _holdTimer;
        private bool _isOpen;
        private bool _waitingForRelease;

        private Bounds _interactionBounds;
        private bool _hasInteractionBounds;
        private Collider[] _doorColliders;
        private BoxCollider _generatedBlocker;

        private void Awake()
        {
            CacheInteractionBounds();
            SetupDoorCollision();
            if (_doorPivot == null)
            {
                _doorPivot = transform;
            }

            _closedRotation = _doorPivot.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, _openAngle, 0f);
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

        private IEnumerator RotateDoor()
        {
            _isAnimating = true;

            Quaternion targetRotation = _isOpen
                ? _openRotation
                : _closedRotation;

            while (Quaternion.Angle(_doorPivot.localRotation, targetRotation) > 0.1f)
            {
                _doorPivot.localRotation = Quaternion.RotateTowards(
                    _doorPivot.localRotation,
                    targetRotation,
                    _rotationSpeed * 180f * Time.deltaTime);

                yield return null;
            }

            _doorPivot.localRotation = targetRotation;

            _isAnimating = false;
        }

        private void Update()
        {
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
                _prompt = DoorInteractionPrompt.Create();
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
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= _holdSeconds)
                {
                    ToggleDoor();
                    _holdTimer = 0f;
                    _waitingForRelease = true;
                }
            }

            ShowPrompt();
        }

        private void ToggleDoor()
        {
            if (_isAnimating)
                return;

            _isOpen = !_isOpen;

            ApplyDoorCollision();

            StopAllCoroutines();
            StartCoroutine(RotateDoor());
        }

        private void ShowPrompt()
        {
            string actionText = _isOpen ? "close" : "open";
            _prompt.Show(this, $"Hold E to {actionText}", Mathf.Clamp01(_holdTimer / _holdSeconds));
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

        private bool IsPlayerInRange()
        {
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

        private void SetupDoorCollision()
        {
            _doorColliders = GetComponentsInChildren<Collider>();

            if (_createClosedDoorBlocker)
            {
                EnsureClosedDoorBlocker();
                _doorColliders = GetComponentsInChildren<Collider>();
            }

            ApplyDoorCollision();
        }

        private void EnsureClosedDoorBlocker()
        {
            if (!_hasInteractionBounds)
            {
                return;
            }

            _generatedBlocker = gameObject.GetComponent<BoxCollider>();
            if (_generatedBlocker == null)
            {
                _generatedBlocker = gameObject.AddComponent<BoxCollider>();
            }

            Vector3 localCenter = transform.InverseTransformPoint(_interactionBounds.center);
            Vector3 lossyScale = transform.lossyScale;
            Vector3 localSize = new Vector3(
                SafeDivide(_interactionBounds.size.x, Mathf.Abs(lossyScale.x)),
                SafeDivide(_interactionBounds.size.y, Mathf.Abs(lossyScale.y)),
                SafeDivide(_interactionBounds.size.z, Mathf.Abs(lossyScale.z)));

            int thinnestAxis = GetThinnestAxis(localSize);
            if (thinnestAxis == 0)
            {
                localSize.x = Mathf.Max(localSize.x, SafeDivide(_minimumBlockerThickness, Mathf.Abs(lossyScale.x)));
            }
            else if (thinnestAxis == 1)
            {
                localSize.y = Mathf.Max(localSize.y, SafeDivide(_minimumBlockerThickness, Mathf.Abs(lossyScale.y)));
            }
            else
            {
                localSize.z = Mathf.Max(localSize.z, SafeDivide(_minimumBlockerThickness, Mathf.Abs(lossyScale.z)));
            }

            _generatedBlocker.center = localCenter;
            _generatedBlocker.size = localSize;
            _generatedBlocker.isTrigger = false;
        }

        private void ApplyDoorCollision()
        {
            if (!_disableCollisionWhenOpen || _doorColliders == null)
            {
                return;
            }

            foreach (Collider doorCollider in _doorColliders)
            {
                if (doorCollider != null && !doorCollider.isTrigger)
                {
                    doorCollider.enabled = !_isOpen;
                }
            }
        }

        private Vector3 GetClosestInteractionPoint(Vector3 targetPosition)
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

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > 0.0001f ? value / divisor : value;
        }

        private static int GetThinnestAxis(Vector3 size)
        {
            if (size.x <= size.y && size.x <= size.z)
            {
                return 0;
            }

            return size.y <= size.z ? 1 : 2;
        }

        private GameObject FindPlayerObject()
        {
            try
            {
                return GameObject.FindWithTag(PlayerTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[DoorController] Tag '{PlayerTag}' is not defined in the project.", this);
                return null;
            }
        }

        private sealed class DoorInteractionPrompt
        {
            private readonly GameObject _root;
            private readonly Text _label;
            private readonly Image _progressFill;

            public DoorController Owner { get; private set; }

            private DoorInteractionPrompt(GameObject root, Text label, Image progressFill)
            {
                _root = root;
                _label = label;
                _progressFill = progressFill;
                _root.SetActive(false);
            }

            public static DoorInteractionPrompt Create()
            {
                GameObject canvasObject = new GameObject("Door Interaction Prompt");
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
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;

                RectTransform fillRect = barFill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                return new DoorInteractionPrompt(panel, label, fillImage);
            }

            public void Show(DoorController owner, string text, float progress)
            {
                Owner = owner;
                _label.text = text;
                _progressFill.fillAmount = progress;
                _root.SetActive(true);
            }

            public bool CanShow(DoorController owner, Vector3 playerPosition)
            {
                if (Owner == null || Owner == owner || !Owner.isActiveAndEnabled || Owner._player == null || !Owner.IsPlayerInRange())
                {
                    return true;
                }

                float ownerDistance = (Owner.GetClosestInteractionPoint(playerPosition) - playerPosition).sqrMagnitude;
                float contenderDistance = (owner.GetClosestInteractionPoint(playerPosition) - playerPosition).sqrMagnitude;
                return contenderDistance < ownerDistance;
            }

            public void Hide(DoorController owner)
            {
                if (Owner != owner)
                {
                    return;
                }

                Owner = null;
                _progressFill.fillAmount = 0f;
                _root.SetActive(false);
            }
        }
    }
}
