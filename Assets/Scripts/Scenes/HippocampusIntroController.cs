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
            ReconstructMemoryPuzzle
        }

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

        [Header("Broken Alarm Clock")]
        [SerializeField] private GameObject _alarmClockGo;
        [SerializeField] private GameObject _alarmClockTrigger;

        [Header("Desk Lamp")]
        [SerializeField] private GameObject _deskLampGo;
        [SerializeField] private GameObject _deskLampTrigger;

        [Header("Scattered Files")]
        [SerializeField] private GameObject _filesGo;
        [SerializeField] private GameObject _filesTrigger;

        [Header("Puzzle Target")]
        [SerializeField] private GameObject _reconstructTriggerGo;

        [Header("Island 1 (Circadian Isle) Trigger")]
        [SerializeField] private GameObject _island1Trigger;
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

        public bool IsAtReconstructStage => _currentStage == IntroStage.ReconstructMemoryPuzzle;

        private Vector3 _ferryInitialPos;
        private Quaternion _ferryInitialRot;
        private bool _isFerryActiveInCutscene = false;

        private bool _hasPickedUpBag = false;
        private bool _hasPickedUpClock = false;
        private bool _hasPickedUpLamp = false;
        private bool _hasPickedUpFiles = false;
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
                    
                    // debug log to console so you can trace your distance to the trigger
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
            }
        }

        private void LateUpdate()
        {
            // Force Ferry NPC to stay locked at its design time coordinates when active,
            // bypassing any animator root motion or physics drift.
            if (_isFerryActiveInCutscene && _ferryNpc != null && _ferryNpc.activeSelf)
            {
                _ferryNpc.transform.position = _ferryInitialPos;
                _ferryNpc.transform.rotation = _ferryInitialRot;
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

            Vector3 targetPos = target.position + offset;
            Vector3 lookDir = (target.position - targetPos).normalized;
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
                if (_firstBridgeGo != null)
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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.sprite = clockSprite;
                _customVisionImageUI.gameObject.SetActive(true);
            }

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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

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

            // Transition stage dynamically based on inventory group collection
            CheckGroupCollectionProgress();
        }

        private void CheckGroupCollectionProgress()
        {
            // check if all three items belonging to the group (Group1) are collected in inventory
            if (HotbarInventory.Instance != null && HotbarInventory.Instance.IsGroupFullyCollected(ItemGroup.Group1))
            {
                Debug.Log("[IntroController] Group 1 is fully collected! Unlocking puzzle / memory reconstruction.");
                _currentStage = IntroStage.ReconstructMemoryPuzzle;
                DisableAllOtherInteractablesExcept(_reconstructTriggerGo);
            }
            else
            {
                // Decide which stage to wait for based on what is still missing from the player's inventory
                // This allows collecting items in any sequence/order
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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.sprite = lampSprite;
                _customVisionImageUI.gameObject.SetActive(true);
            }

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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.sprite = filesSprite;
                _customVisionImageUI.gameObject.SetActive(true);
            }

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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

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

            _currentStage = IntroStage.ReconstructMemoryPuzzle;
            CheckGroupCollectionProgress();
        }

        public void OnMemoryReconstructed()
        {
            StartCoroutine(MemoryReconstructedVisionAndDialogueRoutine());
        }

        private IEnumerator MemoryReconstructedVisionAndDialogueRoutine()
        {
            _hasReconstructedMemory = true;

            Sprite puzzleSprite = GetVisionSpriteFromItem(_reconstructTriggerGo);

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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.sprite = puzzleSprite;
                _customVisionImageUI.gameObject.SetActive(true);
            }

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

            if (_customVisionImageUI != null)
            {
                _customVisionImageUI.gameObject.SetActive(false);
            }

            yield return StartCoroutine(FadeBackdropOut());

            TeleportNPCsToActivePlayer();

            List<DialogueNode> dialogNodes = new List<DialogueNode>
            {
                new DialogueNode { speaker = "Kael", text = "... I never realized it was that bad…" },
                new DialogueNode { speaker = "Keiko", text = "When you stop caring for yourself Kael, your own mind starts crumbling little by little…" },
                new DialogueNode { speaker = "Murial", text = "You may have not felt it, but we felt it Kael, and it has been a nightmare" },
                new DialogueNode { speaker = "Keiko", text = "It’s good that you understand now Kael" },
                new DialogueNode { speaker = "Kael", text = "I understand now." },
                new DialogueNode { speaker = "Narrator", text = "SFX World shaking" },
                new DialogueNode { speaker = "Rona", text = "We have little time now, let’s head to the next island." }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(dialogNodes);
            }
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
            
            // Adjust size to be 80% instead of fullscreen
            RectTransform rt = imgObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.1f);
            rt.anchorMax = new Vector2(0.9f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            imgObj.SetActive(false);
        }

        private IEnumerator FadeBackdropIn()
        {
            if (_visionBackdrop == null) yield break;
            _visionBackdrop.gameObject.SetActive(true);
            float elapsed = 0f;
            const float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _visionBackdrop.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _visionBackdrop.color = new Color(0f, 0f, 0f, 1f);
        }

        private IEnumerator FadeBackdropOut()
        {
            if (_visionBackdrop == null) yield break;
            float elapsed = 0f;
            const float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _visionBackdrop.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _visionBackdrop.color = new Color(0f, 0f, 0f, 0f);
            _visionBackdrop.gameObject.SetActive(false);
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
