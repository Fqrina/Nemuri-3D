using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        [Header("Island 1 (Circadian Isle) Trigger")]
        [SerializeField] private GameObject _island1Trigger;
        [SerializeField] private float _triggerDistance = 5.0f;

        [Header("Audio Configurations")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _ferryPoofSound;

        private Vector3 _ferryInitialPos;
        private Quaternion _ferryInitialRot;
        private bool _isFerryActiveInCutscene = false;

        private bool _isWaitingForExplore = false;
        private bool _island1DialogueTriggered = false;
        private bool _hasPickedUpBag = false;

        private GameObject _hotbarCanvas;
        private List<Interactable> _disabledInteractables = new List<Interactable>();

        private void Awake()
        {
            // Cache reference to hotbar canvas while it is active on Awake
            _hotbarCanvas = _hotbarCanvasOverride != null ? _hotbarCanvasOverride : GameObject.Find("Hotbar Canvas");
        }

        private void Start()
        {
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
        }

        private void Update()
        {
            // Detect player approaching the Circadian Isle trigger point
            if (_isWaitingForExplore && !_island1DialogueTriggered && _island1Trigger != null)
            {
                Transform activePlayer = FindActivePlayerTransform();
                if (activePlayer != null)
                {
                    float distance = Vector3.Distance(activePlayer.position, _island1Trigger.transform.position);
                    if (distance <= _triggerDistance)
                    {
                        StartIsland1Dialogue();
                    }
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

            // Ensure bag starts inactive
            if (_bagObject != null)
            {
                _bagObject.SetActive(false);
            }

            // Disable all other interactables in the scene during dialogue to prevent roaming interactions
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
        }

        private void HandleConversationEnd()
        {
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

            // If the player hasn't picked up the bag yet, keep inventory HUD disabled
            // and keep non-bag interactables blocked, but allow camera movement
            if (!_hasPickedUpBag)
            {
                return;
            }

            // If we are currently in the middle of exploring, do not unlock swapping yet
            if (_isWaitingForExplore && !_island1DialogueTriggered)
            {
                return;
            }

            // Unlock all character slots
            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SetCharacterUnlocked(0, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(1, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(2, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(3, true);
                CharacterSwapManager.Instance.SetCharacterUnlocked(4, true);
            }

            // Restore inventory systems
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

                // Register callback to resume dialogue once the bag is picked up
                Interactable inter = _bagObject.GetComponent<Interactable>();
                if (inter == null) inter = _bagObject.GetComponentInChildren<Interactable>();
                if (inter != null)
                {
                    // Add listener. PickupItem is NOT needed, we handle pickup completely inside OnBagPickedUp.
                    inter.OnInteract.RemoveAllListeners();
                    inter.OnInteract.AddListener(OnBagPickedUp);
                    inter.PromptText = "Press E to Obtain the Bag";
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

            // Add the bag to the inventory database directly
            if (HotbarInventory.Instance != null)
            {
                // We add the item directly, using a placeholder icon or finding a bag icon in the project if desired
                HotbarInventory.Instance.AddItem("Bag", null, "A mysterious bag left by Ferry.");
            }

            // Remove/destroy the bag GameObject so it vanishes
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
            }

            if (DialogueManager.Instance != null)
            {
                // Transition to the next node which will display "Explore The Circadian Isle" objective
                DialogueManager.Instance.ResumeConversation();
                _isWaitingForExplore = true;

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
            _island1DialogueTriggered = true;
            _isWaitingForExplore = false;

            // Lock movement, disable other interactables, lock inventory systems for second cutscene
            SetPlayerMovementEnabled(false);
            SetInventoryLocked(true);
            DisableAllOtherInteractables();

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
            Interactable bagInter = _bagObject != null ? _bagObject.GetComponent<Interactable>() : null;
            if (bagInter == null && _bagObject != null) bagInter = _bagObject.GetComponentInChildren<Interactable>();

            foreach (var inter in allInteractables)
            {
                if (inter != null && inter != bagInter && inter.enabled)
                {
                    inter.enabled = false;
                    _disabledInteractables.Add(inter);
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
        }
    }
}
