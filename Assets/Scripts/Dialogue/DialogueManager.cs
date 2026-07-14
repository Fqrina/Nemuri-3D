using System.Collections;
using System.Collections.Generic;
using Nemuri.Player;
using Nemuri.Interactions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nemuri.Dialogue
{
    [System.Serializable]
    public class DialogueNode
    {
        public string speaker;
        public string text;
        public float typingSpeed = 0.01f;
        public string portraitName;
    }

    [System.Serializable]
    public class DialogueSequence
    {
        public List<DialogueNode> nodes;
    }

    public class DialogueManager : MonoBehaviour
    {
        [System.Serializable]
        private class PanelLayoutSettings
        {
            public Vector2 AnchoredPosition = new Vector2(0f, 60f);
            public Vector2 SizeDelta = new Vector2(1100f, 220f);
        }

        [System.Serializable]
        private class NameTextLayoutSettings
        {
            public Vector2 AnchoredPosition = new Vector2(90f, -22f);
            public Vector2 SizeDelta = new Vector2(250f, 50f);
        }

        private const string PlayerTag = "Player";
        private const string DialogueCanvasName = "Dialogue Canvas";

        public static DialogueManager Instance { get; private set; }

        public static System.Action OnConversationStart;
        public static System.Action OnConversationEnd;
        public static System.Action<DialogueNode> OnNodeDisplayed;

        public bool IsConversationActive => _dialoguePanel != null && _dialoguePanel.activeSelf;

        [Header("Prefab UI References")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _dialogueText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Text _skipPromptText;

        [Header("Settings")]
        [SerializeField, Min(0f)] private float _defaultTypingSpeed = 0.01f;

        [Header("Background Sprites")]
        [SerializeField] private Sprite _dialogueSprite;
        [SerializeField] private Sprite _narrationSprite;
        [SerializeField] private Sprite _objectiveSprite;
        [SerializeField] private Image _panelBackgroundImage;

        [Header("Panel Size & Position")]
        [SerializeField] private PanelLayoutSettings _dialoguePanelLayout = new PanelLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, 60f),
            SizeDelta = new Vector2(1100f, 220f)
        };
        [SerializeField] private PanelLayoutSettings _narrationPanelLayout = new PanelLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, 60f),
            SizeDelta = new Vector2(1100f, 220f)
        };
        [SerializeField] private PanelLayoutSettings _objectivePanelLayout = new PanelLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, 60f),
            SizeDelta = new Vector2(1100f, 220f)
        };

        [Header("Dialogue Audio")]
        [SerializeField] private AudioClip _playerDialogueClip;
        [SerializeField] private AudioClip _animalDialogueClip;
        [SerializeField] private AudioClip _thirdDialogueClip;
        [SerializeField, Min(0f)] private float _audioVolume = 1f;

        [Header("Text Layout Settings")]
        [SerializeField] private Vector2 _dialogueTextSize = new Vector2(900f, 120f);
        [SerializeField] private Vector2 _dialogueTextPosition = new Vector2(0f, -15f);
        [SerializeField] private Vector2 _narrationTextSize = new Vector2(900f, 120f);
        [SerializeField] private Vector2 _narrationTextPosition = new Vector2(0f, -15f);
        [SerializeField] private Vector2 _objectiveTextSize = new Vector2(900f, 120f);
        [SerializeField] private Vector2 _objectiveTextPosition = new Vector2(0f, -15f);

        [Header("Name Text Layout")]
        [SerializeField] private NameTextLayoutSettings _dialogueNameTextLayout = new NameTextLayoutSettings
        {
            AnchoredPosition = new Vector2(90f, -22f),
            SizeDelta = new Vector2(250f, 50f)
        };
        [SerializeField] private NameTextLayoutSettings _narrationNameTextLayout = new NameTextLayoutSettings
        {
            AnchoredPosition = new Vector2(90f, -22f),
            SizeDelta = new Vector2(250f, 50f)
        };
        [SerializeField] private NameTextLayoutSettings _objectiveNameTextLayout = new NameTextLayoutSettings
        {
            AnchoredPosition = new Vector2(90f, -22f),
            SizeDelta = new Vector2(250f, 50f)
        };

        [Header("Skip Prompt")]
        [SerializeField] private string _skipPromptLabel = "Hold E to skip";
        [SerializeField] private Vector2 _skipPromptAnchoredPosition = new Vector2(-140f, 25f);
        [SerializeField] private Vector2 _skipPromptSizeDelta = new Vector2(220f, 40f);
        [SerializeField, Min(8)] private int _skipPromptFontSize = 18;

        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private readonly Queue<DialogueNode> _nodes = new Queue<DialogueNode>();
        private bool _isTyping;
        private bool _waitingForInput;
        private DialogueNode _currentNode;
        private Coroutine _typingCoroutine;
        private Coroutine _autoCloseCoroutine;
        private AudioSource _audioSource;
        [Header("Font Setting")]
        [SerializeField] private Font _customFont;
        private Font _uiFont;
        private string _activeSpeaker = "";
        private Queue<DialogueNode> _savedNodes = new Queue<DialogueNode>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDialogueUi();
            SetDialoguePanelActive(false);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.loop = false;
        }

        private void Start()
        {
            BindPlayerInput();
            SetupInput();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_dialoguePanel == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_activeSpeaker))
            {
                UpdatePanelLayout(_activeSpeaker);
            }
            else
            {
                ApplyPanelLayout(_dialoguePanelLayout);
                ApplyNameTextLayout("");
            }

            ApplySkipPromptLayout();
        }
#endif

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
            EnsureDialogueUi();

            if (_playerInput == null)
            {
                BindPlayerInput();
                SetupInput();
            }

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

            SetPlayerMovementEnabled(false);
            OnConversationStart?.Invoke();

            SetDialoguePanelActive(true);
            DisplayNextNode();
        }

        public void ShowDialogue(string speaker, string text)
        {
            var node = new DialogueNode { speaker = speaker, text = text, typingSpeed = _defaultTypingSpeed };
            StartConversation(new List<DialogueNode> { node });
        }

        public void ShowFeedbackDialogue(string speaker, string text)
        {
            if (_savedNodes.Count == 0 && _nodes.Count > 0)
            {
                foreach (var n in _nodes)
                {
                    _savedNodes.Enqueue(n);
                }
            }

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
            OnNodeDisplayed?.Invoke(_currentNode);

            bool hideName = string.Equals(_currentNode.speaker, "Narrator", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_currentNode.speaker, "Objective", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_currentNode.speaker, "SFX", System.StringComparison.OrdinalIgnoreCase);

            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(!hideName);
                _nameText.text = hideName ? "" : _currentNode.speaker;
            }

            if (_portraitImage != null)
            {
                _portraitImage.gameObject.SetActive(!string.IsNullOrEmpty(_currentNode.portraitName) && !hideName);
            }

            UpdatePanelLayout(_currentNode.speaker);

            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }

            StopDialogueAudio();
            PlayDialogueAudio(_currentNode.speaker);
            SetSkipPromptVisible(true);

            _typingCoroutine = StartCoroutine(TypeText(_currentNode.text, _currentNode.typingSpeed));
        }

        private void UpdatePanelLayout(string speaker)
        {
            _activeSpeaker = speaker;

            Image bgImage = _panelBackgroundImage != null ? _panelBackgroundImage : (_dialoguePanel != null ? _dialoguePanel.GetComponent<Image>() : null);
            PanelLayoutSettings layout = GetPanelLayoutForSpeaker(speaker);

            ApplyPanelLayout(layout);

            if (bgImage != null)
            {
                if (string.Equals(speaker, "Narrator", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (_narrationSprite != null) bgImage.sprite = _narrationSprite;
                }
                else if (string.Equals(speaker, "Objective", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (_objectiveSprite != null) bgImage.sprite = _objectiveSprite;
                }
                else if (_dialogueSprite != null)
                {
                    bgImage.sprite = _dialogueSprite;
                }

                bgImage.color = Color.white;
                bgImage.preserveAspect = false;
            }

            if (_dialogueText != null)
            {
                RectTransform textRect = _dialogueText.rectTransform;
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);

                if (string.Equals(speaker, "Narrator", System.StringComparison.OrdinalIgnoreCase))
                {
                    textRect.sizeDelta = _narrationTextSize;
                    textRect.anchoredPosition = _narrationTextPosition;
                }
                else if (string.Equals(speaker, "Objective", System.StringComparison.OrdinalIgnoreCase))
                {
                    textRect.sizeDelta = _objectiveTextSize;
                    textRect.anchoredPosition = _objectiveTextPosition;
                }
                else
                {
                    textRect.sizeDelta = _dialogueTextSize;
                    textRect.anchoredPosition = _dialogueTextPosition;
                }
            }

            ApplySkipPromptLayout();
            ApplyNameTextLayout(speaker);
        }

        private NameTextLayoutSettings GetNameTextLayoutForSpeaker(string speaker)
        {
            if (string.Equals(speaker, "Narrator", System.StringComparison.OrdinalIgnoreCase))
            {
                return _narrationNameTextLayout;
            }

            if (string.Equals(speaker, "Objective", System.StringComparison.OrdinalIgnoreCase))
            {
                return _objectiveNameTextLayout;
            }

            return _dialogueNameTextLayout;
        }

        private void ApplyNameTextLayout(string speaker)
        {
            if (_nameText == null)
            {
                return;
            }

            NameTextLayoutSettings layout = GetNameTextLayoutForSpeaker(speaker);
            if (layout == null)
            {
                return;
            }

            RectTransform rect = _nameText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.SizeDelta;
        }

        private PanelLayoutSettings GetPanelLayoutForSpeaker(string speaker)
        {
            if (string.Equals(speaker, "Narrator", System.StringComparison.OrdinalIgnoreCase))
            {
                return _narrationPanelLayout;
            }

            if (string.Equals(speaker, "Objective", System.StringComparison.OrdinalIgnoreCase))
            {
                return _objectivePanelLayout;
            }

            return _dialoguePanelLayout;
        }

        private void ApplyPanelLayout(PanelLayoutSettings layout)
        {
            if (_dialoguePanel == null || layout == null)
            {
                return;
            }

            RectTransform rect = _dialoguePanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.SizeDelta;
        }

        private void ApplySkipPromptLayout()
        {
            if (_skipPromptText == null)
            {
                return;
            }

            _skipPromptText.text = _skipPromptLabel;
            _skipPromptText.fontSize = _skipPromptFontSize;

            RectTransform rect = _skipPromptText.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = _skipPromptAnchoredPosition;
            rect.sizeDelta = _skipPromptSizeDelta;
        }

        private AudioClip ResolveClipForSpeaker(string speaker)
        {
            if (string.Equals(speaker, "Kael", System.StringComparison.OrdinalIgnoreCase))
            {
                return _playerDialogueClip;
            }

            if (string.Equals(speaker, "Ignore Animal", System.StringComparison.OrdinalIgnoreCase) ||
                speaker.IndexOf("Animal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _animalDialogueClip;
            }

            if (string.Equals(speaker, "Narrator", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, "Objective", System.StringComparison.OrdinalIgnoreCase))
            {
                return _thirdDialogueClip;
            }

            return null;
        }

        private void PlayDialogueAudio(string speaker)
        {
            if (_audioSource == null)
            {
                return;
            }

            AudioClip clipToPlay = ResolveClipForSpeaker(speaker);
            if (clipToPlay == null)
            {
                return;
            }

            _audioSource.clip = clipToPlay;
            _audioSource.volume = _audioVolume;
            _audioSource.loop = true;
            _audioSource.Play();
        }

        private void StopDialogueAudio()
        {
            if (_audioSource == null)
            {
                return;
            }

            _audioSource.loop = false;
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        private void SetSkipPromptVisible(bool visible)
        {
            if (_skipPromptText != null)
            {
                _skipPromptText.gameObject.SetActive(visible);
            }
        }

        private IEnumerator TypeText(string text, float speed)
        {
            _isTyping = true;
            _waitingForInput = false;

            if (_dialogueText != null)
            {
                _dialogueText.text = "";
            }

            SetSkipPromptVisible(true);

            if (string.IsNullOrEmpty(text))
            {
                FinishTyping();
                yield break;
            }

            for (int i = 0; i <= text.Length; i++)
            {
                if (_dialogueText != null)
                {
                    _dialogueText.text = text.Substring(0, i);
                }

                yield return new WaitForSeconds(speed);
            }

            FinishTyping();
        }

        private void FinishTyping()
        {
            _isTyping = false;
            _waitingForInput = true;
            StopDialogueAudio();
            SetSkipPromptVisible(false);

            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
            }

            if (_currentNode != null && string.Equals(_currentNode.speaker, "Objective", System.StringComparison.OrdinalIgnoreCase))
            {
                ProceedToNextNode();
            }
            else
            {
                _autoCloseCoroutine = StartCoroutine(AutoCloseRoutine(2f));
            }
        }

        private IEnumerator AutoCloseRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_waitingForInput)
            {
                ProceedToNextNode();
            }
        }

        private void ProceedToNextNode()
        {
            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }
            
            _waitingForInput = false;

            if (_currentNode != null && string.Equals(_currentNode.speaker, "Objective", System.StringComparison.OrdinalIgnoreCase) &&
                WalkingSceneObjectiveManager.Instance != null)
            {
                PauseConversationForObjective(_currentNode.text);
            }
            else
            {
                DisplayNextNode();
            }
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

                if (_dialogueText != null && _currentNode != null)
                {
                    _dialogueText.text = _currentNode.text;
                }

                FinishTyping();
            }
        }

        private void PauseConversationForObjective(string objectiveText)
        {
            StopDialogueAudio();
            SetSkipPromptVisible(false);

            SetPlayerMovementEnabled(true);

            if (WalkingSceneObjectiveManager.Instance != null)
            {
                WalkingSceneObjectiveManager.Instance.SetActiveObjective(objectiveText);
            }
        }

        public void ResumeConversation()
        {
            SetPlayerMovementEnabled(false);

            SetDialoguePanelActive(true);
            DisplayNextNode();
        }

        private void EndConversation()
        {
            SetDialoguePanelActive(false);
            StopDialogueAudio();
            SetSkipPromptVisible(false);
            _activeSpeaker = "";

            if (_savedNodes.Count > 0)
            {
                _nodes.Clear();
                foreach (var n in _savedNodes)
                {
                    _nodes.Enqueue(n);
                }
                _savedNodes.Clear();
            }

            SetPlayerMovementEnabled(true);
            OnConversationEnd?.Invoke();
        }

        private void SetPlayerMovementEnabled(bool enabled)
        {
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(enabled);
            }
            if (PlayerMovementChapt1.Instance != null)
            {
                PlayerMovementChapt1.Instance.SetCanMove(enabled);
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
                && _skipPromptText != null;
        }

        private void SetDialoguePanelActive(bool active)
        {
            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(active);
            }

            if (!active)
            {
                SetSkipPromptVisible(false);
            }
        }

        private void EnsureDialogueUi()
        {
            if (_dialoguePanel != null && _nameText != null && _dialogueText != null && _skipPromptText != null)
            {
                ApplyPanelLayout(_dialoguePanelLayout);
                ApplySkipPromptLayout();
                ApplyUiFont(_nameText);
                ApplyUiFont(_dialogueText);
                ApplyUiFont(_skipPromptText);
                return;
            }

            LoadUiResources();

            Canvas canvas = GetOrCreateDialogueCanvas();

            if (_dialoguePanel == null)
            {
                Transform existingPanel = canvas.transform.Find("Dialogue Panel") ?? canvas.transform.Find("DialoguePanel");
                if (existingPanel != null)
                {
                    _dialoguePanel = existingPanel.gameObject;
                }
                else
                {
                    _dialoguePanel = new GameObject("Dialogue Panel");
                    _dialoguePanel.transform.SetParent(canvas.transform, false);

                    Image panelImage = _dialoguePanel.AddComponent<Image>();
                    panelImage.sprite = _dialogueSprite;
                    panelImage.color = Color.white;
                    panelImage.type = Image.Type.Simple;
                    _panelBackgroundImage = panelImage;

                    ApplyPanelLayout(_dialoguePanelLayout);
                }
            }

            if (_panelBackgroundImage == null && _dialoguePanel != null)
            {
                _panelBackgroundImage = _dialoguePanel.GetComponent<Image>();
            }

            if (_nameText == null && _dialoguePanel != null)
            {
                Transform existingName = _dialoguePanel.transform.Find("Name Text") ?? _dialoguePanel.transform.Find("NameText");
                if (existingName != null)
                {
                    _nameText = existingName.GetComponent<Text>();
                    _nameText.fontStyle = FontStyle.Bold;
                }
                else
                {
                    GameObject nameGo = new GameObject("Name Text");
                    nameGo.transform.SetParent(_dialoguePanel.transform, false);

                    _nameText = nameGo.AddComponent<Text>();
                    ApplyUiFont(_nameText);
                    _nameText.fontSize = 76;
                    _nameText.alignment = TextAnchor.UpperLeft;
                    _nameText.fontStyle = FontStyle.Bold;
                    _nameText.color = new Color(0.50f, 0.35f, 0.22f, 1f);
                    _nameText.supportRichText = false;

                    RectTransform rect = nameGo.GetComponent<RectTransform>();
                    ApplyNameTextLayout("");
                }
            }
            else if (_nameText != null && _dialoguePanel != null)
            {
                _nameText.transform.SetParent(_dialoguePanel.transform, false);
                _nameText.fontSize = 76;
                _nameText.fontStyle = FontStyle.Bold;
                _nameText.supportRichText = false;
            }

            if (_dialogueText == null && _dialoguePanel != null)
            {
                Transform existingText = _dialoguePanel.transform.Find("Dialogue Text") ?? _dialoguePanel.transform.Find("DialogueText");
                if (existingText != null)
                {
                    _dialogueText = existingText.GetComponent<Text>();
                    _dialogueText.fontStyle = FontStyle.Bold;
                }
                else
                {
                    GameObject textGo = new GameObject("Dialogue Text");
                    textGo.transform.SetParent(_dialoguePanel.transform, false);

                    _dialogueText = textGo.AddComponent<Text>();
                    ApplyUiFont(_dialogueText);
                    _dialogueText.fontSize = 60;
                    _dialogueText.alignment = TextAnchor.UpperLeft;
                    _dialogueText.fontStyle = FontStyle.Bold;
                    _dialogueText.color = Color.white;
                    _dialogueText.supportRichText = false;
                    _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    _dialogueText.verticalOverflow = VerticalWrapMode.Truncate;

                    RectTransform rect = textGo.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = _dialogueTextSize;
                    rect.anchoredPosition = _dialogueTextPosition;
                }
            }
            else if (_dialogueText != null && _dialoguePanel != null)
            {
                _dialogueText.transform.SetParent(_dialoguePanel.transform, false);
                _dialogueText.fontSize = 60;
                _dialogueText.fontStyle = FontStyle.Bold;
                _dialogueText.supportRichText = false;
                _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _dialogueText.verticalOverflow = VerticalWrapMode.Truncate;

                RectTransform rect = _dialogueText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = _dialogueTextSize;
                rect.anchoredPosition = _dialogueTextPosition;
            }

            if (_dialogueText != null)
            {
                ContentSizeFitter fitter = _dialogueText.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    Destroy(fitter);
                }
            }

            if (_skipPromptText == null && _dialoguePanel != null)
            {
                Transform existingSkipPrompt = _dialoguePanel.transform.Find("Skip Prompt Text") ?? _dialoguePanel.transform.Find("SkipPromptText");
                if (existingSkipPrompt != null)
                {
                    _skipPromptText = existingSkipPrompt.GetComponent<Text>();
                }
                else
                {
                    GameObject skipGo = new GameObject("Skip Prompt Text");
                    skipGo.transform.SetParent(_dialoguePanel.transform, false);

                    _skipPromptText = skipGo.AddComponent<Text>();
                    ApplyUiFont(_skipPromptText);
                    _skipPromptText.alignment = TextAnchor.MiddleRight;
                    _skipPromptText.color = new Color(0.92f, 0.84f, 0.38f, 1f);
                    _skipPromptText.supportRichText = false;
                    _skipPromptText.fontStyle = FontStyle.Bold;
                    skipGo.SetActive(false);
                }
            }
            else if (_skipPromptText != null && _dialoguePanel != null)
            {
                _skipPromptText.transform.SetParent(_dialoguePanel.transform, false);
            }

            DestroyLegacyContinueIndicator();
            ApplyUiFont(_nameText);
            ApplyUiFont(_dialogueText);
            ApplyUiFont(_skipPromptText);
            ApplySkipPromptLayout();
        }

        private void DestroyLegacyContinueIndicator()
        {
            if (_dialoguePanel == null)
            {
                return;
            }

            Transform legacyIndicator = _dialoguePanel.transform.Find("Continue Indicator") ?? _dialoguePanel.transform.Find("ContinueIndicator");
            if (legacyIndicator != null)
            {
                Destroy(legacyIndicator.gameObject);
            }
        }

        private void LoadUiResources()
        {
            if (_dialogueSprite == null) _dialogueSprite = LoadUiSprite("Dialogue");
            if (_narrationSprite == null) _narrationSprite = LoadUiSprite("Narration");
            if (_objectiveSprite == null) _objectiveSprite = LoadUiSprite("Objective");
            if (_playerDialogueClip == null) _playerDialogueClip = Resources.Load<AudioClip>("PlayerDialogue");
            if (_animalDialogueClip == null) _animalDialogueClip = Resources.Load<AudioClip>("AnimalDialogue");
            if (_thirdDialogueClip == null) _thirdDialogueClip = Resources.Load<AudioClip>("ThirdDialogue");

            if (_dialogueSprite == null)
            {
                Debug.LogWarning("[DialogueManager] Dialogue background sprite was not found in Resources.", this);
            }

            if (_narrationSprite == null)
            {
                Debug.LogWarning("[DialogueManager] Narration background sprite was not found in Resources.", this);
            }

            if (_objectiveSprite == null)
            {
                Debug.LogWarning("[DialogueManager] Objective background sprite was not found in Resources.", this);
            }

            if (_thirdDialogueClip == null)
            {
                Debug.LogWarning("[DialogueManager] ThirdDialogue audio clip was not found in Resources.", this);
            }
        }

        private static Sprite LoadUiSprite(string resourceName)
        {
            Sprite sprite = Resources.Load<Sprite>(resourceName);
            if (sprite != null)
            {
                return sprite;
            }

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

        private Canvas GetOrCreateDialogueCanvas()
        {
            GameObject existingCanvasObject = GameObject.Find(DialogueCanvasName);
            if (existingCanvasObject != null)
            {
                Canvas existingCanvas = existingCanvasObject.GetComponent<Canvas>();
                if (existingCanvas != null)
                {
                    return existingCanvas;
                }
            }

            GameObject canvasGo = new GameObject(DialogueCanvasName);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);
            return canvas;
        }

        private Font ResolveUiFont()
        {
            if (_customFont != null)
            {
                return _customFont;
            }

            // Try loading from Resources
            _customFont = Resources.Load<Font>("Spinnenkop DEMO");
            if (_customFont != null)
            {
                return _customFont;
            }

            if (_uiFont != null)
            {
                return _uiFont;
            }

            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null)
            {
                _uiFont = Font.CreateDynamicFontFromOSFont("Arial", 26);
            }

            return _uiFont;
        }

        private void ApplyUiFont(Text textComponent)
        {
            if (textComponent == null)
            {
                return;
            }

            Font font = ResolveUiFont();
            if (font != null)
            {
                textComponent.font = font;
            }
        }
    }
}
