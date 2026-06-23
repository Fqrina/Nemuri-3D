using TMPro;
using System.Collections;
using System.Collections.Generic;
using Nemuri.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nemuri.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        public static DialogueManager Instance { get; private set; }

        [Header("Prefab UI References")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private GameObject _continueIndicator;

        [Header("Settings")]
        [SerializeField, Min(0f)] private float _defaultTypingSpeed = 0.05f;

        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private readonly Queue<DialogueNode> _nodes = new Queue<DialogueNode>();
        private bool _isTyping;
        private bool _waitingForInput;
        private DialogueNode _currentNode;
        private Coroutine _typingCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SetDialoguePanelActive(false);
        }

        private void Start()
        {
            BindPlayerInput();
            SetupInput();
        }

        private void SetupInput()
        {
            if (_playerInput == null)
            {
                Debug.LogWarning("[DialogueManager] PlayerInput was not found on the Player object.", this);
                return;
            }

            _interactAction = _playerInput.actions.FindAction("Interact");
            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractAction;
                return;
            }

            Debug.LogWarning("[DialogueManager] Interact action was not found in the PlayerInput actions.", this);
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractAction;
            }
        }

        public void StartConversation(List<DialogueNode> sequence)
        {
            if (sequence == null || sequence.Count == 0)
            {
                return;
            }

            if (!HasRequiredUi())
            {
                Debug.LogWarning("[DialogueManager] Dialogue UI references are incomplete.", this);
                return;
            }

            _nodes.Clear();
            foreach (DialogueNode node in sequence)
            {
                _nodes.Enqueue(node);
            }

            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(false);
            }

            SetDialoguePanelActive(true);
            DisplayNextNode();
        }

        public void ShowDialogue(string speaker, string text)
        {
            var node = new DialogueNode { speaker = speaker, text = text, typingSpeed = _defaultTypingSpeed };
            StartConversation(new List<DialogueNode> { node });
        }

        public void DisplayNextNode()
        {
            if (_nodes.Count == 0)
            {
                EndConversation();
                return;
            }

            _currentNode = _nodes.Dequeue();
            _nameText.text = _currentNode.speaker;

            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }

            _typingCoroutine = StartCoroutine(TypeText(_currentNode.text, _currentNode.typingSpeed));
        }

        private IEnumerator TypeText(string text, float speed)
        {
            _isTyping = true;
            _dialogueText.text = text;
            _dialogueText.maxVisibleCharacters = 0;
            _continueIndicator.SetActive(false);

            for (int i = 0; i <= text.Length; i++)
            {
                _dialogueText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(speed);
            }

            _isTyping = false;
            _waitingForInput = true;
            _continueIndicator.SetActive(true);
        }

        public void OnInteractAction(InputAction.CallbackContext context)
        {
            if (_dialoguePanel == null || !_dialoguePanel.activeSelf)
            {
                return;
            }

            if (_isTyping)
            {
                if (_typingCoroutine != null)
                {
                    StopCoroutine(_typingCoroutine);
                }

                _dialogueText.maxVisibleCharacters = _currentNode.text.Length;
                _isTyping = false;
                _waitingForInput = true;
                _continueIndicator.SetActive(true);
            }
            else if (_waitingForInput)
            {
                _waitingForInput = false;
                DisplayNextNode();
            }
        }

        private void EndConversation()
        {
            SetDialoguePanelActive(false);
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(true);
            }
        }

        private void BindPlayerInput()
        {
            GameObject playerObject = FindPlayerObject();
            if (playerObject != null)
            {
                _playerInput = playerObject.GetComponent<PlayerInput>();
            }
        }

        private GameObject FindPlayerObject()
        {
            try
            {
                return GameObject.FindWithTag(PlayerTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[DialogueManager] Tag '{PlayerTag}' is not defined in the project.", this);
                return null;
            }
        }

        private bool HasRequiredUi()
        {
            return _dialoguePanel != null
                && _nameText != null
                && _dialogueText != null
                && _continueIndicator != null;
        }

        private void SetDialoguePanelActive(bool active)
        {
            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(active);
            }
        }
    }
}
