using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nemuri.Dialogue;
using Nemuri.Player;
using Nemuri.Core;
using Nemuri.Interactions;

namespace Nemuri.Scenes
{
    public class NocturneIntroController : MonoBehaviour
    {
        private enum IntroState
        {
            InitialWait,
            FirstDialogue,
            WaitingForApproachVines,
            SecondIntroDialogue,
            WaitingForGate,
            SecondDialogue,
            WaitingForKeiko,
            ThirdDialogue,
            WaitingForFeanor,
            FourthDialogue,
            WaitingForFerry,
            FifthDialogue,
            Completed
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

        [Header("Gate Settings")]
        [SerializeField] private Chapt1gatecontroller _gateController;

        [Header("Crystals to Unlock")]
        [SerializeField] private List<GameObject> _crystals = new List<GameObject>();

        private TextAsset _dialogueJson1;
        private TextAsset _dialogueJson1b;
        private TextAsset _dialogueJson2;
        private TextAsset _dialogueJson3;
        private TextAsset _dialogueJson4;
        private TextAsset _dialogueJson5;

        private IntroState _state = IntroState.InitialWait;
        private bool _isMurialFalling = false;

        private float _currentShakeTime = 0f;
        private float _currentShakeMagnitude = 0f;
        private Vector3 _shakeOffset = Vector3.zero;

        private void Start()
        {
            Debug.Log($"[NocturneIntroController] Start initialized. Rona NPC: {_ronaNpc != null}, Murial NPC: {_murialNpc != null}, Gate: {_gateController != null}");

            if (DialogueManager.Instance == null)
            {
                DialogueManager existingManager = FindAnyObjectByType<DialogueManager>();
                if (existingManager == null)
                {
                    GameObject dmGo = new GameObject("DialogueManager");
                    dmGo.AddComponent<DialogueManager>();
                }
            }

            _dialogueJson1 = Resources.Load<TextAsset>("Dialogue/nocturne_intro_1a");
            _dialogueJson1b = Resources.Load<TextAsset>("Dialogue/nocturne_intro_1b");
            _dialogueJson2 = Resources.Load<TextAsset>("Dialogue/nocturne_intro_2");
            _dialogueJson3 = Resources.Load<TextAsset>("Dialogue/nocturne_intro_3");
            _dialogueJson4 = Resources.Load<TextAsset>("Dialogue/nocturne_intro_4");
            _dialogueJson5 = Resources.Load<TextAsset>("Dialogue/nocturne_intro_5");

            if (_ronaNpc != null) _ronaNpc.SetActive(true);
            if (_keikoNpc != null) _keikoNpc.SetActive(true);
            if (_feanorNpc != null) _feanorNpc.SetActive(true);
            if (_ferryNpc != null) _ferryNpc.SetActive(true);

            ConfigureNpcPhysics(_ronaNpc);
            ConfigureNpcPhysics(_murialNpc);
            ConfigureNpcPhysics(_keikoNpc);
            ConfigureNpcPhysics(_feanorNpc);
            ConfigureNpcPhysics(_ferryNpc);

            SnapToGround(_ronaNpc);
            SnapToGround(_keikoNpc);
            SnapToGround(_feanorNpc);
            SnapToGround(_ferryNpc);

            if (_murialNpc != null)
            {
                Quaternion originalRot = _murialNpc.transform.rotation;
                if (_murialSpawnPoint != null)
                {
                    _murialNpc.transform.position = _murialSpawnPoint.position;
                    _murialNpc.transform.rotation = originalRot;
                }
                _murialNpc.SetActive(false);
            }

            if (_gateController != null)
            {
                _gateController.enabled = false;
            }

            SetCrystalsInteractable(false);

            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SetCharacterUnlocked(0, true);  // Kiel (Unlocked)
                CharacterSwapManager.Instance.SetCharacterUnlocked(1, false); // Rona (Locked)
                CharacterSwapManager.Instance.SetCharacterUnlocked(2, false); // Murial (Locked)
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
                case IntroState.WaitingForApproachVines:
                    if (_ronaNpc != null)
                    {
                        float currentY = _ronaNpc.transform.position.y;
                        Vector3 ronaTarget = new Vector3(-20.06f, currentY, 121.5f);
                        
                        Ray ray = new Ray(new Vector3(ronaTarget.x, currentY + 10f, ronaTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            ronaTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_ronaNpc.transform.position, ronaTarget);
                        if (distToTarget > 0.2f)
                        {
                            _ronaNpc.transform.position = Vector3.MoveTowards(_ronaNpc.transform.position, ronaTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (ronaTarget - _ronaNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _ronaNpc.transform.rotation = Quaternion.Slerp(_ronaNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_ronaNpc, true);
                        }
                        else
                        {
                            SetNpcMoving(_ronaNpc, false);
                            
                            Transform activePlayer = FindActivePlayerTransform();
                            if (activePlayer != null)
                            {
                                Vector3 toPlayer = (activePlayer.position - _ronaNpc.transform.position);
                                toPlayer.y = 0f;
                                toPlayer.Normalize();
                                if (toPlayer != Vector3.zero)
                                {
                                    _ronaNpc.transform.rotation = Quaternion.Slerp(_ronaNpc.transform.rotation, Quaternion.LookRotation(toPlayer, Vector3.up), 5f * Time.deltaTime);
                                }
                            }
                        }
                    }

                    if (_gateController != null)
                    {
                        Transform activePlayer = FindActivePlayerTransform();
                        if (activePlayer != null)
                        {
                            float distToVines = Vector3.Distance(activePlayer.position, _gateController.transform.position);
                            if (distToVines <= 2.2f)
                            {
                                Debug.Log($"[NocturneIntroController] Player approached vines (Distance: {distToVines:F2}). Triggering 1b dialog.");
                                SetNpcMoving(_ronaNpc, false);
                                TriggerSecondIntroDialogue();
                            }
                        }
                    }
                    break;

                case IntroState.WaitingForGate:
                    if (_gateController != null && _gateController.isTriggered)
                    {
                        TriggerSecondDialogue();
                    }
                    break;

                case IntroState.WaitingForKeiko:
                    if (_ronaNpc != null && _keikoNpc != null)
                    {
                        Vector3 ronaTarget = _keikoNpc.transform.position + new Vector3(-3f, 0f, -3f);
                        
                        float currentY = _ronaNpc.transform.position.y;
                        Ray ray = new Ray(new Vector3(ronaTarget.x, currentY + 10f, ronaTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            ronaTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_ronaNpc.transform.position, ronaTarget);
                        if (distToTarget > 0.2f)
                        {
                            _ronaNpc.transform.position = Vector3.MoveTowards(_ronaNpc.transform.position, ronaTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (ronaTarget - _ronaNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _ronaNpc.transform.rotation = Quaternion.Slerp(_ronaNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_ronaNpc, true);
                        }
                        else
                        {
                            SetNpcMoving(_ronaNpc, false);
                            
                            Transform activePlayer = FindActivePlayerTransform();
                            if (activePlayer != null)
                            {
                                Vector3 toPlayer = (activePlayer.position - _ronaNpc.transform.position);
                                toPlayer.y = 0f;
                                toPlayer.Normalize();
                                if (toPlayer != Vector3.zero)
                                {
                                    _ronaNpc.transform.rotation = Quaternion.Slerp(_ronaNpc.transform.rotation, Quaternion.LookRotation(toPlayer, Vector3.up), 5f * Time.deltaTime);
                                }
                            }
                        }
                    }

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

        private void LateUpdate()
        {
            if (_currentShakeTime > 0f)
            {
                _currentShakeTime -= Time.deltaTime;

                float x = Random.Range(-1f, 1f) * _currentShakeMagnitude;
                float y = Random.Range(-1f, 1f) * _currentShakeMagnitude;
                float z = Random.Range(-1f, 1f) * _currentShakeMagnitude;
                _shakeOffset = new Vector3(x, y, z);

                if (Camera.main != null)
                {
                    Camera.main.transform.position += _shakeOffset;
                }
            }
            else
            {
                _shakeOffset = Vector3.zero;
            }
        }

        private IEnumerator IntroStartRoutine()
        {
            SetPlayerMovementEnabled(false);
            yield return new WaitForSeconds(2f);
            _state = IntroState.FirstDialogue;
            PlayDialogue(_dialogueJson1);
        }

        private void HandleNodeDisplayed(DialogueNode node)
        {
            if (node.speaker == "SFX")
            {
                if (node.text.Contains("Slight tremor"))
                {
                    Debug.Log("[NocturneIntroController] Triggering slight tremor shake.");
                    TriggerShake(1.5f, 0.5f);
                }
                else if (node.text.Contains("shakes violently") || node.text.Contains("violent quake"))
                {
                    Debug.Log("[NocturneIntroController] Triggering violent quake shake.");
                    TriggerShake(3.0f, 1.6f);
                }
                else if (node.text.Contains("bushes rustle"))
                {
                    if (_state == IntroState.SecondIntroDialogue && !_isMurialFalling)
                    {
                        StartCoroutine(MurialFallRoutine());
                    }
                }
            }
        }

        private void TriggerShake(float duration, float magnitude)
        {
            _currentShakeTime = duration;
            _currentShakeMagnitude = magnitude;
        }

        private IEnumerator MurialFallRoutine()
        {
            _isMurialFalling = true;

            if (_murialNpc != null)
            {
                _murialNpc.SetActive(true);

                Vector3 startPos = _murialSpawnPoint != null ? _murialSpawnPoint.position : _murialNpc.transform.position;
                Vector3 endPos = _murialLandingPoint != null ? _murialLandingPoint.position : startPos + Vector3.down * 8f;

                float elapsed = 0f;
                while (elapsed < _fallDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / _fallDuration;
                    float tAccel = t * t;

                    _murialNpc.transform.position = Vector3.Lerp(startPos, endPos, tAccel);
                    yield return null;
                }
                _murialNpc.transform.position = endPos;
                Debug.Log("[NocturneIntroController] Murial NPC fell from tree and landed on terrain!");
            }
        }

        private void TriggerSecondIntroDialogue()
        {
            _state = IntroState.SecondIntroDialogue;
            RotateNpcToFacePlayer(_ronaNpc);
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson1b);
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
            RotateNpcToFacePlayer(_keikoNpc);
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson3);
        }

        private void TriggerFourthDialogue()
        {
            _state = IntroState.FourthDialogue;
            RotateNpcToFacePlayer(_feanorNpc);
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson4);
        }

        private void TriggerFifthDialogue()
        {
            _state = IntroState.FifthDialogue;
            RotateNpcToFacePlayer(_ferryNpc);
            SetPlayerMovementEnabled(false);
            PlayDialogue(_dialogueJson5);
        }

        private void HandleConversationEnd()
        {
            switch (_state)
            {
                case IntroState.FirstDialogue:
                    SetPlayerMovementEnabled(true);
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(1, true); // Unlock Rona
                    }
                    _state = IntroState.WaitingForApproachVines;
                    Debug.Log("[NocturneIntroController] Dialogue 1a finished. Waiting for player to approach vines.");
                    break;

                case IntroState.SecondIntroDialogue:
                    SetPlayerMovementEnabled(true);
                    if (_gateController != null)
                    {
                        _gateController.enabled = true;
                    }
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(2, true); // Unlock Murial
                    }
                    _state = IntroState.WaitingForGate;
                    Debug.Log("[NocturneIntroController] Dialogue 1b finished. Waiting for gate interaction.");
                    break;

                case IntroState.SecondDialogue:
                    SetPlayerMovementEnabled(true);
                    _state = IntroState.WaitingForKeiko;
                    break;

                case IntroState.ThirdDialogue:
                    SetPlayerMovementEnabled(true);
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(3, true); // Unlock Keiko
                    }
                    _state = IntroState.WaitingForFeanor;
                    break;

                case IntroState.FourthDialogue:
                    SetPlayerMovementEnabled(true);
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(4, true); // Unlock Feanor
                    }
                    _state = IntroState.WaitingForFerry;
                    break;

                case IntroState.FifthDialogue:
                    SetPlayerMovementEnabled(true);
                    SetCrystalsInteractable(true);
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

        private void SetNpcMoving(GameObject npc, bool moving)
        {
            if (npc == null) return;
            Animator anim = npc.GetComponent<Animator>();
            if (anim == null)
            {
                anim = npc.GetComponentInChildren<Animator>();
            }

            if (anim != null)
            {
                try { anim.SetBool("IsMoving", moving); } catch {}
                try { anim.SetFloat("Speed", moving ? 1f : 0f); } catch {}
            }
        }

        private void ConfigureNpcPhysics(GameObject npc)
        {
            if (npc == null) return;
            
            var rb = npc.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var cc = npc.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
        }

        private void SnapToGround(GameObject npc)
        {
            if (npc == null) return;
            Vector3 pos = npc.transform.position;
            Ray ray = new Ray(pos + Vector3.up * 5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                pos.y = hit.point.y;
                npc.transform.position = pos;
            }
        }

        private void RotateNpcToFacePlayer(GameObject npc)
        {
            if (npc == null) return;
            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer == null) return;

            Vector3 toPlayer = (activePlayer.position - npc.transform.position);
            toPlayer.y = 0f;
            toPlayer.Normalize();
            if (toPlayer != Vector3.zero)
            {
                npc.transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);
            }
        }
    }
}
