using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nemuri.Dialogue;
using Nemuri.Player;
using Nemuri.Core;
using Nemuri.Inventory;
using Nemuri.UI;
using Nemuri.Interactions;

namespace Nemuri.Scenes
{
    public class HippocampusIntroController : MonoBehaviour
    {
        private enum IntroStage
        {
            ObtainBag,
            ExploreCircadianIsle,
            NearClockTower,
            CollectAlarmClock,
            NearDeskLamp,
            CollectDeskLamp,
            NearFiles,
            CollectFiles,
            GoBackToMainIsland1,
            MakeKeyPromptGroup1,
            ReconstructMemoryPuzzle1,

            // Island 2: Memory Archives
            ExploreMemoryArchives,
            NearPhoto,
            CollectPhoto,
            NearNovel,
            CollectNovel,
            NearCassette,
            CollectCassette,
            ReconstructMemoryPuzzle2,

            // Island 3: Anxiety Heights
            ExploreAnxietyHeights,
            NearCoffeeMug,
            CollectCoffeeMug,
            NearPlushRabbit,
            CollectPlushRabbit,
            NearPillBottle,
            CollectPillBottle,
            ReconstructMemoryPuzzle3,
            GoBackToMainIsland,
            MainIslandFerryCutscene,
            IslandsFallingCutscene,
            CompletedAll
        }

        [Header("Main Island Climax Configuration")]
        [SerializeField] private Transform _mainIslandFerrySpawn;
        [SerializeField] private Transform _shrinePillarTarget;
        [SerializeField] private Transform _pillarCameraTarget;
        [SerializeField] private Transform _fallingCameraTarget;
        [SerializeField] private List<GameObject> _fallingObjects = new List<GameObject>();
        [SerializeField] private string _nextSceneName = "chpt3";

        [Header("NPC GameObjects")]
        [SerializeField] private GameObject _keikoNpc;
        [SerializeField] private GameObject _ronaNpc;
        [SerializeField] private GameObject _murialNpc;
        [SerializeField] private GameObject _feanorNpc;
        [SerializeField] private GameObject _ferryNpc;

        [Header("Teleport First Ferry Locations")]
        [SerializeField] private Transform _teleportKael;   // TeleportFirstFerry1
        [SerializeField] private Transform _teleportKeiko;  // TeleportFirstFerry2
        [SerializeField] private Transform _teleportRona;   // TeleportFirstFerry3
        [SerializeField] private Transform _teleportMurial; // TeleportFirstFerry4
        [SerializeField] private Transform _teleportFeanor; // TeleportFirstFerry5

        [Header("Camera Transition Targets")]
        [SerializeField] private GameObject _circadianIsleGo;
        [SerializeField] private GameObject _memoryArchiveGo;
        [SerializeField] private GameObject _anxietyHeightsGo;

        [Header("Camera Offsets")]
        [SerializeField] private Vector3 _kaelCamOffset = new Vector3(-3f, 3f, 4f);
        [SerializeField] private Vector3 _circadianIsleCamOffset = new Vector3(0f, 15f, -25f);
        [SerializeField] private Vector3 _memoryArchiveCamOffset = new Vector3(0f, 15f, -25f);
        [SerializeField] private Vector3 _anxietyHeightsCamOffset = new Vector3(0f, 15f, -25f);
        [SerializeField] private Vector3 _ferryCamOffset = new Vector3(-6f, 4f, 5f);

        [Header("Bridges")]
        [SerializeField] private GameObject _firstBridgeGo;

        [Header("Other Objects")]
        [SerializeField] private GameObject _bagObject;
        [SerializeField] private GameObject _hotbarCanvasOverride;

        [Header("Island 1 - Broken Alarm Clock")]
        [SerializeField] private GameObject _alarmClockGo;
        [SerializeField] private GameObject _alarmClockTrigger;

        [Header("Island 1 - Desk Lamp")]
        [SerializeField] private GameObject _deskLampGo;
        [SerializeField] private GameObject _deskLampTrigger;

        [Header("Island 1 - Scattered Files")]
        [SerializeField] private GameObject _filesGo;
        [SerializeField] private GameObject _filesTrigger;

        [Header("Island 1 - Puzzle Target & Trigger")]
        [SerializeField] private GameObject _reconstructTriggerGo;
        [SerializeField] private GameObject _island1Trigger;

        [Header("Island 2 - Memory Archives Trigger & Items")]
        [SerializeField] private GameObject _island2Trigger;
        [SerializeField] private GameObject _photoGo;
        [SerializeField] private GameObject _photoTrigger;
        [SerializeField] private GameObject _novelGo;
        [SerializeField] private GameObject _novelTrigger;
        [SerializeField] private GameObject _cassetteGo;
        [SerializeField] private GameObject _cassetteTrigger;
        [SerializeField] private GameObject _reconstruct2TriggerGo;

        [Header("Island 3 - Anxiety Heights Trigger & Items")]
        [SerializeField] private GameObject _island3Trigger;
        [SerializeField] private GameObject _coffeeMugGo;
        [SerializeField] private GameObject _coffeeMugTrigger;
        [SerializeField] private GameObject _plushRabbitGo;
        [SerializeField] private GameObject _plushRabbitTrigger;
        [SerializeField] private GameObject _pillBottleGo;
        [SerializeField] private GameObject _pillBottleTrigger;
        [SerializeField] private GameObject _reconstruct3TriggerGo;

        [Header("Daftar 9 GameObjects Puzzle (Tipe J, H, G)")]
        [SerializeField] private GameObject _puzzle1Go;
        [SerializeField] private GameObject _puzzle2Go;
        [SerializeField] private GameObject _puzzle3Go;
        [SerializeField] private GameObject _puzzle4Go;
        [SerializeField] private GameObject _puzzle5Go;
        [SerializeField] private GameObject _puzzle6Go;
        [SerializeField] private GameObject _puzzle7Go;
        [SerializeField] private GameObject _puzzle8Go;
        [SerializeField] private GameObject _puzzle9Go;

        [Header("Crystal Icons (Optional)")]
        [SerializeField] private Sprite _crystal1Icon;
        [SerializeField] private Sprite _crystal2Icon;
        [SerializeField] private Sprite _crystal3Icon;

        [Header("Trigger Configurations")]
        [SerializeField] private float _triggerDistance = 5.0f;

        [Header("Custom UI Elements for Vision")]
        [SerializeField] private Image _customVisionImageUI;

        [Header("Animation Configurations")]
        [SerializeField] private float _animatorSpeedMultiplier = 2.0f;

        [Header("Audio Configurations")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _ferryPoofSound;

        public static HippocampusIntroController Instance { get; private set; }

        public static bool IsVisionModeActive => Instance != null &&
            Instance._customVisionImageUI != null &&
            Instance._customVisionImageUI.gameObject.activeSelf;

        private IntroStage _currentStage = IntroStage.ObtainBag;

        public bool IsAtReconstructStage => _currentStage == IntroStage.ReconstructMemoryPuzzle1 ||
                                             _currentStage == IntroStage.ReconstructMemoryPuzzle2 ||
                                             _currentStage == IntroStage.ReconstructMemoryPuzzle3;

        public ItemGroup CurrentActiveGroup
        {
            get
            {
                if (_currentStage >= IntroStage.ExploreAnxietyHeights)
                {
                    return ItemGroup.Group3;
                }
                if (_currentStage >= IntroStage.ExploreMemoryArchives)
                {
                    return ItemGroup.Group2;
                }
                return ItemGroup.Group1;
            }
        }

        private Vector3 _ferryInitialPos;
        private Quaternion _ferryInitialRot;
        private bool _isFerryActiveInCutscene = false;

        private bool _hasPickedUpBag = false;
        private bool _hasPickedUpClock = false;
        private bool _hasPickedUpLamp = false;
        private bool _hasPickedUpFiles = false;
        private bool _hasPickedUpPhoto = false;
        private bool _hasPickedUpNovel = false;
        private bool _hasPickedUpCassette = false;
        private bool _hasPickedUpCoffeeMug = false;
        private bool _hasPickedUpPlushRabbit = false;
        private bool _hasPickedUpPillBottle = false;
        private bool _hasReconstructedMemory = false;

        private GameObject _hotbarCanvas;
        private List<Interactable> _disabledInteractables = new List<Interactable>();
        private bool _conversationEnded = false;

        // when true, proximity triggers in Update() are suppressed
        private bool _triggerLocked = false;

        // programmatically created black overlay used for vision sequences
        // sorting order 848: above game world, below dialogue (900)
        private Image _visionBackdrop;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Cache reference to hotbar canvas while it is active on Awake
            _hotbarCanvas = _hotbarCanvasOverride != null ? _hotbarCanvasOverride : GameObject.Find("Hotbar Canvas");

            // Deactivate the bag immediately on Awake to prevent early visibility
            if (_bagObject != null)
            {
                _bagObject.SetActive(false);
            }
        }

        private void Start()
        {
            SetAllAnimatorsSpeed(_animatorSpeedMultiplier);

            BuildVisionBackdrop();

            // Force the vision image canvas to sort between VisionManager (400) and Dialogue (900)
            if (_customVisionImageUI != null)
            {
                Canvas visionCanvas = _customVisionImageUI.GetComponentInParent<Canvas>();
                if (visionCanvas != null)
                {
                    visionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    visionCanvas.sortingOrder = 850;
                }
                _customVisionImageUI.gameObject.SetActive(false);
            }

            InitializeInteractableItem(_bagObject, OnBagPickedUp);
            InitializeInteractableItem(_alarmClockGo, OnAlarmClockPickedUp);
            InitializeInteractableItem(_deskLampGo, OnDeskLampPickedUp);
            InitializeInteractableItem(_filesGo, OnFilesPickedUp);
            InitializeInteractableItem(_reconstructTriggerGo, OnMemoryReconstructed);

            InitializeInteractableItem(_photoGo, OnPhotoPickedUp);
            InitializeInteractableItem(_novelGo, OnNovelPickedUp);
            InitializeInteractableItem(_cassetteGo, OnCassettePickedUp);
            InitializeInteractableItem(_reconstruct2TriggerGo, OnMemoryReconstructed);

            InitializeInteractableItem(_coffeeMugGo, OnCoffeeMugPickedUp);
            InitializeInteractableItem(_plushRabbitGo, OnPlushRabbitPickedUp);
            InitializeInteractableItem(_pillBottleGo, OnPillBottlePickedUp);
            InitializeInteractableItem(_reconstruct3TriggerGo, OnMemoryReconstructed);

            StartCoroutine(IntroStartRoutine());
        }

        private void OnEnable()
        {
            DialogueManager.OnNodeDisplayed += HandleNodeDisplayed;
            DialogueManager.OnConversationEnd += HandleConversationEnd;
        }

        private void OnDisable()
        {
            DialogueManager.OnNodeDisplayed -= HandleNodeDisplayed;
            DialogueManager.OnConversationEnd -= HandleConversationEnd;

            // Restore normal speed when leaving this scene or when component is disabled
            SetAllAnimatorsSpeed(1.0f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            SetAllAnimatorsSpeed(1.0f);
        }

        private void Update()
        {
            // suppress proximity triggers while a dialogue cutscene is explicitly locked
            if (_triggerLocked) return;

            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer == null) return;

            // Stage-based proximity checks to automatically trigger dialogue sequences
            switch (_currentStage)
            {
                case IntroStage.ExploreCircadianIsle:
                    if (_island1Trigger != null)
                    {
                        float dist = Vector3.Distance(activePlayer.position, _island1Trigger.transform.position);
                        if (dist <= _triggerDistance)
                        {
                            Debug.Log($"[IntroController] Island1 trigger activated at distance {dist}!");
                            StartIsland1Dialogue();
                        }
                    }
                    break;

                case IntroStage.NearClockTower:
                {
                    Vector3 clockCheckPos = _alarmClockTrigger != null
                        ? _alarmClockTrigger.transform.position
                        : (_alarmClockGo != null ? _alarmClockGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, clockCheckPos);
                    
                    if (Time.frameCount % 30 == 0 && _alarmClockTrigger != null)
                    {
                        Debug.Log($"[IntroController] Distance to Clock Trigger: {dist:F2} (Target range: {_triggerDistance})");
                    }

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Clock trigger activated at distance {dist}!");
                        StartAlarmClockDialogue();
                    }
                    break;
                }

                case IntroStage.NearDeskLamp:
                {
                    Vector3 lampCheckPos = _deskLampTrigger != null
                        ? _deskLampTrigger.transform.position
                        : (_deskLampGo != null ? _deskLampGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, lampCheckPos);

                    if (Time.frameCount % 30 == 0 && _deskLampTrigger != null)
                    {
                        Debug.Log($"[IntroController] Distance to Lamp Trigger: {dist:F2} (Target range: {_triggerDistance})");
                    }

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Lamp trigger activated at distance {dist}!");
                        StartDeskLampDialogue();
                    }
                    break;
                }

                case IntroStage.NearFiles:
                {
                    Vector3 filesCheckPos = _filesTrigger != null
                        ? _filesTrigger.transform.position
                        : (_filesGo != null ? _filesGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, filesCheckPos);

                    if (Time.frameCount % 30 == 0 && _filesTrigger != null)
                    {
                        Debug.Log($"[IntroController] Distance to Files Trigger: {dist:F2} (Target range: {_triggerDistance})");
                    }

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Files trigger activated at distance {dist}!");
                        StartFilesDialogue();
                    }
                    break;
                }

                // Island 2: Memory Archives
                case IntroStage.ExploreMemoryArchives:
                {
                    if (_island2Trigger != null)
                    {
                        float dist = Vector3.Distance(activePlayer.position, _island2Trigger.transform.position);
                        if (dist <= _triggerDistance)
                        {
                            Debug.Log($"[IntroController] Island 2 trigger activated at distance {dist}!");
                            StartIsland2Dialogue();
                        }
                    }
                    break;
                }

                case IntroStage.NearPhoto:
                {
                    Vector3 checkPos = _photoTrigger != null
                        ? _photoTrigger.transform.position
                        : (_photoGo != null ? _photoGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, checkPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Photo trigger activated at distance {dist}!");
                        StartPhotoDialogue();
                    }
                    break;
                }

                case IntroStage.NearNovel:
                {
                    Vector3 checkPos = _novelTrigger != null
                        ? _novelTrigger.transform.position
                        : (_novelGo != null ? _novelGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, checkPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Novel trigger activated at distance {dist}!");
                        StartNovelDialogue();
                    }
                    break;
                }

                case IntroStage.NearCassette:
                {
                    Vector3 checkPos = _cassetteTrigger != null
                        ? _cassetteTrigger.transform.position
                        : (_cassetteGo != null ? _cassetteGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, checkPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Cassette trigger activated at distance {dist}!");
                        StartCassetteDialogue();
                    }
                    break;
                }

                // Island 3: Anxiety Heights
                case IntroStage.ExploreAnxietyHeights:
                {
                    if (_island3Trigger != null)
                    {
                        float dist = Vector3.Distance(activePlayer.position, _island3Trigger.transform.position);
                        if (dist <= _triggerDistance)
                        {
                            Debug.Log($"[IntroController] Island 3 trigger activated at distance {dist}!");
                            StartIsland3Dialogue();
                        }
                    }
                    break;
                }

                case IntroStage.NearCoffeeMug:
                {
                    Vector3 checkPos = _coffeeMugTrigger != null
                        ? _coffeeMugTrigger.transform.position
                        : (_coffeeMugGo != null ? _coffeeMugGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, checkPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Coffee Mug trigger activated at distance {dist}!");
                        StartCoffeeMugDialogue();
                    }
                    break;
                }

                case IntroStage.NearPlushRabbit:
                {
                    Vector3 checkPos = _plushRabbitTrigger != null
                        ? _plushRabbitTrigger.transform.position
                        : (_plushRabbitGo != null ? _plushRabbitGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, checkPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Plush Rabbit trigger activated at distance {dist}!");
                        StartPlushRabbitDialogue();
                    }
                    break;
                }

                case IntroStage.NearPillBottle:
                {
                    Vector3 checkPos = _pillBottleTrigger != null
                        ? _pillBottleTrigger.transform.position
                        : (_pillBottleGo != null ? _pillBottleGo.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, checkPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Pill Bottle trigger activated at distance {dist}!");
                        StartPillBottleDialogue();
                    }
                    break;
                }

                case IntroStage.GoBackToMainIsland1:
                {
                    Vector3 ferryPos = _mainIslandFerrySpawn != null
                        ? _mainIslandFerrySpawn.position
                        : (_ferryNpc != null ? _ferryNpc.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, ferryPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Player returned to Ferry on main island for Group 1! Starting shrine dialogue.");
                        StartGroup1MainIslandShrineDialogue();
                    }
                    break;
                }

                case IntroStage.MakeKeyPromptGroup1:
                {
                    if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
                    {
                        Debug.Log("[IntroController] Player pressed C to make Group 1 key! Triggering Puzzle 1-3 chain.");
                        StartGroup1PuzzleChain();
                    }
                    break;
                }

                case IntroStage.GoBackToMainIsland:
                {
                    Vector3 ferryPos = _mainIslandFerrySpawn != null
                        ? _mainIslandFerrySpawn.position
                        : (_ferryNpc != null ? _ferryNpc.transform.position : Vector3.positiveInfinity);
                    float dist = Vector3.Distance(activePlayer.position, ferryPos);

                    if (dist <= _triggerDistance)
                    {
                        Debug.Log($"[IntroController] Player returned to Ferry on main island at distance {dist}! Triggering final climax cutscene.");
                        StartMainIslandFerryCutscene();
                    }
                    break;
                }
            }
        }

        private bool _isContinuousEarthquake = false;
        private float _continuousShakeMagnitude = 0.15f;

        private void LateUpdate()
        {
            // Force Ferry NPC to stay locked at its design time coordinates when active,
            // bypassing any animator root motion or physics drift.
            if (_isFerryActiveInCutscene && _ferryNpc != null && _ferryNpc.activeSelf)
            {
                _ferryNpc.transform.position = _ferryInitialPos;
                _ferryNpc.transform.rotation = _ferryInitialRot;
            }

            if (_isContinuousEarthquake && Camera.main != null)
            {
                float x = Random.Range(-1f, 1f) * _continuousShakeMagnitude;
                float y = Random.Range(-1f, 1f) * _continuousShakeMagnitude;
                Camera.main.transform.position += new Vector3(x, y, 0f);
            }
        }

        private void InitializeInteractableItem(GameObject go, UnityEngine.Events.UnityAction callback)
        {
            if (go == null) return;

            Interactable inter = go.GetComponent<Interactable>();
            if (inter == null) inter = go.GetComponentInChildren<Interactable>();
            if (inter != null)
            {
                inter.InteractionRange = 2.0f;
                inter.OnInteract.RemoveAllListeners();
                inter.OnInteract.AddListener(callback);

                // Clear the _interactionPoint so the prompt anchors to the object's actual
                // world position instead of a stale transform reference
                var ipField = typeof(Interactable).GetField("_interactionPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (ipField != null)
                {
                    ipField.SetValue(inter, null);
                }
            }

            // disable PickupItem so its built-in listener does not fire alongside this controller's callback
            PickupItem pickup = go.GetComponent<PickupItem>();
            if (pickup == null) pickup = go.GetComponentInChildren<PickupItem>();
            if (pickup != null)
            {
                pickup.enabled = false;
            }
        }

        private IEnumerator IntroStartRoutine()
        {
            // Lock movement and inventory systems immediately
            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            // Force active character to Kael and lock swapping during cutscene
            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SetCharacterUnlocked(0, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(1, false);
                CharacterSwapManager.Instance.SetCharacterUnlocked(2, false);
                CharacterSwapManager.Instance.SetCharacterUnlocked(3, false);
                CharacterSwapManager.Instance.SetCharacterUnlocked(4, false);
                CharacterSwapManager.Instance.SwapToCharacter(0, isDialogueSwap: true);
            }

            // Save Ferry NPC's initial coordinate and orientation (keeps active as it was at start)
            if (_ferryNpc != null)
            {
                _ferryInitialPos = _ferryNpc.transform.position;
                _ferryInitialRot = _ferryNpc.transform.rotation;
                _isFerryActiveInCutscene = false;
            }

            // Hide the first bridge on start
            if (_firstBridgeGo != null)
            {
                _firstBridgeGo.SetActive(false);
            }

            // Bag starts inactive until Ferry disappears and spawns it
            if (_bagObject != null) _bagObject.SetActive(false);

            // Items remain visible in the world the whole time so players can see them,
            // but their Interactable components are disabled until their dialogue trigger fires
            DisableAllOtherInteractables();

            // Wait a moment for initialization to settle
            yield return new WaitForSeconds(0.5f);

            // Construct dialogue nodes
            List<DialogueNode> nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Kael and the Energeons are pulled into a mysterious world, where forgotten memories slowly begin to take shape." },
                new DialogueNode { speaker = "Kael", text = "What is this place...?" },
                new DialogueNode { speaker = "Keiko", text = "I can feel something here... but I've never been here before." },
                new DialogueNode { speaker = "Rona", text = "Wait... look over there. There are three islands." },
                new DialogueNode { speaker = "Ferry", text = "Hey fellas!" },
                new DialogueNode { speaker = "Murial", text = "Gosh... why does he always appear out of nowhere?" },
                new DialogueNode { speaker = "Ferry", text = "Welcome to Hippocampus!" },
                new DialogueNode { speaker = "Feanor", text = "Hippocampus... a realm where memories are stored, reconstructed, and understood." },
                new DialogueNode { speaker = "Ferry", text = "Exactly!" },
                new DialogueNode { speaker = "Rona", text = "But what does this place have to do with restoring the Nocturne Heart?" },
                new DialogueNode { speaker = "Ferry", text = "The answer is not something you can simply be told." },
                new DialogueNode { speaker = "Ferry", text = "It is something you must discover yourself..." },
                new DialogueNode { speaker = "Ferry", text = "Because the key to restoring the Nocturne Heart lies within your own memories, Kael." },
                new DialogueNode { speaker = "Kael", text = "My memories...?" },
                new DialogueNode { speaker = "Ferry", text = "Three fragments await you." },
                new DialogueNode { speaker = "Ferry", text = "The memories of your habits..." },
                new DialogueNode { speaker = "Ferry", text = "The memories of what you have forgotten..." },
                new DialogueNode { speaker = "Ferry", text = "And the memories of what you fear." },
                new DialogueNode { speaker = "Ferry", text = "Only by understanding them can you uncover the truth." },
                new DialogueNode { speaker = "Murial", text = "Huh, can we really trust a rabbit that can suddenly disappear like that…" },
                new DialogueNode { speaker = "Feanor", text = "I don’t know, but it seems we don't really have a choice here" },
                new DialogueNode { speaker = "Rona", text = "I think he left something" },
                new DialogueNode { speaker = "Kael", text = "It’s a bag" },
                new DialogueNode { speaker = "Objective", text = "Obtain the bag" },
                new DialogueNode { speaker = "Objective", text = "Explore The Circadian Isle" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        private void HandleNodeDisplayed(DialogueNode node)
        {
            if (node.text.Contains("Hey fellas!"))
            {
                StartCoroutine(FerryAppearRoutine());
            }
            else if (node.text.Contains("Because the key to restoring the Nocturne Heart lies within your own memories, Kael."))
            {
                StartCoroutine(PanCameraToTargetRoutine(FindActivePlayerTransform(), _kaelCamOffset));
            }
            else if (node.text.Contains("The memories of your habits..."))
            {
                StartCoroutine(PanCameraToTargetRoutine(_circadianIsleGo != null ? _circadianIsleGo.transform : null, _circadianIsleCamOffset));
            }
            else if (node.text.Contains("The memories of what you have forgotten..."))
            {
                StartCoroutine(PanCameraToTargetRoutine(_memoryArchiveGo != null ? _memoryArchiveGo.transform : null, _memoryArchiveCamOffset));
            }
            else if (node.text.Contains("And the memories of what you fear."))
            {
                StartCoroutine(PanCameraToTargetRoutine(_anxietyHeightsGo != null ? _anxietyHeightsGo.transform : null, _anxietyHeightsCamOffset));
            }
            else if (node.text.Contains("Only by understanding them can you uncover the truth."))
            {
                // Pan back to Ferry before he delivers his final line and disappears
                StartCoroutine(PanCameraToFerryRoutine());
            }
            else if (node.text.Contains("Huh, can we really trust a rabbit that can suddenly disappear like that…"))
            {
                // Disappear Ferry, play poof sound, rotate group smoothly, and look back at player
                StartCoroutine(FerryDisappearAndTurnRoutine());
            }
            else if (node.text.Contains("Explore The Circadian Isle"))
            {
                // Re-enable interactables for the exploration phase
                ReEnableInteractables();
            }
            else if (node.text.Contains("Kael stays silent, looking around the island."))
            {
                // Pan camera to clock tower and back
                StartCoroutine(SilentLookAroundRoutine());
            }
            else if (node.text.Contains("Search for 3 mysterious fragments on this island"))
            {
                // Ensure interactables are fully re-enabled
                ReEnableInteractables();
            }
            else if (node.text.Contains("SFX World shaking"))
            {
                // Trigger screen shake
                StartCoroutine(ScreenShakeRoutine(1.5f, 0.2f));
            }
        }

        private void HandleConversationEnd()
        {
            _conversationEnded = true;
            _triggerLocked = false;

            // Re-enable Cinemachine Brain to return controls to character follow camera
            if (Camera.main != null)
            {
                var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
                if (brain != null)
                {
                    brain.enabled = true;
                }
            }

            SetPlayerMovementEnabled(true);

            if (!_hasPickedUpBag)
            {
                return;
            }

            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SetCharacterUnlocked(0, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(1, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(2, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(3, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(4, true);
            }

            SetInventoryLocked(false);
            ReEnableInteractables();
        }

        private IEnumerator FerryAppearRoutine()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = false;
            }

            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            // Teleport Ferry to its correct initial coordinate BEFORE starting camera pan
            if (_ferryNpc != null)
            {
                _ferryNpc.SetActive(true);
                _isFerryActiveInCutscene = true;
                _ferryNpc.transform.position = _ferryInitialPos;
                _ferryNpc.transform.rotation = _ferryInitialRot;
            }

            // Smooth pan to Ferry's position
            Vector3 startPos = Camera.main.transform.position;
            Quaternion startRot = Camera.main.transform.rotation;

            Vector3 targetPos = _ferryInitialPos + _ferryCamOffset;
            Vector3 lookDir = (_ferryInitialPos - targetPos).normalized;
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

            float elapsed = 0f;
            float duration = 2.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
                Camera.main.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            Camera.main.transform.position = targetPos;
            Camera.main.transform.rotation = targetRot;

            // Fade out, teleport/orient entities, fade back in
            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToBlack(0.25f);
            }

            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer != null && _teleportKael != null)
            {
                Vector3 targetPlayerPos = GetSnappedPosition(_teleportKael.position);
                Vector3 dir = (_ferryInitialPos - targetPlayerPos);
                dir.y = 0f;
                Quaternion targetPlayerRot = dir != Vector3.zero ? Quaternion.LookRotation(dir, Vector3.up) : activePlayer.rotation;

                TeleportObject(activePlayer.gameObject, targetPlayerPos, targetPlayerRot);
            }

            TeleportAndFaceFerry(_keikoNpc, _teleportKeiko);
            TeleportAndFaceFerry(_ronaNpc, _teleportRona);
            TeleportAndFaceFerry(_murialNpc, _teleportMurial);
            TeleportAndFaceFerry(_feanorNpc, _teleportFeanor);

            yield return new WaitForSeconds(0.1f);

            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToClear(0.25f);
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = true;
            }
        }

        private IEnumerator PanCameraToFerryRoutine()
        {
            if (Camera.main == null) yield break;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = false;
            }

            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            Vector3 startPos = Camera.main.transform.position;
            Quaternion startRot = Camera.main.transform.rotation;

            Vector3 targetPos = _ferryInitialPos + _ferryCamOffset;
            Vector3 lookDir = (_ferryInitialPos - targetPos).normalized;
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

            float elapsed = 0f;
            float duration = 1.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
                Camera.main.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            Camera.main.transform.position = targetPos;
            Camera.main.transform.rotation = targetRot;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = true;
            }
        }

        private IEnumerator PanCameraToTargetRoutine(Transform target, Vector3 offset)
        {
            if (target == null || Camera.main == null) yield break;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = false;
            }

            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            Vector3 startPos = Camera.main.transform.position;
            Quaternion startRot = Camera.main.transform.rotation;

            Vector3 targetPos = offset != Vector3.zero ? target.position + offset : target.position;
            Quaternion targetRot = offset != Vector3.zero
                ? (target.position != targetPos ? Quaternion.LookRotation((target.position - targetPos).normalized, Vector3.up) : target.rotation)
                : target.rotation;

            float elapsed = 0f;
            float duration = 2.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
                Camera.main.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            Camera.main.transform.position = targetPos;
            Camera.main.transform.rotation = targetRot;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = true;
            }
        }

        private IEnumerator FerryDisappearAndTurnRoutine()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = false;
            }

            _isFerryActiveInCutscene = false;

            // 1. Play poof audio
            if (_audioSource != null && _ferryPoofSound != null)
            {
                _audioSource.PlayOneShot(_ferryPoofSound);
            }
            else if (_ferryPoofSound != null)
            {
                AudioSource.PlayClipAtPoint(_ferryPoofSound, _ferryInitialPos);
            }

            // 2. Shrink and Fade Ferry smoothly
            if (_ferryNpc != null)
            {
                Vector3 startScale = _ferryNpc.transform.localScale;
                float elapsedShrink = 0f;
                float durationShrink = 0.5f;

                Renderer[] renderers = _ferryNpc.GetComponentsInChildren<Renderer>();
                List<Material> mats = new List<Material>();
                foreach (var r in renderers)
                {
                    if (r.material != null) mats.Add(r.material);
                }

                while (elapsedShrink < durationShrink)
                {
                    elapsedShrink += Time.deltaTime;
                    float t = elapsedShrink / durationShrink;

                    _ferryNpc.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

                    foreach (var mat in mats)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.color;
                            c.a = Mathf.Lerp(1f, 0f, t);
                            mat.color = c;
                        }
                    }
                    yield return null;
                }

                // Deactivate the NPC and also deactivate the parent FERRYANIMATED gameobject to prevent it flying/floating
                if (_ferryNpc.transform.parent != null && _ferryNpc.transform.parent.name.Contains("FERRY"))
                {
                    _ferryNpc.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    _ferryNpc.SetActive(false);
                }

                // Reset scale for future references
                _ferryNpc.transform.localScale = startScale;
            }

            // 3. Spawn the bag exactly at Ferry's coordinates (snapped to ground)
            if (_bagObject != null)
            {
                _bagObject.transform.position = GetSnappedPosition(_ferryInitialPos);
                _bagObject.SetActive(true);

                // Re-enable the interactable component for the bag explicitly
                Interactable inter = _bagObject.GetComponent<Interactable>();
                if (inter == null) inter = _bagObject.GetComponentInChildren<Interactable>();
                if (inter != null)
                {
                    inter.enabled = true;
                }
            }

            // 4. Wait for a second as requested
            yield return new WaitForSeconds(1.0f);

            // 5. Smoothly rotate group to face center (over 1 second)
            RotateGroupToCenterSmooth(1.0f);

            // 6. Smooth pan back to Kael/Active Player
            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer != null)
            {
                Vector3 startPos = Camera.main.transform.position;
                Quaternion startRot = Camera.main.transform.rotation;

                Vector3 targetPos = activePlayer.position + _kaelCamOffset;
                Vector3 lookDir = (activePlayer.position - targetPos).normalized;
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

                float elapsed = 0f;
                float duration = 1.5f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    t = Mathf.SmoothStep(0f, 1f, t);

                    Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    Camera.main.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = true;
            }
        }

        private void OnBagPickedUp()
        {
            _hasPickedUpBag = true;

            // Remove/destroy the bag GameObject so it vanishes (does NOT add it as item)
            if (_bagObject != null)
            {
                Interactable inter = _bagObject.GetComponent<Interactable>();
                if (inter == null) inter = _bagObject.GetComponentInChildren<Interactable>();
                if (inter != null)
                {
                    inter.DismissInteraction();
                    inter.enabled = false;
                }
                Destroy(_bagObject);
                // reveal the hotbar canvas now that the bag has been obtained.
                // use HotbarUI.CanvasObject which is set during HotbarUI.Start() — always the correct reference.
                if (HotbarUI.CanvasObject != null)
                {
                    HotbarUI.CanvasObject.SetActive(true);
                }
            }

            // unlock inventory so items picked up from here onwards register
            SetInventoryLocked(false);

            // unlock all characters now so the player can swap freely during exploration
            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SetCharacterUnlocked(0, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(1, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(2, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(3, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(4, true);
            }

            if (DialogueManager.Instance != null)
            {
                // Transition to the next node which will display "Explore The Circadian Isle" objective
                DialogueManager.Instance.ResumeConversation();
                _currentStage = IntroStage.ExploreCircadianIsle;

                // Smoothly spawn/reveal first bridge
                SmoothBridgeController sbc = GetComponent<SmoothBridgeController>();
                if (sbc != null)
                {
                    sbc.TriggerBridge1();
                }
                else if (_firstBridgeGo != null)
                {
                    StartCoroutine(RevealBridgeRoutine(_firstBridgeGo));
                }
            }
        }

        private IEnumerator RevealBridgeRoutine(GameObject bridge)
        {
            if (bridge == null) yield break;
            bridge.SetActive(true);

            Vector3 originalScale = bridge.transform.localScale;
            Vector3 startScale = new Vector3(originalScale.x, 0f, originalScale.z);

            Vector3 targetPos = bridge.transform.position;
            Vector3 startPos = targetPos - new Vector3(0f, 4f, 0f); // rise from below

            bridge.transform.localScale = startScale;
            bridge.transform.position = startPos;

            float elapsed = 0f;
            float duration = 2.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                bridge.transform.localScale = Vector3.Lerp(startScale, originalScale, t);
                bridge.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            bridge.transform.localScale = originalScale;
            bridge.transform.position = targetPos;
        }

        private void StartIsland1Dialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.NearClockTower;

            // Lock movement and disable inventory
            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            // Teleport NPCs dynamically to form a circle facing Kael
            TeleportNPCsToActivePlayer();

            List<DialogueNode> islandNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "At the center of the island stands a massive clock tower, towering over the endless night.." },
                new DialogueNode { speaker = "Keiko", text = "This place... feels unsettling." },
                new DialogueNode { speaker = "Feanor", text = "An endless night... It feels like this island is trapped in a cycle." },
                new DialogueNode { speaker = "Rona", text = "A cycle...?" },
                new DialogueNode { speaker = "Feanor", text = "Yeah. Like it's waiting for something... but morning never comes." },
                new DialogueNode { speaker = "Narrator", text = "Kael stays silent, looking around the island." },
                new DialogueNode { speaker = "Murial", text = "Kael... this place seems familiar to you, doesn't it?" },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Kael", text = "I don't know. But somehow... it feels like I've been here before." },
                new DialogueNode { speaker = "Keiko", text = "Wait... I feel something nearby." },
                new DialogueNode { speaker = "Rona", text = "You do? Where?" },
                new DialogueNode { speaker = "Keiko", text = "I’m not sure... but something is pulling us there." },
                new DialogueNode { speaker = "Kael", text = "Then let's follow it." },
                new DialogueNode { speaker = "Kael", text = "Lead the way, Keiko." },
                new DialogueNode { speaker = "Objective", text = "Search for 3 mysterious fragments on this island" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(islandNodes);
            }
        }

        private void StartAlarmClockDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectAlarmClock;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);
            TeleportNPCsToActivePlayer();

            List<DialogueNode> nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Murial", text = "An alarm clock?" },
                new DialogueNode { speaker = "Keiko", text = "I don’t know but i can feel a strong resonance through that alarm clock" },
                new DialogueNode { speaker = "Kael", text = "It’s my alarm clock…" },
                new DialogueNode { speaker = "Kael", text = "How did it get here?" },
                new DialogueNode { speaker = "Objective", text = "Collect the alarm clock" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }

            // Unlock ONLY the alarm clock for interaction (it was always visible, just non-interactable)
            DisableAllOtherInteractablesExcept(_alarmClockGo);
        }

        private void OnAlarmClockPickedUp()
        {
            StartCoroutine(AlarmClockVisionAndDialogueRoutine());
        }

        private IEnumerator AlarmClockVisionAndDialogueRoutine()
        {
            _hasPickedUpClock = true;

            Sprite clockSprite = GetVisionSpriteFromItem(_alarmClockGo);

            if (_alarmClockGo != null)
            {
                AddItemToInventory(_alarmClockGo);
                Interactable inter = _alarmClockGo.GetComponent<Interactable>();
                if (inter == null) inter = _alarmClockGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_alarmClockGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());
            ShowVisionImage(clockSprite);

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "The ringing of the alarm clocks no longer mattered." },
                new DialogueNode { speaker = "Narrator", text = "Nights blurred into mornings." },
                new DialogueNode { speaker = "Narrator", text = "Kael barely noticed the difference anymore." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            HideVisionImage();
            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Keiko", text = "I think these objects means deeper than it looked" },
                new DialogueNode { speaker = "Kael", text = "I really did ignore the alarms…" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            CheckGroup1Progress();
        }

        private void CheckGroupCollectionProgress()
        {
            if (_currentStage >= IntroStage.ExploreAnxietyHeights)
            {
                CheckGroup3Progress();
            }
            else if (_currentStage >= IntroStage.ExploreMemoryArchives)
            {
                CheckGroup2Progress();
            }
            else
            {
                CheckGroup1Progress();
            }
        }

        private bool _group1PuzzlesStarted = false;
        private bool _group2PuzzlesStarted = false;
        private bool _group3PuzzlesStarted = false;

        private void StartGroup1PuzzleChain()
        {
            if (_group1PuzzlesStarted) return;
            _group1PuzzlesStarted = true;
            _currentStage = IntroStage.ReconstructMemoryPuzzle1;

            Debug.Log("[IntroController] Group 1 (3 items) collected! Opening Puzzle 1...");

            System.Action onPuzzle1Solved = null;
            onPuzzle1Solved = () =>
            {
                PuzzleHelper.UnregisterOnPuzzleSolved(_puzzle1Go, onPuzzle1Solved);
                PuzzleHelper.ClosePuzzle(_puzzle1Go);
                Debug.Log("[IntroController] Puzzle 1 Solved! Opening Puzzle 2...");

                System.Action onPuzzle2Solved = null;
                onPuzzle2Solved = () =>
                {
                    PuzzleHelper.UnregisterOnPuzzleSolved(_puzzle2Go, onPuzzle2Solved);
                    PuzzleHelper.ClosePuzzle(_puzzle2Go);
                    Debug.Log("[IntroController] Puzzle 2 Solved! Opening Puzzle 3...");

                    System.Action onPuzzle3Solved = null;
                    onPuzzle3Solved = () =>
                    {
                        PuzzleHelper.UnregisterOnPuzzleSolved(_puzzle3Go, onPuzzle3Solved);
                        PuzzleHelper.ClosePuzzle(_puzzle3Go);
                        Debug.Log("[IntroController] Puzzle 3 Solved! Absorbing Group 1 items...");

                        OnGroup1PuzzlesCompleted();
                    };
                    PuzzleHelper.RegisterOnPuzzleSolved(_puzzle3Go, onPuzzle3Solved);
                    PuzzleHelper.OpenPuzzle(_puzzle3Go);
                };
            PuzzleHelper.RegisterOnPuzzleSolved(_puzzle2Go, onPuzzle2Solved);
                PuzzleHelper.OpenPuzzle(_puzzle2Go);
            };

            PuzzleHelper.RegisterOnPuzzleSolved(_puzzle1Go, onPuzzle1Solved);
            PuzzleHelper.OpenPuzzle(_puzzle1Go);
        }

        private void OnGroup1PuzzlesCompleted()
        {
            if (HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.AbsorbGroupAndGrantCrystal(
                    ItemGroup.Group1,
                    "First Island Key",
                    _crystal1Icon,
                    "The first island key crafted from Circadian Isle memories."
                );
            }
            StartCoroutine(Group1KeyHandoverDialogueRoutine());
        }

        private void StartGroup1MainIslandShrineDialogue()
        {
            StartCoroutine(Group1MainIslandShrineDialogueRoutine());
        }

        private IEnumerator Group1MainIslandShrineDialogueRoutine()
        {
            _currentStage = IntroStage.MakeKeyPromptGroup1;
            SetPlayerMovementEnabled(false);

            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer != null)
            {
                Vector3 center = activePlayer.position;
                Transform lookTarget = _ferryNpc != null ? _ferryNpc.transform : activePlayer;
                TeleportAndOrientNPC(_keikoNpc, center + new Vector3(-1.8f, 0f, 1.8f), lookTarget);
                TeleportAndOrientNPC(_ronaNpc, center + new Vector3(1.8f, 0f, 1.8f), lookTarget);
                TeleportAndOrientNPC(_murialNpc, center + new Vector3(-1.8f, 0f, -1.8f), lookTarget);
                TeleportAndOrientNPC(_feanorNpc, center + new Vector3(1.8f, 0f, -1.8f), lookTarget);
            }

            List<DialogueNode> part1Nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Ferry", text = "I see that you have gotten the objects" },
                new DialogueNode { speaker = "Rona", text = "Now that we're done, what are we going to do with it?" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part1Nodes);
            }

            yield return WaitForConversation();

            // Pan camera to Shrine / Pillar
            Transform shrineTarget = _shrinePillarTarget != null ? _shrinePillarTarget : _pillarCameraTarget;
            if (shrineTarget == null)
            {
                GameObject shrineObj = GameObject.Find("Shrine");
                if (shrineObj == null) shrineObj = GameObject.Find("Pillar");
                if (shrineObj == null) shrineObj = GameObject.Find("Nocturne Heart");
                if (shrineObj != null) shrineTarget = shrineObj.transform;
            }

            if (shrineTarget != null)
            {
                yield return StartCoroutine(PanCameraToTargetRoutine(shrineTarget, Vector3.zero));
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            List<DialogueNode> part2Nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Ferry", text = "See that pillar with a keyhole in it?" },
                new DialogueNode { speaker = "Ferry", text = "From those objects that you obtain, you can make a key." },
                new DialogueNode { speaker = "Ferry", text = "Now, try making a key!" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part2Nodes);
            }

            yield return WaitForConversation();

            SetPlayerMovementEnabled(true);

            List<DialogueNode> promptNode = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Objective", text = "Make a key (Press C)" }
            };
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(promptNode);
            }
        }

        private IEnumerator Group1KeyHandoverDialogueRoutine()
        {
            SetPlayerMovementEnabled(false);

            List<DialogueNode> handoverNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "Woah..." },
                new DialogueNode { speaker = "Rona", text = "We did it! The first key is complete." },
                new DialogueNode { speaker = "Rona", text = "Let's put it in the Nocturne Heart!" },
                new DialogueNode { speaker = "Ferry", text = "Wait a minute there!" },
                new DialogueNode { speaker = "Ferry", text = "Give the key to me first." },
                new DialogueNode { speaker = "Murial", text = "Why?" },
                new DialogueNode { speaker = "Ferry", text = "Because you can't restore the Nocturne Heart with only one key." },
                new DialogueNode { speaker = "Feanor", text = "Then what happens if we try?" },
                new DialogueNode { speaker = "Ferry", text = "It won't work." },
                new DialogueNode { speaker = "Ferry", text = "You need all three keys before the Nocturne Heart can be restored." },
                new DialogueNode { speaker = "Murial", text = "And you need to hold onto them?" },
                new DialogueNode { speaker = "Ferry", text = "Yes." },
                new DialogueNode { speaker = "Feanor", text = "Why though?" },
                new DialogueNode { speaker = "Ferry", text = "Trust me on this." },
                new DialogueNode { speaker = "Kael", text = "Just give it to him." },
                new DialogueNode { speaker = "Kael", text = "He was the one who guided us here in the first place." },
                new DialogueNode { speaker = "Kael", text = "I don't think he means any harm." },
                new DialogueNode { speaker = "Rona", text = "Are you sure, Kael?" },
                new DialogueNode { speaker = "Kael", text = "He showed us the way when we didn't know where to go." },
                new DialogueNode { speaker = "Kael", text = "Why would he try to stop us now?" },
                new DialogueNode { speaker = "Rona", text = "I guess you’re right Kael…" },
                new DialogueNode { speaker = "Ferry", text = "Great! Now see you!" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(handoverNodes);
            }

            yield return WaitForConversation();

            // Ferry poof disappearance cutscene
            yield return StartCoroutine(FerryDisappearAndTurnRoutine());

            Debug.Log("[HippocampusIntroController] Activating Bridge 2 to Memory Archives...");
            SmoothBridgeController sbc = GetComponent<SmoothBridgeController>();
            if (sbc == null) sbc = FindAnyObjectByType<SmoothBridgeController>();
            if (sbc != null)
            {
                sbc.TriggerBridge2();
            }

            _currentStage = IntroStage.ExploreMemoryArchives;
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            List<DialogueNode> nextObjective = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Objective", text = "Explore Memory Archives" }
            };
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nextObjective);
            }
        }

        private void StartGroup2PuzzleChain()
        {
            if (_group2PuzzlesStarted) return;
            _group2PuzzlesStarted = true;
            _currentStage = IntroStage.ReconstructMemoryPuzzle2;

            Debug.Log("[IntroController] Group 2 (3 items) collected! Opening Puzzle 4...");

            System.Action onPuzzle4Solved = null;
            onPuzzle4Solved = () =>
            {
                PuzzleHelper.UnregisterOnPuzzleSolved(_puzzle4Go, onPuzzle4Solved);
                PuzzleHelper.ClosePuzzle(_puzzle4Go);
                Debug.Log("[IntroController] Puzzle 4 Solved! Opening Puzzle 5...");

                System.Action onPuzzle5Solved = null;
                onPuzzle5Solved = () =>
                {
                    PuzzleHelper.UnregisterOnPuzzleSolved(_puzzle5Go, onPuzzle5Solved);
                    PuzzleHelper.ClosePuzzle(_puzzle5Go);
                    Debug.Log("[IntroController] Puzzle 5 Solved! Opening Puzzle 6...");

                    System.Action onPuzzle6Solved = null;
                    onPuzzle6Solved = () =>
                    {
                        PuzzleHelper.UnregisterOnPuzzleSolved(_puzzle6Go, onPuzzle6Solved);
                        PuzzleHelper.ClosePuzzle(_puzzle6Go);
                        Debug.Log("[IntroController] Puzzle 6 Solved! Absorbing Group 2 items...");

                        OnGroup2PuzzlesCompleted();
                    };
                    PuzzleHelper.RegisterOnPuzzleSolved(_puzzle6Go, onPuzzle6Solved);
                    PuzzleHelper.OpenPuzzle(_puzzle6Go);
                };
                PuzzleHelper.RegisterOnPuzzleSolved(_puzzle5Go, onPuzzle5Solved);
                PuzzleHelper.OpenPuzzle(_puzzle5Go);
            };

            PuzzleHelper.RegisterOnPuzzleSolved(_puzzle4Go, onPuzzle4Solved);
            PuzzleHelper.OpenPuzzle(_puzzle4Go);
        }

        private void OnGroup2PuzzlesCompleted()
        {
            if (HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.AbsorbGroupAndGrantCrystal(
                    ItemGroup.Group2,
                    "Aether Shard",
                    _crystal2Icon,
                    "The glowing crystal fragment of Memory Archives."
                );
            }
            OnMemoryReconstructed();
        }

        private void CheckGroup1Progress()
        {
            if (HotbarInventory.Instance != null && HotbarInventory.Instance.IsGroupFullyCollected(ItemGroup.Group1))
            {
                Debug.Log("[IntroController] Group 1 is fully collected! Spawning Ferry on main island and prompting player to return.");

                if (_ferryNpc != null)
                {
                    if (_ferryNpc.transform.parent != null)
                    {
                        _ferryNpc.transform.parent.gameObject.SetActive(true);
                    }
                    _ferryNpc.SetActive(true);
                    if (_mainIslandFerrySpawn != null)
                    {
                        _ferryInitialPos = _mainIslandFerrySpawn.position;
                        _ferryInitialRot = _mainIslandFerrySpawn.rotation;
                    }
                    _ferryNpc.transform.position = _ferryInitialPos;
                    _ferryNpc.transform.rotation = _ferryInitialRot;
                    _isFerryActiveInCutscene = true;
                }

                _currentStage = IntroStage.GoBackToMainIsland1;
                SetPlayerMovementEnabled(true);

                List<DialogueNode> returnObjective = new List<DialogueNode>
                {
                    new DialogueNode { speaker = "Objective", text = "Return to Ferry on the main island" }
                };
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartConversation(returnObjective);
                }
            }
            else
            {
                if (!_hasPickedUpClock)
                {
                    _currentStage = IntroStage.NearClockTower;
                }
                else if (!_hasPickedUpLamp)
                {
                    _currentStage = IntroStage.NearDeskLamp;
                }
                else if (!_hasPickedUpFiles)
                {
                    _currentStage = IntroStage.NearFiles;
                }
            }
            UpdateInteractablesState();
        }

        private void CheckGroup2Progress()
        {
            if (HotbarInventory.Instance != null && HotbarInventory.Instance.IsGroupFullyCollected(ItemGroup.Group2))
            {
                Debug.Log("[IntroController] Group 2 is fully collected! Triggering Puzzle 4-6 chain.");
                StartGroup2PuzzleChain();
            }
            else
            {
                if (!_hasPickedUpPhoto)
                {
                    _currentStage = IntroStage.NearPhoto;
                }
                else if (!_hasPickedUpNovel)
                {
                    _currentStage = IntroStage.NearNovel;
                }
                else if (!_hasPickedUpCassette)
                {
                    _currentStage = IntroStage.NearCassette;
                }
            }
            UpdateInteractablesState();
        }

        private void CheckGroup3Progress()
        {
            if (HotbarInventory.Instance != null && HotbarInventory.Instance.IsGroupFullyCollected(ItemGroup.Group3))
            {
                Debug.Log("[IntroController] Group 3 is fully collected! Triggering Puzzle 7-9 chain.");
                StartGroup3PuzzleChain();
            }
            else
            {
                if (!_hasPickedUpCoffeeMug)
                {
                    _currentStage = IntroStage.NearCoffeeMug;
                }
                else if (!_hasPickedUpPlushRabbit)
                {
                    _currentStage = IntroStage.NearPlushRabbit;
                }
                else if (!_hasPickedUpPillBottle)
                {
                    _currentStage = IntroStage.NearPillBottle;
                }
            }
            UpdateInteractablesState();
        }

        private void StartDeskLampDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectDeskLamp;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);
            TeleportNPCsToActivePlayer();

            List<DialogueNode> nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "My desk lamp…" },
                new DialogueNode { speaker = "Rona", text = "What does it mean to you?" },
                new DialogueNode { speaker = "Kael", text = "I just use it for work" },
                new DialogueNode { speaker = "Murial", text = "Then why is it even here?" },
                new DialogueNode { speaker = "Kael", text = "I guess we gotta find out" },
                new DialogueNode { speaker = "Objective", text = "Collect the desk lamp" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }

            DisableAllOtherInteractablesExcept(_deskLampGo);
        }

        private void OnDeskLampPickedUp()
        {
            StartCoroutine(DeskLampVisionAndDialogueRoutine());
        }

        private IEnumerator DeskLampVisionAndDialogueRoutine()
        {
            _hasPickedUpLamp = true;

            Sprite lampSprite = GetVisionSpriteFromItem(_deskLampGo);

            if (_deskLampGo != null)
            {
                AddItemToInventory(_deskLampGo);
                Interactable inter = _deskLampGo.GetComponent<Interactable>();
                if (inter == null) inter = _deskLampGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_deskLampGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());
            ShowVisionImage(lampSprite);

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "The light that stayed by your side through countless sleepless nights." },
                new DialogueNode { speaker = "Narrator", text = "Yet even it deserves to rest... just like you, Kael." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            HideVisionImage();
            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Feanor", text = "These objects... They aren't random." },
                new DialogueNode { speaker = "Murial", text = "They're fragments of the same memory." },
                new DialogueNode { speaker = "Kael", text = "...I remember this night." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            CheckGroupCollectionProgress();
        }

        private void StartFilesDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectFiles;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);
            TeleportNPCsToActivePlayer();

            List<DialogueNode> nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "These files..." },
                new DialogueNode { speaker = "Murial", text = "They seem important." },
                new DialogueNode { speaker = "Kael", text = "I remember spending countless nights working on them." },
                new DialogueNode { speaker = "Rona", text = "Were they really worth losing your sleep for?" },
                new DialogueNode { speaker = "Kael", text = "...At that time, I thought they were." },
                new DialogueNode { speaker = "Objective", text = "Collect the scattered files" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }

            // Unlock ONLY the files for interaction (always visible, just non-interactable)
            DisableAllOtherInteractablesExcept(_filesGo);
        }

        private void OnFilesPickedUp()
        {
            StartCoroutine(FilesVisionAndDialogueRoutine());
        }

        private IEnumerator FilesVisionAndDialogueRoutine()
        {
            _hasPickedUpFiles = true;

            Sprite filesSprite = GetVisionSpriteFromItem(_filesGo);

            if (_filesGo != null)
            {
                AddItemToInventory(_filesGo);
                Interactable inter = _filesGo.GetComponent<Interactable>();
                if (inter == null) inter = _filesGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_filesGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());
            ShowVisionImage(filesSprite);

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Piles of unfinished work covered the desk, every page carried another deadline." },
                new DialogueNode { speaker = "Narrator", text = "No matter how many were completed... More always seemed to take their place." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            HideVisionImage();

            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Rona", text = "They weren't just papers..." },
                new DialogueNode { speaker = "Feanor", text = "They became a burden you carried every night." },
                new DialogueNode { speaker = "Kael", text = "…" },
                new DialogueNode { speaker = "Murial", text = "What can these fragments of things do?" },
                new DialogueNode { speaker = "Keiko", text = "I feel it’s telling us something." },
                new DialogueNode { speaker = "Feanor", text = "If we piece these fragments, it tells us a story…" },
                new DialogueNode { speaker = "Kael", text = "I understand now…" },
                new DialogueNode { speaker = "Objective", text = "Reconstruct memory" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            _currentStage = IntroStage.ReconstructMemoryPuzzle1;
            CheckGroupCollectionProgress();
        }

        public void OnMemoryReconstructed()
        {
            if (_currentStage == IntroStage.ReconstructMemoryPuzzle3)
            {
                StartCoroutine(MemoryReconstructed3Routine());
            }
            else if (_currentStage == IntroStage.ReconstructMemoryPuzzle2)
            {
                StartCoroutine(MemoryReconstructed2Routine());
            }
            else
            {
                StartCoroutine(MemoryReconstructedVisionAndDialogueRoutine());
            }
        }

        private IEnumerator MemoryReconstructedVisionAndDialogueRoutine()
        {
            _hasReconstructedMemory = true;
            Debug.Log("[HippocampusIntroController] Memory 1 Reconstructed! Displaying full.PNG cutscene...");

            Sprite puzzleSprite = null;
#if UNITY_EDITOR
            puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full.PNG");
            if (puzzleSprite == null) puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full.png");
#endif
            if (puzzleSprite == null) puzzleSprite = Resources.Load<Sprite>("full");

            if (_reconstructTriggerGo != null)
            {
                Interactable inter = _reconstructTriggerGo.GetComponent<Interactable>();
                if (inter == null) inter = _reconstructTriggerGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_reconstructTriggerGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());
            ShowVisionImage(puzzleSprite);

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Beneath the dim glow of the desk lamp, Kael lay awake with his phone in hand." },
                new DialogueNode { speaker = "Narrator", text = "The alarm clock rang again and again, but he never reached for it." },
                new DialogueNode { speaker = "Narrator", text = "Unfinished files piled up around the room." },
                new DialogueNode { speaker = "Narrator", text = "Night after night, sleep slipped further away." },
                new DialogueNode { speaker = "Narrator", text = "Before he realized it, staying awake had become his normal." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            HideVisionImage();
            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "... I never realized it was that bad…" },
                new DialogueNode { speaker = "Keiko", text = "When you stop caring for yourself Kael, your own mind starts crumbling little by little…" },
                new DialogueNode { speaker = "Murial", text = "You may have not felt it, but we felt it Kael, and it has been a nightmare" },
                new DialogueNode { speaker = "Keiko", text = "It’s good that you understand now Kael" },
                new DialogueNode { speaker = "Kael", text = "I understand now." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            // Trigger physical world shake and SFX
            TriggerWorldShake(2.5f, 0.4f);
            yield return new WaitForSeconds(1.5f);

            List<DialogueNode> postShakeNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Rona", text = "We have little time now, let’s head to the next island." },
                new DialogueNode { speaker = "Objective", text = "Explore the Memory Archives" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(postShakeNodes);
            }

            yield return WaitForConversation();

            Debug.Log("[HippocampusIntroController] Activating Bridge 2 to Memory Archives...");
            SmoothBridgeController sbc = GetComponent<SmoothBridgeController>();
            if (sbc == null) sbc = FindAnyObjectByType<SmoothBridgeController>();
            if (sbc != null)
            {
                sbc.TriggerBridge2();
            }
            else
            {
                Debug.LogError("[HippocampusIntroController] Could not find SmoothBridgeController in scene to activate Bridge 2!");
            }

            _currentStage = IntroStage.ExploreMemoryArchives;
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
        }

        private IEnumerator MemoryReconstructed2Routine()
        {
            _hasReconstructedMemory = true;
            Debug.Log("[HippocampusIntroController] Memory 2 Reconstructed! Displaying full2.PNG cutscene...");

            Sprite puzzleSprite = null;
#if UNITY_EDITOR
            puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full2.PNG");
            if (puzzleSprite == null) puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full2.png");
            if (puzzleSprite == null) puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full.PNG");
#endif
            if (puzzleSprite == null) puzzleSprite = Resources.Load<Sprite>("full2");
            if (puzzleSprite == null) puzzleSprite = Resources.Load<Sprite>("full");

            if (_reconstruct2TriggerGo != null)
            {
                Interactable inter = _reconstruct2TriggerGo.GetComponent<Interactable>();
                if (inter == null) inter = _reconstruct2TriggerGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_reconstruct2TriggerGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());
            ShowVisionImage(puzzleSprite);

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Soft music filled the room." },
                new DialogueNode { speaker = "Narrator", text = "A novel rested in Kael's hands while laughter echoed behind him." },
                new DialogueNode { speaker = "Narrator", text = "Familiar faces stood nearby... though their features slowly faded into nothingness." },
                new DialogueNode { speaker = "Narrator", text = "Happiness remained in the memory, even as the people within it slipped away." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            HideVisionImage();
            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "So these are just memories of my past?" },
                new DialogueNode { speaker = "Rona", text = "No, Kael. They are pieces of who you are." },
                new DialogueNode { speaker = "Keiko", text = "The things you love... the moments you cared about... they're still part of you." },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Kael", text = "You’re right, I shouldn't have lost myself to this insomnia." },
                new DialogueNode { speaker = "Murial", text = "Good thing you realized it, now let’s hurry up to the last Island!" },
                new DialogueNode { speaker = "Objective", text = "Explore Anxiety Heights" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            Debug.Log("[HippocampusIntroController] Activating Bridge 3 to Anxiety Heights...");
            SmoothBridgeController sbc2 = GetComponent<SmoothBridgeController>();
            if (sbc2 == null) sbc2 = FindAnyObjectByType<SmoothBridgeController>();
            if (sbc2 != null)
            {
                sbc2.TriggerBridge3();
            }
            else
            {
                Debug.LogError("[HippocampusIntroController] Could not find SmoothBridgeController in scene to activate Bridge 3!");
            }

            _currentStage = IntroStage.ExploreAnxietyHeights;
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
        }

        private IEnumerator MemoryReconstructed3Routine()
        {
            _hasReconstructedMemory = true;
            Debug.Log("[HippocampusIntroController] Memory 3 Reconstructed! Displaying full3.PNG cutscene...");

            Sprite puzzleSprite = null;
#if UNITY_EDITOR
            puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full3.PNG");
            if (puzzleSprite == null) puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full3.png");
            if (puzzleSprite == null) puzzleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Maps/CHAPT2/full.PNG");
#endif
            if (puzzleSprite == null) puzzleSprite = Resources.Load<Sprite>("full3");
            if (puzzleSprite == null) puzzleSprite = Resources.Load<Sprite>("full");

            if (_reconstruct3TriggerGo != null)
            {
                Interactable inter = _reconstruct3TriggerGo.GetComponent<Interactable>();
                if (inter == null) inter = _reconstruct3TriggerGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_reconstruct3TriggerGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());
            ShowVisionImage(puzzleSprite);

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Night after night, the same cycle repeated." },
                new DialogueNode { speaker = "Narrator", text = "Coffee kept Kael awake when his body begged for rest." },
                new DialogueNode { speaker = "Narrator", text = "Sleeping pills became his way to force the night to end." },
                new DialogueNode { speaker = "Narrator", text = "The little rabbit stayed beside him, holding the memories he refused to let go." },
                new DialogueNode { speaker = "Narrator", text = "Day after day, Kael continued the same routine... until exhaustion became something he accepted as normal." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            HideVisionImage();
            yield return StartCoroutine(FadeBackdropOut());

             List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Kael", text = "I didn't realize I was doing that every day." },
                new DialogueNode { speaker = "Keiko", text = "Kael..." },
                new DialogueNode { speaker = "Keiko", text = "You were stuck in the same cycle for so long." },
                new DialogueNode { speaker = "Kael", text = "It was just a phase." },
                new DialogueNode { speaker = "Murial", text = "You couldn't sleep. You relied on coffee. You needed pills just to rest." },
                new DialogueNode { speaker = "Murial", text = "And you still call it a phase?" },
                new DialogueNode { speaker = "Kael", text = "Because it was." },
                new DialogueNode { speaker = "Murial", text = "No, it wasn't." },
                new DialogueNode { speaker = "Kael", text = "You don't understand." },
                new DialogueNode { speaker = "Murial", text = "Then explain it." },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Murial", text = "Explain why you kept hurting yourself and pretending nothing was wrong." },
                new DialogueNode { speaker = "Kael", text = "I wasn't hurting myself." },
                new DialogueNode { speaker = "Murial", text = "Then what do you call this?" },
                new DialogueNode { speaker = "Kael", text = "I was trying to move forward." },
                new DialogueNode { speaker = "Murial", text = "No." },
                new DialogueNode { speaker = "Murial", text = "You were running away." },
                new DialogueNode { speaker = "Kael", text = "Shut up." },
                new DialogueNode { speaker = "Murial", text = "You're still doing it." },
                new DialogueNode { speaker = "Kael", text = "Doing what?" },
                new DialogueNode { speaker = "Murial", text = "Refusing to admit that something was wrong." },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Rona", text = "Murial, stop." },
                new DialogueNode { speaker = "Murial", text = "No." },
                new DialogueNode { speaker = "Murial", text = "He needs to understand." },
                new DialogueNode { speaker = "Kael", text = "You think you know everything about me?" },
                new DialogueNode { speaker = "Murial", text = "No." },
                new DialogueNode { speaker = "Murial", text = "That's the problem." },
                new DialogueNode { speaker = "Murial", text = "We know what you did, Kael." },
                new DialogueNode { speaker = "Murial", text = "But you won't even admit why you did it." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            // Trigger physical world shake and SFX
            TriggerWorldShake(2.5f, 0.4f);
            yield return new WaitForSeconds(1.5f);

            List<DialogueNode> postShakeNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Keiko", text = "The island..." },
                new DialogueNode { speaker = "Feanor", text = "The memory is destabilizing." },
                new DialogueNode { speaker = "Rona", text = "Kael, we need to go." },
                new DialogueNode { speaker = "Murial", text = "You found the truth." },
                new DialogueNode { speaker = "Murial", text = "But you still chose to deny it." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(postShakeNodes);
            }

            yield return WaitForConversation();

            // Spawn Ferry on the main island & lock its position
            if (_ferryNpc != null)
            {
                _ferryNpc.SetActive(true);
                if (_mainIslandFerrySpawn != null)
                {
                    _ferryInitialPos = _mainIslandFerrySpawn.position;
                    _ferryInitialRot = _mainIslandFerrySpawn.rotation;
                }
                _ferryNpc.transform.position = _ferryInitialPos;
                _ferryNpc.transform.rotation = _ferryInitialRot;
                _isFerryActiveInCutscene = true;
            }

            // Start continuous ambient world earthquake
            _isContinuousEarthquake = true;
            _continuousShakeMagnitude = 0.15f;

            _currentStage = IntroStage.GoBackToMainIsland;
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            List<DialogueNode> returnObjective = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Objective", text = "Return to Ferry on the main island" }
            };
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(returnObjective);
            }
        }

        private void StartMainIslandFerryCutscene()
        {
            StartCoroutine(MainIslandFerryCutsceneRoutine());
        }

        private IEnumerator MainIslandFerryCutsceneRoutine()
        {
            _currentStage = IntroStage.MainIslandFerryCutscene;
            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            // Teleport 4 NPCs to dynamic positions facing Ferry
            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer != null)
            {
                Vector3 center = activePlayer.position;
                Transform lookTarget = _ferryNpc != null ? _ferryNpc.transform : activePlayer;
                TeleportAndOrientNPC(_keikoNpc, center + new Vector3(-1.8f, 0f, 1.8f), lookTarget);
                TeleportAndOrientNPC(_ronaNpc, center + new Vector3(1.8f, 0f, 1.8f), lookTarget);
                TeleportAndOrientNPC(_murialNpc, center + new Vector3(-1.8f, 0f, -1.8f), lookTarget);
                TeleportAndOrientNPC(_feanorNpc, center + new Vector3(1.8f, 0f, -1.8f), lookTarget);
            }

            List<DialogueNode> part1Nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "Ferry? You’re back?" },
                new DialogueNode { speaker = "Ferry", text = "I’m sorry…" },
                new DialogueNode { speaker = "Ferry", text = "You were so close. Kael…" },
                new DialogueNode { speaker = "Rona", text = "What do you mean?" },
                new DialogueNode { speaker = "Feanor", text = "One of the pillars…" },
                new DialogueNode { speaker = "Feanor", text = "It’s not lighten up" },
                new DialogueNode { speaker = "Murial", text = "It’s because of you Kael!" },
                new DialogueNode { speaker = "Kael", text = "What does it have to do with me?" },
                new DialogueNode { speaker = "Murial", text = "You found the memory." },
                new DialogueNode { speaker = "Murial", text = "But you still refused to accept it." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part1Nodes);
            }

            yield return WaitForConversation();

            // Pan camera to wide isometric angle (or _pillarCameraTarget if assigned)
            if (Camera.main != null)
            {
                var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
                if (brain != null) brain.enabled = false;

                if (_pillarCameraTarget != null)
                {
                    yield return StartCoroutine(PanCameraToTargetRoutine(_pillarCameraTarget, Vector3.zero));
                }
                else
                {
                    Vector3 center = activePlayer != null ? activePlayer.position : Vector3.zero;
                    Vector3 isoPos = center + new Vector3(0f, 16f, -18f);
                    Quaternion isoRot = Quaternion.Euler(40f, 0f, 0f);
                    Camera.main.transform.position = isoPos;
                    Camera.main.transform.rotation = isoRot;
                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            List<DialogueNode> part2Nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Feanor", text = "The balance is breaking." },
                new DialogueNode { speaker = "Rona", text = "Kael, stay calm." },
                new DialogueNode { speaker = "Rona", text = "You're losing control." },
                new DialogueNode { speaker = "Kael", text = "I can't…" },
                new DialogueNode { speaker = "Keiko", text = "Kael, don't let your fear consume you." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part2Nodes);
            }

            yield return WaitForConversation();

            // SFX: Light subtle shaking starts right here!
            _continuousShakeMagnitude = 0.18f;
            TriggerWorldShake(3.0f, 0.3f);
            yield return new WaitForSeconds(1.0f);

            List<DialogueNode> part3Nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Murial", text = "This is exactly what happened." },
                new DialogueNode { speaker = "Murial", text = "You keep running from it." },
                new DialogueNode { speaker = "Kael", text = "…" },
                new DialogueNode { speaker = "Ferry", text = "I’m sorry, I can't hold it in anymore." },
                new DialogueNode { speaker = "Ferry", text = "Kael... don't lose yourself." },
                new DialogueNode { speaker = "Ferry", text = "Face it." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part3Nodes);
            }

            yield return WaitForConversation();

            // Start Island Gravity Collapse Routine
            yield return StartCoroutine(IslandsFallingCutsceneRoutine());
        }

        private IEnumerator IslandsFallingCutsceneRoutine()
        {
            _currentStage = IntroStage.IslandsFallingCutscene;
            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            // Keep shake subtle during fall
            _continuousShakeMagnitude = 0.12f;

            // Disable all KillTrigger/KillZone scripts so player doesn't get teleported back up
            MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var s in allScripts)
            {
                if (s != null && (s.GetType().Name.Contains("KillTrigger") || s.GetType().Name.Contains("KillZone")))
                {
                    s.enabled = false;
                }
            }

            // Fixed camera framing (isometric wide shot looking down at island)
            if (Camera.main != null)
            {
                var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
                if (brain != null) brain.enabled = false;

                if (_fallingCameraTarget != null)
                {
                    Camera.main.transform.position = _fallingCameraTarget.position;
                    Camera.main.transform.rotation = _fallingCameraTarget.rotation;
                }
                else
                {
                    Transform playerT = FindActivePlayerTransform();
                    Vector3 center = playerT != null ? playerT.position : Vector3.zero;
                    Camera.main.transform.position = center + new Vector3(0f, 16f, -18f);
                    Camera.main.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
                }
            }

            // Collect all falling targets (islands, forcefields, Ferry, NPCs, player, Fog)
            List<Transform> targets = new List<Transform>();
            foreach (var go in _fallingObjects)
            {
                if (go != null) targets.Add(go.transform);
            }

            // Auto-find Fog (2) or Fog if not manually assigned
            GameObject fogObj = GameObject.Find("Fog (2)");
            if (fogObj == null) fogObj = GameObject.Find("Fog");
            if (fogObj != null && !targets.Contains(fogObj.transform)) targets.Add(fogObj.transform);

            if (_ferryNpc != null && !targets.Contains(_ferryNpc.transform)) targets.Add(_ferryNpc.transform);
            if (_keikoNpc != null && !targets.Contains(_keikoNpc.transform)) targets.Add(_keikoNpc.transform);
            if (_ronaNpc != null && !targets.Contains(_ronaNpc.transform)) targets.Add(_ronaNpc.transform);
            if (_murialNpc != null && !targets.Contains(_murialNpc.transform)) targets.Add(_murialNpc.transform);
            if (_feanorNpc != null && !targets.Contains(_feanorNpc.transform)) targets.Add(_feanorNpc.transform);

            Transform playerTransform = FindActivePlayerTransform();
            if (playerTransform != null && !targets.Contains(playerTransform)) targets.Add(playerTransform);

            float duration = 4.0f;
            float elapsed = 0f;
            float gravityAccel = 9.8f;
            float currentVelocity = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                currentVelocity += gravityAccel * Time.deltaTime;
                Vector3 deltaFall = Vector3.down * (currentVelocity * Time.deltaTime);

                foreach (var t in targets)
                {
                    if (t != null)
                    {
                        t.position += deltaFall;
                    }
                }

                yield return null;
            }

            _isContinuousEarthquake = false;

            // Fade screen to black
            if (Nemuri.UI.ScreenFader.Instance != null)
            {
                yield return Nemuri.UI.ScreenFader.Instance.FadeToBlack(2.0f);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            // Transition to Chapter 3
            Debug.Log("[HippocampusIntroController] Chapter 2 Complete! Loading next scene: " + _nextSceneName);
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }

        // ==================== ISLAND 2 DIALOGUE & VISION METHODS ====================

        private void StartIsland2Dialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.NearPhoto;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);
            TeleportNPCsToActivePlayer();

            List<DialogueNode> nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Murial", text = "A big bookshelf?" },
                new DialogueNode { speaker = "Kael", text = "What could a bookshelf possibly be connected to my memories?" },
                new DialogueNode { speaker = "Rona", text = "I guess we better find out for ourselves" },
                new DialogueNode { speaker = "Objective", text = "Collect the faded photograph" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }

            StartCoroutine(WaitForIsland2DialogueEnd());
        }

        private IEnumerator WaitForIsland2DialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
        }

        private void StartPhotoDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectPhoto;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            List<DialogueNode> dialogue = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "My family..." },
                new DialogueNode { speaker = "Keiko", text = "The photograph is fading." },
                new DialogueNode { speaker = "Kael", text = "I don't remember it looking like this." },
                new DialogueNode { speaker = "Objective", text = "Collect the faded photograph" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogue);
            }

            StartCoroutine(WaitForPhotoDialogueEnd());
        }

        private IEnumerator WaitForPhotoDialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
            DisableAllOtherInteractablesExcept(_photoGo);
        }

        private void OnPhotoPickedUp()
        {
            StartCoroutine(PhotoVisionAndDialogueRoutine());
        }

        private IEnumerator PhotoVisionAndDialogueRoutine()
        {
            _hasPickedUpPhoto = true;
            Sprite visionSprite = GetVisionSpriteFromItem(_photoGo);

            if (_photoGo != null)
            {
                AddItemToInventory(_photoGo);
                Interactable inter = _photoGo.GetComponent<Interactable>();
                if (inter == null) inter = _photoGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_photoGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());

            if (_customVisionImageUI != null && visionSprite != null)
            {
                _customVisionImageUI.sprite = visionSprite;
                _customVisionImageUI.color = Color.white;
                _customVisionImageUI.gameObject.SetActive(true);
            }

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Some memories don't disappear overnight." },
                new DialogueNode { speaker = "Narrator", text = "They slowly fade... when they're no longer revisited." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

            yield return StartCoroutine(FadeBackdropOut());

             List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "I haven't looked at this photo in a long time…" },
                new DialogueNode { speaker = "Kael", text = "I miss them so much…" },
                new DialogueNode { speaker = "Keiko", text = "Kael is making me sad" },
                new DialogueNode { speaker = "Rona", text = "Kael, even if they are not here anymore, they are still watching you" },
                new DialogueNode { speaker = "Murial", text = "You say you miss them, but you’re slowly forgetting everything they gave you." },
                new DialogueNode { speaker = "Murial", text = "Do you really think they would be proud of the way you're living now?" },
                new DialogueNode { speaker = "Kael", text = "Sorry…" },
                new DialogueNode { speaker = "Objective", text = "Collect the novel" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            CheckGroup2Progress();
        }

        private void StartNovelDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectNovel;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            List<DialogueNode> dialogue = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "My favorite novel..." },
                new DialogueNode { speaker = "Feanor", text = "You used to read this often?" },
                new DialogueNode { speaker = "Kael", text = "Yeah…" },
                new DialogueNode { speaker = "Objective", text = "Collect the novel" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogue);
            }

            StartCoroutine(WaitForNovelDialogueEnd());
        }

        private IEnumerator WaitForNovelDialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
            DisableAllOtherInteractablesExcept(_novelGo);
        }

        private void OnNovelPickedUp()
        {
            StartCoroutine(NovelVisionAndDialogueRoutine());
        }

        private IEnumerator NovelVisionAndDialogueRoutine()
        {
            _hasPickedUpNovel = true;
            Sprite visionSprite = GetVisionSpriteFromItem(_novelGo);

            if (_novelGo != null)
            {
                AddItemToInventory(_novelGo);
                Interactable inter = _novelGo.GetComponent<Interactable>();
                if (inter == null) inter = _novelGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_novelGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());

            if (_customVisionImageUI != null && visionSprite != null)
            {
                _customVisionImageUI.sprite = visionSprite;
                _customVisionImageUI.color = Color.white;
                _customVisionImageUI.gameObject.SetActive(true);
            }

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Kael used to love reading novels." },
                new DialogueNode { speaker = "Narrator", text = "They were his favorite way to spend time, lost in worlds beyond his own." },
                new DialogueNode { speaker = "Narrator", text = "But as insomnia consumed his nights, he slowly forgot the joy those stories once gave him." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Feanor", text = "I remember this one." },
                new DialogueNode { speaker = "Kael", text = "Wait... you actually read it?" },
                new DialogueNode { speaker = "Feanor", text = "Of course. It was a good novel." },
                new DialogueNode { speaker = "Feanor", text = "I enjoyed reading it as much as you did." },
                new DialogueNode { speaker = "Rona", text = "hahaha i remembered Feanor being so into it when you start reading novels" },
                new DialogueNode { speaker = "Murial", text = "He actually had a personality for once" },
                new DialogueNode { speaker = "Feanor", text = "Whatever" },
                new DialogueNode { speaker = "Objective", text = "Collect the cassette tape" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            CheckGroup2Progress();
        }

        private void StartCassetteDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectCassette;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            List<DialogueNode> dialogue = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "This cassette..." },
                new DialogueNode { speaker = "Murial", text = "It looks well used." },
                new DialogueNode { speaker = "Kael", text = "It was my favorite playlist." },
                new DialogueNode { speaker = "Objective", text = "Collect the cassette tape" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogue);
            }

            StartCoroutine(WaitForCassetteDialogueEnd());
        }

        private IEnumerator WaitForCassetteDialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
            DisableAllOtherInteractablesExcept(_cassetteGo);
        }

        private void OnCassettePickedUp()
        {
            StartCoroutine(CassetteVisionAndDialogueRoutine());
        }

        private IEnumerator CassetteVisionAndDialogueRoutine()
        {
            _hasPickedUpCassette = true;
            Sprite visionSprite = GetVisionSpriteFromItem(_cassetteGo);

            if (_cassetteGo != null)
            {
                AddItemToInventory(_cassetteGo);
                Interactable inter = _cassetteGo.GetComponent<Interactable>();
                if (inter == null) inter = _cassetteGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_cassetteGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());

            if (_customVisionImageUI != null && visionSprite != null)
            {
                _customVisionImageUI.sprite = visionSprite;
                _customVisionImageUI.color = Color.white;
                _customVisionImageUI.gameObject.SetActive(true);
            }

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "Kael had a favorite cassette tape, a playlist that always brought him comfort." },
                new DialogueNode { speaker = "Narrator", text = "It was part of his little routines and moments of peace." },
                new DialogueNode { speaker = "Narrator", text = "Even the things he loved began to feel meaningless." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Keiko", text = "This memory feels... warm." },
                new DialogueNode { speaker = "Kael", text = "I almost forgot what it felt like." },
                new DialogueNode { speaker = "Keiko", text = "I really loved the songs Kael" },
                new DialogueNode { speaker = "Murial", text = "Keiko have really lost touch of her muse when you stopped" },
                new DialogueNode { speaker = "Keiko", text = "Don’t say that Murial, you’d make him feel bad!" },
                new DialogueNode { speaker = "Objective", text = "Reconstruct memory" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            CheckGroup2Progress();
        }

        // ==================== ISLAND 3 DIALOGUE & VISION METHODS ====================

        private void StartIsland3Dialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.NearCoffeeMug;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);
            TeleportNPCsToActivePlayer();

            List<DialogueNode> nodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Keiko", text = "This place feels different..." },
                new DialogueNode { speaker = "Feanor", text = "The other islands showed his memories." },
                new DialogueNode { speaker = "Murial", text = "But this one..." },
                new DialogueNode { speaker = "Murial", text = "It feels like we're inside his thoughts." },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Kael", text = "So this is what kept me awake all this time." },
                new DialogueNode { speaker = "Objective", text = "Collect the coffee mug" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }

            StartCoroutine(WaitForIsland3DialogueEnd());
        }

        private IEnumerator WaitForIsland3DialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
        }

        private void StartCoffeeMugDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectCoffeeMug;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            List<DialogueNode> dialogue = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "My coffee mug..." },
                new DialogueNode { speaker = "Murial", text = "It looks like you've used it a lot." },
                new DialogueNode { speaker = "Kael", text = "I did." },
                new DialogueNode { speaker = "Kael", text = "I drank coffee every night just to stay awake." },
                new DialogueNode { speaker = "Objective", text = "Collect the coffee mug" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogue);
            }

            StartCoroutine(WaitForCoffeeMugDialogueEnd());
        }

        private IEnumerator WaitForCoffeeMugDialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
            DisableAllOtherInteractablesExcept(_coffeeMugGo);
        }

        private void OnCoffeeMugPickedUp()
        {
            StartCoroutine(CoffeeMugVisionAndDialogueRoutine());
        }

        private IEnumerator CoffeeMugVisionAndDialogueRoutine()
        {
            _hasPickedUpCoffeeMug = true;
            Sprite visionSprite = GetVisionSpriteFromItem(_coffeeMugGo);

            if (_coffeeMugGo != null)
            {
                AddItemToInventory(_coffeeMugGo);
                Interactable inter = _coffeeMugGo.GetComponent<Interactable>();
                if (inter == null) inter = _coffeeMugGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_coffeeMugGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());

            if (_customVisionImageUI != null && visionSprite != null)
            {
                _customVisionImageUI.sprite = visionSprite;
                _customVisionImageUI.color = Color.white;
                _customVisionImageUI.gameObject.SetActive(true);
            }

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Rona", text = "So this was what you did every night..." },
                new DialogueNode { speaker = "Kael", text = "I thought it was the only way to keep up." },
                new DialogueNode { speaker = "Rona", text = "But you were only making yourself more tired." },
                new DialogueNode { speaker = "Rona", text = "The more you forced yourself to stay awake, the harder everything became." },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Kael", text = "It wasn't that big of a deal." },
                new DialogueNode { speaker = "Objective", text = "Collect the plush rabbit" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

            yield return StartCoroutine(FadeBackdropOut());

            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            CheckGroup3Progress();
        }

        private void StartPlushRabbitDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectPlushRabbit;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            List<DialogueNode> dialogue = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Rona", text = "A bunny plushie..." },
                new DialogueNode { speaker = "Keiko", text = "It feels familiar." },
                new DialogueNode { speaker = "Kael", text = "That’s Ferry…" },
                new DialogueNode { speaker = "Murial", text = "It’s your sister's plushie isn’t it?" },
                new DialogueNode { speaker = "Kael", text = "Yeah, I kept it after she was gone." },
                new DialogueNode { speaker = "Objective", text = "Collect the plush rabbit" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogue);
            }

            StartCoroutine(WaitForPlushRabbitDialogueEnd());
        }

        private IEnumerator WaitForPlushRabbitDialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
            DisableAllOtherInteractablesExcept(_plushRabbitGo);
        }

        private void OnPlushRabbitPickedUp()
        {
            StartCoroutine(PlushRabbitVisionAndDialogueRoutine());
        }

        private IEnumerator PlushRabbitVisionAndDialogueRoutine()
        {
            _hasPickedUpPlushRabbit = true;
            Sprite visionSprite = GetVisionSpriteFromItem(_plushRabbitGo);

            if (_plushRabbitGo != null)
            {
                AddItemToInventory(_plushRabbitGo);
                Interactable inter = _plushRabbitGo.GetComponent<Interactable>();
                if (inter == null) inter = _plushRabbitGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_plushRabbitGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());

            if (_customVisionImageUI != null && visionSprite != null)
            {
                _customVisionImageUI.sprite = visionSprite;
                _customVisionImageUI.color = Color.white;
                _customVisionImageUI.gameObject.SetActive(true);
            }

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "The plushie became something Kael held onto after losing his sister." },
                new DialogueNode { speaker = "Narrator", text = "It was a reminder of the memories they shared together." },
                new DialogueNode { speaker = "Narrator", text = "Even after all this time, Kael was still afraid of letting go." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Keiko", text = "You kept this because you missed her." },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Kael", text = "It's just a plush rabbit." },
                new DialogueNode { speaker = "Keiko", text = "Kael..." },
                new DialogueNode { speaker = "Kael", text = "I'm fine." },
                new DialogueNode { speaker = "Murial", text = "If it was really just a plush rabbit, why did you keep it all these years?" },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Murial", text = "You know it meant something to you." },
                new DialogueNode { speaker = "Kael", text = "It doesn't matter." },
                new DialogueNode { speaker = "Murial", text = "Then why are you trying so hard to convince us?" },
                new DialogueNode { speaker = "Kael", text = "Just stop." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            // Trigger physical world shake and SFX
            TriggerWorldShake(2.5f, 0.4f);
            yield return new WaitForSeconds(1.5f);

            List<DialogueNode> postShakeNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Rona", text = "Murial, that's enough." },
                new DialogueNode { speaker = "Rona", text = "You're pushing him too hard." },
                new DialogueNode { speaker = "Rona", text = "But Kael..." },
                new DialogueNode { speaker = "Rona", text = "This reaction proves that this memory still affects you." },
                new DialogueNode { speaker = "Rona", text = "And if it affects you, it affects this world too." },
                new DialogueNode { speaker = "Objective", text = "Collect the pill bottle" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(postShakeNodes);
            }

            yield return WaitForConversation();

            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            CheckGroup3Progress();
        }

        private void StartPillBottleDialogue()
        {
            _triggerLocked = true;
            _currentStage = IntroStage.CollectPillBottle;

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            List<DialogueNode> dialogue = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Rona", text = "Kael?" },
                new DialogueNode { speaker = "Kael", text = "I remember this." },
                new DialogueNode { speaker = "Murial", text = "What is it?" },
                new DialogueNode { speaker = "Kael", text = "Something I took when I couldn't sleep." },
                new DialogueNode { speaker = "Murial", text = "You took those?" },
                new DialogueNode { speaker = "Kael", text = "It wasn't a big deal." },
                new DialogueNode { speaker = "Murial", text = "Of course you would say that." },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Murial", text = "You always make everything sound like it doesn't matter." },
                new DialogueNode { speaker = "Objective", text = "Collect the pill bottle" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogue);
            }

            StartCoroutine(WaitForPillBottleDialogueEnd());
        }

        private IEnumerator WaitForPillBottleDialogueEnd()
        {
            yield return WaitForConversation();
            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);
            _triggerLocked = false;
            DisableAllOtherInteractablesExcept(_pillBottleGo);
        }

        private void OnPillBottlePickedUp()
        {
            StartCoroutine(PillBottleVisionAndDialogueRoutine());
        }

        private IEnumerator PillBottleVisionAndDialogueRoutine()
        {
            _hasPickedUpPillBottle = true;
            Sprite visionSprite = GetVisionSpriteFromItem(_pillBottleGo);

            if (_pillBottleGo != null)
            {
                AddItemToInventory(_pillBottleGo);
                Interactable inter = _pillBottleGo.GetComponent<Interactable>();
                if (inter == null) inter = _pillBottleGo.GetComponentInChildren<Interactable>();
                if (inter != null) inter.DismissInteraction();
                Destroy(_pillBottleGo);
            }

            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);

            yield return StartCoroutine(FadeBackdropIn());

            if (_customVisionImageUI != null && visionSprite != null)
            {
                _customVisionImageUI.sprite = visionSprite;
                _customVisionImageUI.color = Color.white;
                _customVisionImageUI.gameObject.SetActive(true);
            }

            List<DialogueNode> visionNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Narrator", text = "When sleep became impossible, Kael searched for anything that could help him rest." },
                new DialogueNode { speaker = "Narrator", text = "But even after the nights became easier to endure" },
                new DialogueNode { speaker = "Narrator", text = "The exhaustion never truly disappeared." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(visionNodes);
            }

            yield return WaitForConversation();

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Murial", text = "So this is what you did every night?" },
                new DialogueNode { speaker = "Kael", text = "..." },
                new DialogueNode { speaker = "Murial", text = "Drinking coffee to stay awake." },
                new DialogueNode { speaker = "Murial", text = "Taking pills just to sleep." },
                new DialogueNode { speaker = "Murial", text = "And you still think everything was fine?" },
                new DialogueNode { speaker = "Kael", text = "Stop acting like you understand." },
                new DialogueNode { speaker = "Murial", text = "I don't." },
                new DialogueNode { speaker = "Murial", text = "That's the problem." },
                new DialogueNode { speaker = "Murial", text = "I know what you did, but I don't understand why you kept letting yourself fall apart." },
                new DialogueNode { speaker = "Objective", text = "Reconstruct memory" }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }

            yield return WaitForConversation();

            SetPlayerMovementEnabled(true);
            SetInventoryLocked(false);

            CheckGroup3Progress();
        }

        private void AddItemToInventory(GameObject itemGo)
        {
            if (itemGo == null) return;
            PickupItem pickup = itemGo.GetComponent<PickupItem>();
            if (pickup == null) pickup = itemGo.GetComponentInChildren<PickupItem>();
            
            if (pickup != null && HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.AddItem(
                    pickup.DisplayName,
                    pickup.ItemIcon,
                    pickup.Description,
                    pickup.Group,
                    pickup.ItemId
                );
            }
        }

        private Sprite GetVisionSpriteFromItem(GameObject go)
        {
            if (go == null) return null;
            PickupItem pickup = go.GetComponent<PickupItem>();
            if (pickup == null) pickup = go.GetComponentInChildren<PickupItem>();
            if (pickup != null)
            {
                var field = typeof(PickupItem).GetField("_visionSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(pickup) as Sprite;
                }
            }
            return null;
        }

        private Image _visionContentImage;

        private void BuildVisionBackdrop()
        {
            GameObject canvasObj = new GameObject("Vision Backdrop Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 848; // above VisionManager (400), below dialogue (900)
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);

            GameObject imgObj = new GameObject("Vision Backdrop");
            imgObj.transform.SetParent(canvasObj.transform, false);
            _visionBackdrop = imgObj.AddComponent<Image>();
            _visionBackdrop.color = new Color(0f, 0f, 0f, 0f);
            
            RectTransform rt = imgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject contentObj = new GameObject("Vision Content Image");
            contentObj.transform.SetParent(imgObj.transform, false);
            _visionContentImage = contentObj.AddComponent<Image>();
            _visionContentImage.color = Color.white;

            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            _visionContentImage.gameObject.SetActive(false);

            imgObj.SetActive(false);
        }

        private void ShowVisionImage(Sprite sprite)
        {
            // All vision popups disabled by design: item pickup leads directly to narrative & dialogue
        }

        private void HideVisionImage()
        {
            if (_visionContentImage != null)
            {
                _visionContentImage.gameObject.SetActive(false);
            }
            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeBackdropIn()
        {
            yield break;
        }

        private IEnumerator FadeBackdropOut()
        {
            yield break;
        }

        private IEnumerator WaitForConversation()
        {
            _conversationEnded = false;
            DialogueManager.OnConversationEnd += OnConversationEnded;
            while (!_conversationEnded)
            {
                yield return null;
            }
            DialogueManager.OnConversationEnd -= OnConversationEnded;
        }

        private void OnConversationEnded()
        {
            _conversationEnded = true;
        }

        private IEnumerator SilentLookAroundRoutine()
        {
            if (Camera.main == null) yield break;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = false;
            }

            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            Vector3 startPos = Camera.main.transform.position;
            Quaternion startRot = Camera.main.transform.rotation;

            // Pan up slightly looking at Circadian Isle clock tower
            Vector3 islandPos = _circadianIsleGo != null ? _circadianIsleGo.transform.position : startPos;
            Vector3 towerTargetPos = startPos + new Vector3(0f, 6f, 2f);
            Vector3 lookDir = (islandPos - towerTargetPos).normalized;
            Quaternion towerTargetRot = Quaternion.LookRotation(lookDir, Vector3.up);

            float elapsed = 0f;
            float duration = 2.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                Camera.main.transform.position = Vector3.Lerp(startPos, towerTargetPos, t);
                Camera.main.transform.rotation = Quaternion.Slerp(startRot, towerTargetRot, t);
                yield return null;
            }

            yield return new WaitForSeconds(1.5f);

            // Pan back to Kael
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                Camera.main.transform.position = Vector3.Lerp(towerTargetPos, startPos, t);
                Camera.main.transform.rotation = Quaternion.Slerp(towerTargetRot, startRot, t);
                yield return null;
            }

            Camera.main.transform.position = startPos;
            Camera.main.transform.rotation = startRot;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = true;
            }

            // Immediately switch stage and activate the Alarm Clock sequence trigger
            _currentStage = IntroStage.NearClockTower;
        }

        private void TriggerWorldShake(float duration, float magnitude)
        {
            AudioClip clip = Resources.Load<AudioClip>("WorldFallApart");
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, 0.8f);
            }
            StartCoroutine(ScreenShakeRoutine(duration, magnitude));
        }

        private IEnumerator ScreenShakeRoutine(float duration, float magnitude)
        {
            Vector3 originalPos = Camera.main.transform.position;
            float elapsed = 0.0f;

            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            bool brainEnabled = brain != null && brain.enabled;
            if (brain != null) brain.enabled = false;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                Camera.main.transform.position = originalPos + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Camera.main.transform.position = originalPos;
            if (brain != null) brain.enabled = brainEnabled;
        }

        private void RotateGroupToCenterSmooth(float duration)
        {
            Transform activePlayer = FindActivePlayerTransform();
            Vector3 sum = Vector3.zero;
            int count = 0;

            if (activePlayer != null) { sum += activePlayer.position; count++; }
            if (_keikoNpc != null && _keikoNpc.activeInHierarchy) { sum += _keikoNpc.transform.position; count++; }
            if (_ronaNpc != null && _ronaNpc.activeInHierarchy) { sum += _ronaNpc.transform.position; count++; }
            if (_murialNpc != null && _murialNpc.activeInHierarchy) { sum += _murialNpc.transform.position; count++; }
            if (_feanorNpc != null && _feanorNpc.activeInHierarchy) { sum += _feanorNpc.transform.position; count++; }

            if (count == 0) return;
            Vector3 center = sum / count;

            if (activePlayer != null) StartCoroutine(SmoothRotateToFacePoint(activePlayer.gameObject, center, duration));
            StartCoroutine(SmoothRotateToFacePoint(_keikoNpc, center, duration));
            StartCoroutine(SmoothRotateToFacePoint(_ronaNpc, center, duration));
            StartCoroutine(SmoothRotateToFacePoint(_murialNpc, center, duration));
            StartCoroutine(SmoothRotateToFacePoint(_feanorNpc, center, duration));
        }

        private IEnumerator SmoothRotateToFacePoint(GameObject go, Vector3 point, float duration)
        {
            if (go == null) yield break;
            Vector3 dir = (point - go.transform.position);
            dir.y = 0f;
            dir.Normalize();
            if (dir == Vector3.zero) yield break;

            Quaternion startRot = go.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            float elapsed = 0f;

            // Deactivate CharacterController temporarily if active to allow rotation smoothly without physics locking
            var cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.GetComponentInChildren<CharacterController>();
            bool ccWasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);
                go.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            go.transform.rotation = targetRot;
            if (cc != null) cc.enabled = ccWasEnabled;
        }

        private void TeleportNPCsToActivePlayer()
        {
            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer == null) return;

            // Teleport Keiko, Rona, Murial, and Feanor to dynamic circle positions facing Kael
            TeleportAndOrientNPC(_keikoNpc, activePlayer.position + new Vector3(-1.8f, 0f, 1.8f), activePlayer);
            TeleportAndOrientNPC(_ronaNpc, activePlayer.position + new Vector3(1.8f, 0f, 1.8f), activePlayer);
            TeleportAndOrientNPC(_murialNpc, activePlayer.position + new Vector3(-1.8f, 0f, -1.8f), activePlayer);
            TeleportAndOrientNPC(_feanorNpc, activePlayer.position + new Vector3(1.8f, 0f, -1.8f), activePlayer);
        }

        private void TeleportAndOrientNPC(GameObject npc, Vector3 position, Transform lookTarget)
        {
            if (npc == null) return;
            Vector3 snappedPos = GetSnappedPosition(position);
            Vector3 dir = (lookTarget.position - snappedPos);
            dir.y = 0f;
            Quaternion rot = dir != Vector3.zero ? Quaternion.LookRotation(dir, Vector3.up) : npc.transform.rotation;

            TeleportObject(npc, snappedPos, rot);
        }

        private void TeleportAndFaceFerry(GameObject npc, Transform point)
        {
            if (npc == null || point == null) return;
            Vector3 targetNpcPos = GetSnappedPosition(point.position);
            Vector3 dir = (_ferryInitialPos - targetNpcPos);
            dir.y = 0f;
            Quaternion targetNpcRot = dir != Vector3.zero ? Quaternion.LookRotation(dir, Vector3.up) : npc.transform.rotation;

            TeleportObject(npc, targetNpcPos, targetNpcRot);
        }

        private void TeleportObject(GameObject go, Vector3 position, Quaternion rotation)
        {
            if (go == null) return;
            var cc = go.GetComponent<CharacterController>();
            if (go.GetComponentInChildren<CharacterController>() != null) cc = go.GetComponentInChildren<CharacterController>();

            bool ccWasEnabled = false;
            if (cc != null)
            {
                ccWasEnabled = cc.enabled;
                cc.enabled = false;
            }

            go.transform.position = position;
            go.transform.rotation = rotation;

            if (cc != null)
            {
                cc.enabled = ccWasEnabled;
            }
        }

        public static float GetGroundHeight(Vector3 position)
        {
            Ray ray = new Ray(new Vector3(position.x, 300.0f, position.z), Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 600f, ~0);

            float bestY = position.y;
            float highestGroundY = -999f;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                GameObject hitObj = hit.collider.gameObject;
                string nameLower = hitObj.name.ToLower();

                // Ignore player, NPCs, and helpers
                if (hitObj.CompareTag("Player") ||
                    hitObj.GetComponentInParent<CharacterController>() != null ||
                    hitObj.GetComponentInParent<PlayerMovementChapt1>() != null ||
                    hitObj.GetComponentInParent<PlayerMovement>() != null ||
                    nameLower.Contains("npc") ||
                    nameLower.Contains("chara") ||
                    nameLower.Contains("player") ||
                    nameLower.Contains("walking") ||
                    nameLower.Contains("land") ||
                    nameLower.Contains("spawn") ||
                    nameLower.Contains("target") ||
                    nameLower.Contains("waypoint") ||
                    nameLower.Contains("roof") ||
                    nameLower.Contains("dome") ||
                    nameLower.Contains("ceiling") ||
                    nameLower.Contains("pillar") ||
                    nameLower.Contains("column") ||
                    nameLower.Contains("arch") ||
                    nameLower.Contains("treetop") ||
                    nameLower.Contains("leaf") ||
                    nameLower.Contains("leaves") ||
                    nameLower.Contains("branch") ||
                    nameLower.Contains("canopy"))
                {
                    continue;
                }

                if (hitObj.layer == 2 || hitObj.layer == 3 || hitObj.layer == 6)
                {
                    continue;
                }

                if (hit.point.y > highestGroundY)
                {
                    highestGroundY = hit.point.y;
                }
            }

            if (highestGroundY > -990f)
            {
                return highestGroundY;
            }
            return bestY;
        }

        private Vector3 GetSnappedPosition(Vector3 rawPos)
        {
            Vector3 snapped = rawPos;
            snapped.y = GetGroundHeight(rawPos);
            return snapped;
        }

        private Transform FindActivePlayerTransform()
        {
            if (PlayerMovementChapt1.Instance != null && PlayerMovementChapt1.Instance.gameObject.activeInHierarchy)
            {
                return PlayerMovementChapt1.Instance.transform;
            }
            if (PlayerMovement.Instance != null && PlayerMovement.Instance.gameObject.activeInHierarchy)
            {
                return PlayerMovement.Instance.transform;
            }

            GameObject defaultPlayer = GameObject.FindWithTag("Player");
            if (defaultPlayer != null && defaultPlayer.activeInHierarchy)
            {
                return defaultPlayer.transform;
            }

            return null;
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

        private void SetInventoryLocked(bool locked)
        {
            if (HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.IsLocked = locked;
            }

            // Disable/Enable the UI script component completely to guarantee nothing is rendered
            HotbarUI ui = FindAnyObjectByType<HotbarUI>();
            if (ui != null)
            {
                ui.enabled = !locked;
            }

            if (_hotbarCanvas == null)
            {
                _hotbarCanvas = _hotbarCanvasOverride != null ? _hotbarCanvasOverride : GameObject.Find("Hotbar Canvas");
            }
            if (_hotbarCanvas != null)
            {
                _hotbarCanvas.SetActive(!locked);
            }
        }

        private void DisableAllOtherInteractables()
        {
            _disabledInteractables.Clear();
            Interactable[] allInteractables = FindObjectsByType<Interactable>(FindObjectsInactive.Include);
            
            // Set of target objects we want to manage/lock
            HashSet<GameObject> targetObjects = new HashSet<GameObject>();
            if (_bagObject != null) targetObjects.Add(_bagObject);
            if (_alarmClockGo != null) targetObjects.Add(_alarmClockGo);
            if (_deskLampGo != null) targetObjects.Add(_deskLampGo);
            if (_filesGo != null) targetObjects.Add(_filesGo);
            if (_reconstructTriggerGo != null) targetObjects.Add(_reconstructTriggerGo);

            if (_photoGo != null) targetObjects.Add(_photoGo);
            if (_novelGo != null) targetObjects.Add(_novelGo);
            if (_cassetteGo != null) targetObjects.Add(_cassetteGo);
            if (_reconstruct2TriggerGo != null) targetObjects.Add(_reconstruct2TriggerGo);

            if (_coffeeMugGo != null) targetObjects.Add(_coffeeMugGo);
            if (_plushRabbitGo != null) targetObjects.Add(_plushRabbitGo);
            if (_pillBottleGo != null) targetObjects.Add(_pillBottleGo);
            if (_reconstruct3TriggerGo != null) targetObjects.Add(_reconstruct3TriggerGo);

            foreach (var inter in allInteractables)
            {
                if (inter != null && inter.enabled)
                {
                    // Disable if it matches our special items (they will be unlocked explicitly when active)
                    if (targetObjects.Contains(inter.gameObject) || targetObjects.Contains(inter.transform.parent != null ? inter.transform.parent.gameObject : null))
                    {
                        inter.enabled = false;
                        _disabledInteractables.Add(inter);
                    }
                }
            }
        }

        private void DisableAllOtherInteractablesExcept(GameObject activeTarget)
        {
            if (activeTarget == null) return;

            // Re-lock all managed interactables
            DisableAllOtherInteractables();

            // Explicitly enable ONLY the current objective target
            Interactable inter = activeTarget.GetComponent<Interactable>();
            if (inter == null) inter = activeTarget.GetComponentInChildren<Interactable>();
            if (inter != null)
            {
                inter.enabled = true;
            }
        }

        private void UpdateInteractablesState()
        {
            // Lock all items first
            DisableAllOtherInteractables();

            // Explicitly enable ONLY the active item for the current stage
            GameObject activeGo = null;
            if (_currentStage == IntroStage.NearClockTower || _currentStage == IntroStage.CollectAlarmClock) activeGo = _alarmClockGo;
            else if (_currentStage == IntroStage.NearDeskLamp || _currentStage == IntroStage.CollectDeskLamp) activeGo = _deskLampGo;
            else if (_currentStage == IntroStage.NearFiles || _currentStage == IntroStage.CollectFiles) activeGo = _filesGo;
            else if (_currentStage == IntroStage.ReconstructMemoryPuzzle1) activeGo = _reconstructTriggerGo;

            else if (_currentStage == IntroStage.NearPhoto || _currentStage == IntroStage.CollectPhoto) activeGo = _photoGo;
            else if (_currentStage == IntroStage.NearNovel || _currentStage == IntroStage.CollectNovel) activeGo = _novelGo;
            else if (_currentStage == IntroStage.NearCassette || _currentStage == IntroStage.CollectCassette) activeGo = _cassetteGo;
            else if (_currentStage == IntroStage.ReconstructMemoryPuzzle2) activeGo = _reconstruct2TriggerGo;

            else if (_currentStage == IntroStage.NearCoffeeMug || _currentStage == IntroStage.CollectCoffeeMug) activeGo = _coffeeMugGo;
            else if (_currentStage == IntroStage.NearPlushRabbit || _currentStage == IntroStage.CollectPlushRabbit) activeGo = _plushRabbitGo;
            else if (_currentStage == IntroStage.NearPillBottle || _currentStage == IntroStage.CollectPillBottle) activeGo = _pillBottleGo;
            else if (_currentStage == IntroStage.ReconstructMemoryPuzzle3) activeGo = _reconstruct3TriggerGo;

            if (activeGo != null)
            {
                Interactable inter = activeGo.GetComponent<Interactable>();
                if (inter == null) inter = activeGo.GetComponentInChildren<Interactable>();
                if (inter != null)
                {
                    inter.enabled = true;
                }
            }
        }

        private void ReEnableInteractables()
        {
            foreach (var inter in _disabledInteractables)
            {
                if (inter != null)
                {
                    inter.enabled = true;
                }
            }
            _disabledInteractables.Clear();

            // Re-enforce strict objective item locking
            UpdateInteractablesState();
        }

        private void SetAllAnimatorsSpeed(float speed)
        {
            Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Include);
            foreach (var anim in animators)
            {
                if (anim != null)
                {
                    // Do not alter Ferry's standard animation speed
                    if (_ferryNpc != null && (anim.gameObject == _ferryNpc || anim.transform.IsChildOf(_ferryNpc.transform)))
                    {
                        continue;
                    }
                    anim.speed = speed;
                }
            }
        }
    }
}
