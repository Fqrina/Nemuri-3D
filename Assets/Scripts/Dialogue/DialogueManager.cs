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
        protected class PanelLayoutSettings
        {
            public Vector2 AnchoredPosition = new Vector2(0f, 60f);
            public Vector2 SizeDelta = new Vector2(1100f, 220f);
        }

        [System.Serializable]
        protected class NameTextLayoutSettings
        {
            public Vector2 AnchoredPosition = new Vector2(90f, -22f);
            public Vector2 SizeDelta = new Vector2(250f, 50f);
        }

        [System.Serializable]
        public class CharacterSplashArtSettings
        {
            public Sprite SplashSprite;
            public Vector2 AnchoredPosition = new Vector2(0f, 0f);
            public Vector2 SizeDelta = new Vector2(800f, 1000f);
            public bool RenderBehindPanel = true;
        }

        private const string PlayerTag = "Player";
        private const string DialogueCanvasName = "Dialogue Canvas";

        public static DialogueManager Instance { get; private set; }

        public static System.Action OnConversationStart;
        public static System.Action OnConversationEnd;
        public static System.Action<DialogueNode> OnNodeDisplayed;

        public bool IsConversationActive => _dialoguePanel != null && _dialoguePanel.activeSelf && (_activeSpeaker != "Objective" || _isTyping || _waitingForInput);
        public bool canProceed = true;

        [Header("Prefab UI References")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _dialogueText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Text _skipPromptText;
        [SerializeField] private Image _splashArtImage;

        [Header("Character Splash Art Settings")]
        [SerializeField] protected CharacterSplashArtSettings _kaelSplashArtSettings = new CharacterSplashArtSettings
        {
            AnchoredPosition = new Vector2(350f, -100f),
            SizeDelta = new Vector2(800f, 1000f),
            RenderBehindPanel = true
        };
        [SerializeField] protected CharacterSplashArtSettings _feanorSplashArtSettings = new CharacterSplashArtSettings
        {
            AnchoredPosition = new Vector2(400f, 0f),
            SizeDelta = new Vector2(1000f, 1200f),
            RenderBehindPanel = true
        };
        [SerializeField] protected CharacterSplashArtSettings _keikoSplashArtSettings = new CharacterSplashArtSettings
        {
            AnchoredPosition = new Vector2(400f, 0f),
            SizeDelta = new Vector2(1000f, 1200f),
            RenderBehindPanel = true
        };
        [SerializeField] protected CharacterSplashArtSettings _murialSplashArtSettings = new CharacterSplashArtSettings
        {
            AnchoredPosition = new Vector2(400f, 0f),
            SizeDelta = new Vector2(1000f, 1200f),
            RenderBehindPanel = true
        };
        [SerializeField] protected CharacterSplashArtSettings _ronaSplashArtSettings = new CharacterSplashArtSettings
        {
            AnchoredPosition = new Vector2(400f, 0f),
            SizeDelta = new Vector2(1000f, 1200f),
            RenderBehindPanel = true
        };

        [Header("Settings")]
        [SerializeField, Min(0f)] protected float _defaultTypingSpeed = 0.01f;

        [Header("Background Sprites")]
        [SerializeField] protected Sprite _dialogueSprite;
        [SerializeField] protected Sprite _narrationSprite;
        [SerializeField] protected Sprite _objectiveSprite;
        [SerializeField] protected Image _panelBackgroundImage;

        [Header("Panel Size & Position")]
        [SerializeField] protected PanelLayoutSettings _dialoguePanelLayout = new PanelLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, -50f),
            SizeDelta = new Vector2(2150f, 1300f)
        };
        [SerializeField] protected PanelLayoutSettings _narrationPanelLayout = new PanelLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, 760f),
            SizeDelta = new Vector2(1920f, 1080f)
        };
        [SerializeField] protected PanelLayoutSettings _objectivePanelLayout = new PanelLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, 60f),
            SizeDelta = new Vector2(1920f, 1080f)
        };

        [Header("Dialogue Audio")]
        [SerializeField] protected AudioClip _playerDialogueClip;
        [SerializeField] protected AudioClip _animalDialogueClip;
        [SerializeField] protected AudioClip _thirdDialogueClip;
        [SerializeField, Min(0f)] protected float _audioVolume = 10f;

        [Header("Text Layout Settings")]
        [SerializeField] protected Vector2 _dialogueTextSize = new Vector2(1400f, 600f);
        [SerializeField] protected Vector2 _dialogueTextPosition = new Vector2(20f, -475f);
        [SerializeField] protected Vector2 _narrationTextSize = new Vector2(1700f, 600f);
        [SerializeField] protected Vector2 _narrationTextPosition = new Vector2(30f, -530f);
        [SerializeField] protected Vector2 _objectiveTextSize = new Vector2(1350f, 600f);
        [SerializeField] protected Vector2 _objectiveTextPosition = new Vector2(-100f, 220f);

        [Header("Name Text Layout")]
        [SerializeField] protected NameTextLayoutSettings _dialogueNameTextLayout = new NameTextLayoutSettings
        {
            AnchoredPosition = new Vector2(530f, -700f),
            SizeDelta = new Vector2(850f, 500f)
        };
        [SerializeField] protected NameTextLayoutSettings _narrationNameTextLayout = new NameTextLayoutSettings
        {
            AnchoredPosition = new Vector2(90f, -22f),
            SizeDelta = new Vector2(650f, 350f)
        };
        [SerializeField] protected NameTextLayoutSettings _objectiveNameTextLayout = new NameTextLayoutSettings
        {
            AnchoredPosition = new Vector2(90f, 0f),
            SizeDelta = new Vector2(650f, 350f)
        };

        [Header("Skip Prompt")]
        [SerializeField] protected string _skipPromptLabel = "Hold E to skip";
        [SerializeField] protected Vector2 _skipPromptAnchoredPosition = new Vector2(-120f, -80f);
        [SerializeField] protected Vector2 _skipPromptSizeDelta = new Vector2(400f, 300f);
        [SerializeField, Min(8)] protected int _skipPromptFontSize = 42;

        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private readonly Queue<DialogueNode> _nodes = new Queue<DialogueNode>();
        private bool _isTyping;
        protected bool _waitingForInput;
        protected DialogueNode _currentNode;
        private Coroutine _typingCoroutine;
        protected Coroutine _autoCloseCoroutine;
        private AudioSource _audioSource;
        [Header("Font Setting")]
        [SerializeField] protected Font _customFont;
        protected Font _uiFont;
        protected string _activeSpeaker = "";
        protected Queue<DialogueNode> _savedNodes = new Queue<DialogueNode>();

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.GetType() != this.GetType())
                {
                    Destroy(Instance.gameObject);
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
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

        protected virtual void Start()
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
                UpdateSplashArtLayout("");
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

        protected virtual void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractAction;
            }
        }

        public void StartConversation(List<DialogueNode> sequence)
        {
            Interactable.ForceHidePrompt();
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

            DismissHeldObjective();

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

            if (string.Equals(_currentNode.speaker, "SFX", System.StringComparison.OrdinalIgnoreCase))
            {
                DisplayNextNode();
                return;
            }

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
            UpdateSplashArtLayout(speaker);
        }

        private CharacterSplashArtSettings GetSplashArtSettingsForSpeaker(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return null;

            if (string.Equals(speaker, "Kael", System.StringComparison.OrdinalIgnoreCase))
                return _kaelSplashArtSettings;
            if (string.Equals(speaker, "Feanor", System.StringComparison.OrdinalIgnoreCase))
                return _feanorSplashArtSettings;
            if (string.Equals(speaker, "Keiko", System.StringComparison.OrdinalIgnoreCase))
                return _keikoSplashArtSettings;
            if (string.Equals(speaker, "Murial", System.StringComparison.OrdinalIgnoreCase))
                return _murialSplashArtSettings;
            if (string.Equals(speaker, "Rona", System.StringComparison.OrdinalIgnoreCase))
                return _ronaSplashArtSettings;

            return null;
        }

        private void UpdateSplashArtLayout(string speaker)
        {
            if (_splashArtImage == null) return;

            CharacterSplashArtSettings settings = GetSplashArtSettingsForSpeaker(speaker);
            if (settings != null && settings.SplashSprite != null)
            {
                _splashArtImage.sprite = settings.SplashSprite;
                _splashArtImage.gameObject.SetActive(true);

                RectTransform rect = _splashArtImage.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = settings.AnchoredPosition;
                rect.sizeDelta = settings.SizeDelta;

                if (_dialoguePanel != null && _splashArtImage.transform.parent == _dialoguePanel.transform.parent)
                {
                    int panelIndex = _dialoguePanel.transform.GetSiblingIndex();
                    int splashIndex = _splashArtImage.transform.GetSiblingIndex();

                    if (settings.RenderBehindPanel)
                    {
                        if (splashIndex > panelIndex)
                        {
                            _splashArtImage.transform.SetSiblingIndex(panelIndex);
                        }
                    }
                    else
                    {
                        if (splashIndex < panelIndex)
                        {
                            _splashArtImage.transform.SetSiblingIndex(panelIndex);
                        }
                    }
                }
            }
            else
            {
                _splashArtImage.gameObject.SetActive(false);
            }
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
            if (string.IsNullOrEmpty(speaker))
            {
                return _thirdDialogueClip;
            }

            if (string.Equals(speaker, "Kael", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, "Rona", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, "Murial", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, "Keiko", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, "Feanor", System.StringComparison.OrdinalIgnoreCase))
            {
                return _playerDialogueClip;
            }

            if (string.Equals(speaker, "Ferry", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, "Animal", System.StringComparison.OrdinalIgnoreCase) ||
                speaker.IndexOf("Animal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _animalDialogueClip;
            }

            return _thirdDialogueClip;
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

        protected void StopDialogueAudio()
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

        protected void SetSkipPromptVisible(bool visible)
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

        protected IEnumerator AutoCloseRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_waitingForInput)
            {
                ProceedToNextNode();
            }
        }

        protected virtual void ProceedToNextNode()
        {
            if (!canProceed)
            {
                if (_autoCloseCoroutine != null)
                {
                    StopCoroutine(_autoCloseCoroutine);
                }
                _autoCloseCoroutine = StartCoroutine(AutoCloseRoutine(0.5f));
                return;
            }

            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }
            
            _waitingForInput = false;

            if (_currentNode != null && string.Equals(_currentNode.speaker, "Objective", System.StringComparison.OrdinalIgnoreCase))
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

        protected virtual void PauseConversationForObjective(string objectiveText)
        {
            StopDialogueAudio();
            SetSkipPromptVisible(false);

            SetPlayerMovementEnabled(true);

            if (WalkingSceneObjectiveManager.Instance != null)
            {
                WalkingSceneObjectiveManager.Instance.SetActiveObjective(objectiveText);
            }

            OnConversationEnd?.Invoke();
        }

        public void DismissHeldObjective()
        {
        }

        public void ResumeConversation()
        {
            SetPlayerMovementEnabled(false);

            SetDialoguePanelActive(true);
            DisplayNextNode();
        }

        public void ForceEndConversation()
        {
            EndConversation();
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

        protected virtual void SetPlayerMovementEnabled(bool enabled)
        {
            var move1 = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
            foreach (var m in move1)
            {
                m.SetCanMove(enabled);
            }

            var move2 = FindObjectsByType<PlayerMovementChapt1>(FindObjectsInactive.Include);
            foreach (var m in move2)
            {
                m.SetCanMove(enabled);
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

        protected void SetDialoguePanelActive(bool active)
        {
            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(active);
            }

            if (!active)
            {
                SetSkipPromptVisible(false);
                if (_splashArtImage != null)
                {
                    _splashArtImage.gameObject.SetActive(false);
                }
            }
        }

        public void ToggleDialoguePanel(bool isActive)
        {
            SetDialoguePanelActive(isActive);
        }

        private void EnsureDialogueUi()
        {
            LoadUiResources();

            Canvas canvas = GetOrCreateDialogueCanvas();

            if (_splashArtImage == null && canvas != null)
            {
                Transform existingSplash = canvas.transform.Find("Splash Art Image") ?? canvas.transform.Find("SplashArtImage");
                if (existingSplash != null)
                {
                    _splashArtImage = existingSplash.GetComponent<Image>();
                }
                else
                {
                    GameObject splashGo = new GameObject("Splash Art Image");
                    splashGo.transform.SetParent(canvas.transform, false);

                    _splashArtImage = splashGo.AddComponent<Image>();
                    _splashArtImage.color = Color.white;
                    _splashArtImage.preserveAspect = true;
                    _splashArtImage.raycastTarget = false;
                    splashGo.SetActive(false);
                }
            }

            if (_dialoguePanel != null && _nameText != null && _dialogueText != null && _skipPromptText != null)
            {
                ApplyPanelLayout(_dialoguePanelLayout);
                ApplySkipPromptLayout();
                ApplyUiFont(_nameText);
                ApplyUiFont(_dialogueText);
                ApplyUiFont(_skipPromptText);
                return;
            }

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

            if (_kaelSplashArtSettings.SplashSprite == null) _kaelSplashArtSettings.SplashSprite = LoadUiSprite("KaelSplashArt");
            if (_feanorSplashArtSettings.SplashSprite == null) _feanorSplashArtSettings.SplashSprite = LoadUiSprite("FeanorSplashArt");
            if (_keikoSplashArtSettings.SplashSprite == null) _keikoSplashArtSettings.SplashSprite = LoadUiSprite("KeikoSplashArt");
            if (_murialSplashArtSettings.SplashSprite == null) _murialSplashArtSettings.SplashSprite = LoadUiSprite("MurialSplashArt");
            if (_ronaSplashArtSettings.SplashSprite == null) _ronaSplashArtSettings.SplashSprite = LoadUiSprite("RonaSplashArt");

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
