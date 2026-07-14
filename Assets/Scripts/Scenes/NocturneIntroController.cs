using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nemuri.Dialogue;
using Nemuri.Player;
using Nemuri.Core;

namespace Nemuri.Scenes
{
    public class NocturneIntroController : MonoBehaviour
    {
        private enum IntroState
        {
            InitialWait,
            FirstDialogue,       // Playing nocturne_intro_1 (D1-D15)
            WaitingForGate,      // Playmode (T3 - Swapping to Murial to open gate)
            SecondDialogue,      // Playing nocturne_intro_2 (D16-D18)
            WaitingForKeiko,     // Playmode (T4 - Approach Keiko)
            ThirdDialogue,       // Playing nocturne_intro_3 (D19-D34)
            WaitingForFeanor,    // Playmode (T5 - Approach Feanor)
            FourthDialogue,      // Playing nocturne_intro_4 (D35-D42)
            WaitingForFerry,     // Playmode - Approach Ferry (Bunny)
            FifthDialogue,       // Playing nocturne_intro_5 (D43-D52)
            Completed            // Quest T6 started, crystals unlocked
        }

        [Header("NPC GameObjects")]
        [SerializeField] private GameObject _ronaNpc;
        [SerializeField] private GameObject _murialNpc;
        [SerializeField] private GameObject _keikoNpc;
        [SerializeField] private GameObject _feanorNpc;
        [SerializeField] private GameObject _ferryNpc;

        [Header("Murial Fall Coordinates")]
        [SerializeField] private Transform _murialSpawnPoint;
        [SerializeField] private Transform _murialLandingPoint;
        [SerializeField, Min(0.1f)] private float _fallDuration = 0.8f;

        [Header("Trigger Settings")]
        [SerializeField, Min(0.1f)] private float _dialogueTriggerDistance = 3.5f;

        [Header("Dialogue Assets (nocturne_intro_1 to 5)")]
        [SerializeField] private TextAsset _dialogueJson1;
        [SerializeField] private TextAsset _dialogueJson2;
        [SerializeField] private TextAsset _dialogueJson3;
        [SerializeField] private TextAsset _dialogueJson4;
        [SerializeField] private TextAsset _dialogueJson5;

        [Header("Gate Settings")]
        [SerializeField] private Chapt1gatecontroller _gateController;

        [Header("Crystals to Unlock")]
        [SerializeField] private List<GameObject> _crystals = new List<GameObject>();

        private IntroState _state = IntroState.InitialWait;
        private bool _isMurialFalling = false;

        private void Start()
        {
            // Initial NPC states
            if (_ronaNpc != null) _ronaNpc.SetActive(true); // Rona is present from the start
            if (_keikoNpc != null) _keikoNpc.SetActive(true);
            if (_feanorNpc != null) _feanorNpc.SetActive(true);
            if (_ferryNpc != null) _ferryNpc.SetActive(true);

            // Murial spawn setup
            if (_murialNpc != null)
            {
                if (_murialSpawnPoint != null)
                {
                    _murialNpc.transform.position = _murialSpawnPoint.position;
                    _murialNpc.transform.rotation = _murialSpawnPoint.rotation;
                }
                _murialNpc.SetActive(false); // Hidden until falling
            }

            // Disable gate until T3 objective starts
            if (_gateController != null)
            {
                _gateController.enabled = false;
            }

            // Disable crystals initially
            SetCrystalsInteractable(false);

            // Configure character swapper: initially only characters 1-3 (Kiel, Rona, Murial) are unlocked
            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SetCharacterUnlocked(0, true);  // Kiel
                CharacterSwapManager.Instance.SetCharacterUnlocked(1, true);  // Rona
                CharacterSwapManager.Instance.SetCharacterUnlocked(2, true);  // Murial
                CharacterSwapManager.Instance.SetCharacterUnlocked(3, false); // Keiko (Locked)
                CharacterSwapManager.Instance.SetCharacterUnlocked(4, false); // Feanor (Locked)
            }

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
            switch (_state)
            {
                case IntroState.WaitingForGate:
                    if (_gateController != null && _gateController.isTriggered)
                    {
                        TriggerSecondDialogue();
                    }
                    break;

                case IntroState.WaitingForKeiko:
                    CheckApproach(_keikoNpc, () => TriggerThirdDialogue());
                    break;

                case IntroState.WaitingForFeanor:
                    CheckApproach(_feanorNpc, () => TriggerFourthDialogue());
                    break;

                case IntroState.WaitingForFerry:
                    CheckApproach(_ferryNpc, () => TriggerFifthDialogue());
                    break;
            }
        }

        private IEnumerator IntroStartRoutine()
        {
            SetPlayerMovementEnabled(false);

            // Wait a few seconds before dialogue triggers
            yield return new WaitForSeconds(2f);

            _state = IntroState.FirstDialogue;
            PlayDialogue(_dialogueJson1);
        }

        private void HandleNodeDisplayed(DialogueNode node)
        {
            // Trigger Murial falling when the bushes rustle text is displayed
            if (_state == IntroState.FirstDialogue && !_isMurialFalling &&
                node.speaker == "SFX" && node.text.Contains("bushes rustle"))
            {
                StartCoroutine(MurialFallRoutine());
            }
        }

        private IEnumerator MurialFallRoutine()
        {
            _isMurialFalling = true;

            if (_murialNpc != null)
            {
                _murialNpc.SetActive(true);

                Vector3 startPos = _murialSpawnPoint != null ? _murialSpawnPoint.position : _murialNpc.transform.position;
                Vector3 endPos = _murialLandingPoint != null ? _murialLandingPoint.position : startPos + Vector3.down * 5f;

                float elapsed = 0f;
                while (elapsed < _fallDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / _fallDuration;

                    _murialNpc.transform.position = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
                _murialNpc.transform.position = endPos;
                Debug.Log("[NocturneIntroController] Murial NPC fell from tree!");
            }
        }

        private void TriggerSecondDialogue()
        {
            _state = IntroState.SecondDialogue;
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson2);
        }

        private void TriggerThirdDialogue()
        {
            _state = IntroState.ThirdDialogue;
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson3);
        }

        private void TriggerFourthDialogue()
        {
            _state = IntroState.FourthDialogue;
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson4);
        }

        private void TriggerFifthDialogue()
        {
            _state = IntroState.FifthDialogue;
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson5);
        }

        private void HandleConversationEnd()
        {
            switch (_state)
            {
                case IntroState.FirstDialogue:
                    SetPlayerMovementEnabled(true);
                    
                    // Enable the gate controller so Murial can open it
                    if (_gateController != null)
                    {
                        _gateController.enabled = true;
                    }
                    _state = IntroState.WaitingForGate;
                    break;

                case IntroState.SecondDialogue:
                    SetPlayerMovementEnabled(true);
                    _state = IntroState.WaitingForKeiko;
                    break;

                case IntroState.ThirdDialogue:
                    SetPlayerMovementEnabled(true);
                    _state = IntroState.WaitingForFeanor;
                    break;

                case IntroState.FourthDialogue:
                    SetPlayerMovementEnabled(true);
                    _state = IntroState.WaitingForFerry;
                    break;

                case IntroState.FifthDialogue:
                    SetPlayerMovementEnabled(true);
                    SetCrystalsInteractable(true); // Quest T6 started, crystals can now be collected
                    _state = IntroState.Completed;
                    Debug.Log("[NocturneIntroController] Intro flow sequence fully completed!");
                    break;
            }
        }

        private void CheckApproach(GameObject targetNpc, System.Action onApproach)
        {
            if (targetNpc == null || !targetNpc.activeInHierarchy)
            {
                return;
            }

            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer == null)
            {
                return;
            }

            float distance = Vector3.Distance(activePlayer.position, targetNpc.transform.position);
            if (distance <= _dialogueTriggerDistance)
            {
                onApproach?.Invoke();
            }
        }

        private void PlayDialogue(TextAsset dialogueJson)
        {
            if (dialogueJson != null && DialogueManager.Instance != null)
            {
                DialogueSequence sequence = JsonUtility.FromJson<DialogueSequence>(dialogueJson.text);
                if (sequence != null && sequence.nodes != null)
                {
                    DialogueManager.Instance.StartConversation(sequence.nodes);
                    return;
                }
            }
            Debug.LogWarning($"[NocturneIntroController] Dialogue asset is missing or parsing failed: {dialogueJson?.name}");
        }

        private void SetCrystalsInteractable(bool active)
        {
            if (_crystals == null) return;
            foreach (var crystal in _crystals)
            {
                if (crystal != null)
                {
                    // Disable colliders and interactables to block interaction
                    var colliders = crystal.GetComponentsInChildren<Collider>(true);
                    foreach (var col in colliders)
                    {
                        col.enabled = active;
                    }

                    var interactable = crystal.GetComponent<Interactable>();
                    if (interactable != null)
                    {
                        interactable.enabled = active;
                    }
                }
            }
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
    }
}
