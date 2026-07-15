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

        public static NocturneIntroController Instance { get; private set; }
        public static bool IsIntroCompleted { get; private set; } = false;

        public static bool CanSwapTo(int characterIndex)
        {
            if (IsIntroCompleted)
            {
                return true;
            }

            if (Instance != null)
            {
                if (Instance._state == IntroState.WaitingForGate)
                {
                    // Allow Kael (0), Rona (1), and Murial (2) when waiting to lower the gate
                    return characterIndex == 0 || characterIndex == 1 || characterIndex == 2;
                }
            }

            // Otherwise, stay locked to Kael (0)
            return characterIndex == 0;
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

        private GameObject _portalObject;

        // Path indices for NPCs
        private int _ronaPathIndex = 0;
        private int _murialPathIndex = 0;
        private int _keikoPathIndex = 0;
        private int _feanorPathIndex = 0;

        // 1. Rona NPC Path to Keiko (3 Waypoints)
        private List<Vector2> _ronaPath = new List<Vector2>()
        {
            new Vector2(-26.04f, 114.53f),
            new Vector2(-25.47f, 109.22f),
            new Vector2(-20.44f, 104.67f)
        };

        // 2. Murial NPC Path to Keiko (5 Waypoints)
        private List<Vector2> _murialPath = new List<Vector2>()
        {
            new Vector2(-21.8f, 122.85f),
            new Vector2(-21.31f, 119.03f),
            new Vector2(-25.07f, 114.66f),
            new Vector2(-24.86f, 107.11f),
            new Vector2(-19.37f, 106.16f)
        };

        // 3. Keiko NPC Path to Feanor (7 Waypoints)
        private List<Vector2> _keikoPath = new List<Vector2>()
        {
            new Vector2(-14.71f, 103.88f),
            new Vector2(-11.92f, 99.92f),
            new Vector2(-11.92f, 96.04f),
            new Vector2(-18.44f, 92.19f),
            new Vector2(-24.92f, 89.26f),
            new Vector2(-27.61f, 84.22f),
            new Vector2(-27.94f, 74.39f)
        };

        private List<Vector2> _ronaPathToFeanor = new List<Vector2>()
        {
            new Vector2(-16.21f, 104.38f),
            new Vector2(-13.42f, 100.42f),
            new Vector2(-13.42f, 96.54f),
            new Vector2(-19.94f, 92.69f),
            new Vector2(-26.42f, 89.76f),
            new Vector2(-29.11f, 84.72f),
            new Vector2(-29.44f, 74.89f)
        };

        private List<Vector2> _murialPathToFeanor = new List<Vector2>()
        {
            new Vector2(-13.21f, 103.38f),
            new Vector2(-10.42f, 99.42f),
            new Vector2(-10.42f, 95.54f),
            new Vector2(-16.94f, 91.69f),
            new Vector2(-23.42f, 88.76f),
            new Vector2(-26.11f, 83.72f),
            new Vector2(-26.44f, 73.89f)
        };

        // 4. Group Paths to Ferry (8 Waypoints each, sharing the narrow gate at point 6)
        private List<Vector2> _ronaPathToFerry = new List<Vector2>()
        {
            new Vector2(-28.08f, 83.21f),
            new Vector2(-26.88f, 87.65f),
            new Vector2(-24.91f, 89.56f),
            new Vector2(-21.62f, 91.87f),
            new Vector2(-16.36f, 93.07f),
            new Vector2(-15.86f, 89.98f), // Gate (exact)
            new Vector2(-16.4f, 86.63f),  // Offset
            new Vector2(-15.86f, 84.95f)  // Final
        };

        private List<Vector2> _murialPathToFerry = new List<Vector2>()
        {
            new Vector2(-27.08f, 82.21f),
            new Vector2(-25.88f, 86.65f),
            new Vector2(-23.91f, 88.56f),
            new Vector2(-20.62f, 90.87f),
            new Vector2(-15.36f, 92.07f),
            new Vector2(-15.86f, 89.98f), // Gate (exact)
            new Vector2(-14.8f, 86.63f),  // Offset
            new Vector2(-14.6f, 84.8f)    // Final
        };

        private List<Vector2> _keikoPathToFerry = new List<Vector2>()
        {
            new Vector2(-27.78f, 82.91f),
            new Vector2(-26.58f, 87.35f),
            new Vector2(-24.61f, 89.26f),
            new Vector2(-21.32f, 91.57f),
            new Vector2(-16.06f, 92.77f),
            new Vector2(-15.86f, 89.98f), // Gate (exact)
            new Vector2(-15.9f, 86.63f),  // Offset
            new Vector2(-17.57f, 84.38f)  // Final
        };

        private List<Vector2> _feanorPathToFerry = new List<Vector2>()
        {
            new Vector2(-27.38f, 82.51f),
            new Vector2(-26.18f, 86.95f),
            new Vector2(-24.21f, 88.86f),
            new Vector2(-20.92f, 91.17f),
            new Vector2(-15.66f, 92.37f),
            new Vector2(-15.86f, 89.98f), // Gate (exact)
            new Vector2(-15.3f, 86.63f),  // Offset
            new Vector2(-19.48f, 83.77f)  // Final
        };

        private GameObject _crystalObject;

        private List<Vector2> _ronaPathToGem = new List<Vector2>()
        {
            new Vector2(-16.33f, 88.08f),
            new Vector2(-15.83f, 89.96f), // Gate (exact)
            new Vector2(-15.99f, 92.55f),
            new Vector2(-12.36f, 98.18f),
            new Vector2(-12.36f, 105.85f),
            new Vector2(-15.43f, 107.92f), // Path 3 start
            new Vector2(-14.984f, 111.964f) // Path 3 end
        };

        private List<Vector2> _murialPathToGem = new List<Vector2>()
        {
            new Vector2(-15.33f, 87.08f),
            new Vector2(-15.83f, 89.96f), // Gate (exact)
            new Vector2(-14.99f, 91.55f),
            new Vector2(-11.36f, 97.18f),
            new Vector2(-11.36f, 104.85f),
            new Vector2(-15.43f, 107.92f), // Path 4 start
            new Vector2(-14.984f, 111.964f),
            new Vector2(-16.14f, 112.703f) // Path 4 end
        };

        private List<Vector2> _keikoPathToGem = new List<Vector2>()
        {
            new Vector2(-16.03f, 87.78f),
            new Vector2(-15.83f, 89.96f), // Gate (exact)
            new Vector2(-15.69f, 92.25f),
            new Vector2(-12.06f, 97.88f),
            new Vector2(-12.06f, 105.55f),
            new Vector2(-11.86f, 109.19f), // Path 2 start
            new Vector2(-12.085f, 111.217f),
            new Vector2(-14.361f, 112.13f),
            new Vector2(-13.787f, 113.613f) // Path 2 end
        };

        private List<Vector2> _feanorPathToGem = new List<Vector2>()
        {
            new Vector2(-15.63f, 87.38f),
            new Vector2(-15.83f, 89.96f), // Gate (exact)
            new Vector2(-15.29f, 91.85f),
            new Vector2(-11.66f, 97.48f),
            new Vector2(-11.66f, 105.15f),
            new Vector2(-11.86f, 109.19f), // Path 1 start
            new Vector2(-12.085f, 111.217f),
            new Vector2(-14.361f, 112.13f) // Path 1 end
        };

        private void Start()
        {
            Instance = this;
            IsIntroCompleted = false; // Reset lock on start

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

            if (Camera.main != null)
            {
                if (Camera.main.GetComponent<Nemuri.CameraEffects.CameraObstructionManager>() == null)
                {
                    Camera.main.gameObject.AddComponent<Nemuri.CameraEffects.CameraObstructionManager>();
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
                
                var interactables = _gateController.GetComponentsInChildren<Interactable>(true);
                foreach (var interactable in interactables)
                {
                    interactable.DismissInteraction();
                    interactable.enabled = false;
                }
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

            _portalObject = FindPortalObject();

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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case IntroState.WaitingForApproachVines:
                    bool ronaArrived = false;
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
                            ronaArrived = true;
                            
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
                    else
                    {
                        ronaArrived = true;
                    }

                    if (ronaArrived && _gateController != null)
                    {
                        Transform activePlayer = FindActivePlayerTransform();
                        if (activePlayer != null)
                        {
                            float distToVines = Vector3.Distance(activePlayer.position, _gateController.transform.position);
                            if (distToVines <= 4.0f)
                            {
                                Debug.Log($"[NocturneIntroController] Player approached vines after Rona arrived. Triggering 1b dialog.");
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
                    // Rona NPC walks to Keiko
                    if (_ronaNpc != null && _ronaPathIndex < _ronaPath.Count)
                    {
                        Vector2 target2D = _ronaPath[_ronaPathIndex];
                        float currentY = _ronaNpc.transform.position.y;
                        Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);

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
                            _ronaPathIndex++;
                            if (_ronaPathIndex >= _ronaPath.Count)
                            {
                                SetNpcMoving(_ronaNpc, false);
                            }
                        }
                    }
                    else if (_ronaNpc != null)
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

                    // Murial NPC walks to Keiko (with Rona)
                    if (_murialNpc != null && _murialPathIndex < _murialPath.Count)
                    {
                        Vector2 target2D = _murialPath[_murialPathIndex];
                        float currentY = _murialNpc.transform.position.y;
                        Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(murialTarget.x, currentY + 10f, murialTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            murialTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                        if (distToTarget > 0.2f)
                        {
                            _murialNpc.transform.position = Vector3.MoveTowards(_murialNpc.transform.position, murialTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (murialTarget - _murialNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _murialNpc.transform.rotation = Quaternion.Slerp(_murialNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_murialNpc, true);
                        }
                        else
                        {
                            _murialPathIndex++;
                            if (_murialPathIndex >= _murialPath.Count)
                            {
                                SetNpcMoving(_murialNpc, false);
                            }
                        }
                    }
                    else if (_murialNpc != null)
                    {
                        SetNpcMoving(_murialNpc, false);
                        Transform activePlayer = FindActivePlayerTransform();
                        if (activePlayer != null)
                        {
                            Vector3 toPlayer = (activePlayer.position - _murialNpc.transform.position);
                            toPlayer.y = 0f;
                            toPlayer.Normalize();
                            if (toPlayer != Vector3.zero)
                            {
                                _murialNpc.transform.rotation = Quaternion.Slerp(_murialNpc.transform.rotation, Quaternion.LookRotation(toPlayer, Vector3.up), 5f * Time.deltaTime);
                            }
                        }
                    }

                    CheckApproach(_keikoNpc, () => TriggerThirdDialogue());
                    break;

                case IntroState.WaitingForFeanor:
                    // Keiko NPC walks to Feanor
                    if (_keikoNpc != null && _keikoPathIndex < _keikoPath.Count)
                    {
                        Vector2 target2D = _keikoPath[_keikoPathIndex];
                        float currentY = _keikoNpc.transform.position.y;
                        Vector3 keikoTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(keikoTarget.x, currentY + 10f, keikoTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            keikoTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_keikoNpc.transform.position, keikoTarget);
                        if (distToTarget > 0.2f)
                        {
                            _keikoNpc.transform.position = Vector3.MoveTowards(_keikoNpc.transform.position, keikoTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (keikoTarget - _keikoNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _keikoNpc.transform.rotation = Quaternion.Slerp(_keikoNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_keikoNpc, true);
                        }
                        else
                        {
                            _keikoPathIndex++;
                            if (_keikoPathIndex >= _keikoPath.Count)
                            {
                                SetNpcMoving(_keikoNpc, false);
                            }
                        }
                    }
                    else if (_keikoNpc != null)
                    {
                        SetNpcMoving(_keikoNpc, false);
                        Transform activePlayer = FindActivePlayerTransform();
                        if (activePlayer != null)
                        {
                            Vector3 toPlayer = (activePlayer.position - _keikoNpc.transform.position);
                            toPlayer.y = 0f;
                            toPlayer.Normalize();
                            if (toPlayer != Vector3.zero)
                            {
                                _keikoNpc.transform.rotation = Quaternion.Slerp(_keikoNpc.transform.rotation, Quaternion.LookRotation(toPlayer, Vector3.up), 5f * Time.deltaTime);
                            }
                        }
                    }

                    // Rona NPC walks to Feanor
                    if (_ronaNpc != null && _ronaPathIndex < _ronaPathToFeanor.Count)
                    {
                        Vector2 target2D = _ronaPathToFeanor[_ronaPathIndex];
                        float currentY = _ronaNpc.transform.position.y;
                        Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);

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
                            _ronaPathIndex++;
                            if (_ronaPathIndex >= _ronaPathToFeanor.Count)
                            {
                                SetNpcMoving(_ronaNpc, false);
                            }
                        }
                    }
                    else if (_ronaNpc != null)
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

                    // Murial NPC walks to Feanor
                    if (_murialNpc != null && _murialPathIndex < _murialPathToFeanor.Count)
                    {
                        Vector2 target2D = _murialPathToFeanor[_murialPathIndex];
                        float currentY = _murialNpc.transform.position.y;
                        Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(murialTarget.x, currentY + 10f, murialTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            murialTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                        if (distToTarget > 0.2f)
                        {
                            _murialNpc.transform.position = Vector3.MoveTowards(_murialNpc.transform.position, murialTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (murialTarget - _murialNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _murialNpc.transform.rotation = Quaternion.Slerp(_murialNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_murialNpc, true);
                        }
                        else
                        {
                            _murialPathIndex++;
                            if (_murialPathIndex >= _murialPathToFeanor.Count)
                            {
                                SetNpcMoving(_murialNpc, false);
                            }
                        }
                    }
                    else if (_murialNpc != null)
                    {
                        SetNpcMoving(_murialNpc, false);
                        Transform activePlayer = FindActivePlayerTransform();
                        if (activePlayer != null)
                        {
                            Vector3 toPlayer = (activePlayer.position - _murialNpc.transform.position);
                            toPlayer.y = 0f;
                            toPlayer.Normalize();
                            if (toPlayer != Vector3.zero)
                            {
                                _murialNpc.transform.rotation = Quaternion.Slerp(_murialNpc.transform.rotation, Quaternion.LookRotation(toPlayer, Vector3.up), 5f * Time.deltaTime);
                            }
                        }
                    }

                    CheckApproach(_feanorNpc, () => TriggerFourthDialogue());
                    break;

                case IntroState.WaitingForFerry:
                    // 1. Rona NPC path movement to Ferry
                    if (_ronaNpc != null && _ronaPathIndex < _ronaPathToFerry.Count)
                    {
                        Vector2 target2D = _ronaPathToFerry[_ronaPathIndex];
                        float currentY = _ronaNpc.transform.position.y;
                        Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);

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
                            _ronaPathIndex++;
                            if (_ronaPathIndex >= _ronaPathToFerry.Count)
                            {
                                SetNpcMoving(_ronaNpc, false);
                            }
                        }
                    }
                    else if (_ronaNpc != null)
                    {
                        SetNpcMoving(_ronaNpc, false);
                        RotateNpcToFacePlayer(_ronaNpc);
                    }

                    // 2. Murial NPC path movement to Ferry
                    if (_murialNpc != null && _murialPathIndex < _murialPathToFerry.Count)
                    {
                        Vector2 target2D = _murialPathToFerry[_murialPathIndex];
                        float currentY = _murialNpc.transform.position.y;
                        Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(murialTarget.x, currentY + 10f, murialTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            murialTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                        if (distToTarget > 0.2f)
                        {
                            _murialNpc.transform.position = Vector3.MoveTowards(_murialNpc.transform.position, murialTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (murialTarget - _murialNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _murialNpc.transform.rotation = Quaternion.Slerp(_murialNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_murialNpc, true);
                        }
                        else
                        {
                            _murialPathIndex++;
                            if (_murialPathIndex >= _murialPathToFerry.Count)
                            {
                                SetNpcMoving(_murialNpc, false);
                            }
                        }
                    }
                    else if (_murialNpc != null)
                    {
                        SetNpcMoving(_murialNpc, false);
                        RotateNpcToFacePlayer(_murialNpc);
                    }

                    // 3. Keiko NPC path movement to Ferry
                    if (_keikoNpc != null && _keikoPathIndex < _keikoPathToFerry.Count)
                    {
                        Vector2 target2D = _keikoPathToFerry[_keikoPathIndex];
                        float currentY = _keikoNpc.transform.position.y;
                        Vector3 keikoTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(keikoTarget.x, currentY + 10f, keikoTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            keikoTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_keikoNpc.transform.position, keikoTarget);
                        if (distToTarget > 0.2f)
                        {
                            _keikoNpc.transform.position = Vector3.MoveTowards(_keikoNpc.transform.position, keikoTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (keikoTarget - _keikoNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _keikoNpc.transform.rotation = Quaternion.Slerp(_keikoNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_keikoNpc, true);
                        }
                        else
                        {
                            _keikoPathIndex++;
                            if (_keikoPathIndex >= _keikoPathToFerry.Count)
                            {
                                SetNpcMoving(_keikoNpc, false);
                            }
                        }
                    }
                    else if (_keikoNpc != null)
                    {
                        SetNpcMoving(_keikoNpc, false);
                        RotateNpcToFacePlayer(_keikoNpc);
                    }

                    // 4. Feanor NPC path movement to Ferry
                    if (_feanorNpc != null && _feanorPathIndex < _feanorPathToFerry.Count)
                    {
                        Vector2 target2D = _feanorPathToFerry[_feanorPathIndex];
                        float currentY = _feanorNpc.transform.position.y;
                        Vector3 feanorTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(feanorTarget.x, currentY + 10f, feanorTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            feanorTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_feanorNpc.transform.position, feanorTarget);
                        if (distToTarget > 0.2f)
                        {
                            _feanorNpc.transform.position = Vector3.MoveTowards(_feanorNpc.transform.position, feanorTarget, 3f * Time.deltaTime);
                            
                            Vector3 dir = (feanorTarget - _feanorNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _feanorNpc.transform.rotation = Quaternion.Slerp(_feanorNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }

                            SetNpcMoving(_feanorNpc, true);
                        }
                        else
                        {
                            _feanorPathIndex++;
                            if (_feanorPathIndex >= _feanorPathToFerry.Count)
                            {
                                SetNpcMoving(_feanorNpc, false);
                            }
                        }
                    }
                    else if (_feanorNpc != null)
                    {
                        SetNpcMoving(_feanorNpc, false);
                        RotateNpcToFacePlayer(_feanorNpc);
                    }

                    CheckApproach(_ferryNpc, () => TriggerFifthDialogue());
                    break;

                case IntroState.FifthDialogue:
                    break;

                case IntroState.Completed:
                    if (_crystalObject == null)
                    {
                        _crystalObject = FindCrystalObject();
                    }

                    // 1. Rona NPC path movement to Gem Puzzle
                    if (_ronaNpc != null && _ronaPathIndex < _ronaPathToGem.Count)
                    {
                        Vector2 target2D = _ronaPathToGem[_ronaPathIndex];
                        float currentY = _ronaNpc.transform.position.y;
                        Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);

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
                            _ronaPathIndex++;
                            if (_ronaPathIndex >= _ronaPathToGem.Count)
                            {
                                SetNpcMoving(_ronaNpc, false);
                            }
                        }
                    }
                    else if (_ronaNpc != null)
                    {
                        SetNpcMoving(_ronaNpc, false);
                        if (_crystalObject != null)
                        {
                            RotateNpcToFaceTarget(_ronaNpc, _crystalObject);
                        }
                    }

                    // 2. Murial NPC path movement to Gem Puzzle
                    if (_murialNpc != null && _murialPathIndex < _murialPathToGem.Count)
                    {
                        Vector2 target2D = _murialPathToGem[_murialPathIndex];
                        float currentY = _murialNpc.transform.position.y;
                        Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(murialTarget.x, currentY + 10f, murialTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            murialTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                        if (distToTarget > 0.2f)
                        {
                            _murialNpc.transform.position = Vector3.MoveTowards(_murialNpc.transform.position, murialTarget, 3f * Time.deltaTime);
                            Vector3 dir = (murialTarget - _murialNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _murialNpc.transform.rotation = Quaternion.Slerp(_murialNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }
                            SetNpcMoving(_murialNpc, true);
                        }
                        else
                        {
                            _murialPathIndex++;
                            if (_murialPathIndex >= _murialPathToGem.Count)
                            {
                                SetNpcMoving(_murialNpc, false);
                            }
                        }
                    }
                    else if (_murialNpc != null)
                    {
                        SetNpcMoving(_murialNpc, false);
                        if (_crystalObject != null)
                        {
                            RotateNpcToFaceTarget(_murialNpc, _crystalObject);
                        }
                    }

                    // 3. Keiko NPC path movement to Gem Puzzle
                    if (_keikoNpc != null && _keikoPathIndex < _keikoPathToGem.Count)
                    {
                        Vector2 target2D = _keikoPathToGem[_keikoPathIndex];
                        float currentY = _keikoNpc.transform.position.y;
                        Vector3 keikoTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(keikoTarget.x, currentY + 10f, keikoTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            keikoTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_keikoNpc.transform.position, keikoTarget);
                        if (distToTarget > 0.2f)
                        {
                            _keikoNpc.transform.position = Vector3.MoveTowards(_keikoNpc.transform.position, keikoTarget, 3f * Time.deltaTime);
                            Vector3 dir = (keikoTarget - _keikoNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _keikoNpc.transform.rotation = Quaternion.Slerp(_keikoNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }
                            SetNpcMoving(_keikoNpc, true);
                        }
                        else
                        {
                            _keikoPathIndex++;
                            if (_keikoPathIndex >= _keikoPathToGem.Count)
                            {
                                SetNpcMoving(_keikoNpc, false);
                            }
                        }
                    }
                    else if (_keikoNpc != null)
                    {
                        SetNpcMoving(_keikoNpc, false);
                        if (_crystalObject != null)
                        {
                            RotateNpcToFaceTarget(_keikoNpc, _crystalObject);
                        }
                    }

                    // 4. Feanor NPC path movement to Gem Puzzle
                    if (_feanorNpc != null && _feanorPathIndex < _feanorPathToGem.Count)
                    {
                        Vector2 target2D = _feanorPathToGem[_feanorPathIndex];
                        float currentY = _feanorNpc.transform.position.y;
                        Vector3 feanorTarget = new Vector3(target2D.x, currentY, target2D.y);

                        Ray ray = new Ray(new Vector3(feanorTarget.x, currentY + 10f, feanorTarget.z), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                        {
                            feanorTarget.y = hit.point.y;
                        }

                        float distToTarget = Vector3.Distance(_feanorNpc.transform.position, feanorTarget);
                        if (distToTarget > 0.2f)
                        {
                            _feanorNpc.transform.position = Vector3.MoveTowards(_feanorNpc.transform.position, feanorTarget, 3f * Time.deltaTime);
                            Vector3 dir = (feanorTarget - _feanorNpc.transform.position);
                            dir.y = 0f;
                            dir.Normalize();
                            if (dir != Vector3.zero)
                            {
                                _feanorNpc.transform.rotation = Quaternion.Slerp(_feanorNpc.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 15f * Time.deltaTime);
                            }
                            SetNpcMoving(_feanorNpc, true);
                        }
                        else
                        {
                            _feanorPathIndex++;
                            if (_feanorPathIndex >= _feanorPathToGem.Count)
                            {
                                SetNpcMoving(_feanorNpc, false);
                            }
                        }
                    }
                    else if (_feanorNpc != null)
                    {
                        SetNpcMoving(_feanorNpc, false);
                        if (_crystalObject != null)
                        {
                            RotateNpcToFaceTarget(_feanorNpc, _crystalObject);
                        }
                    }
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
            else if (node.text.Contains("A Rabbit is sitting"))
            {
                Debug.Log("[NocturneIntroController] Triggering camera pan to Rabbit.");
                StartCoroutine(LookAtRabbitRoutine());
            }

            if (node.text.Contains("key to restoring the Nocturne Heart") ||
                node.text.Contains("do something with this") ||
                node.text.Contains("Keiko concentrates her power"))
            {
                if (_portalObject == null)
                {
                    _portalObject = FindPortalObject();
                }
                if (_portalObject != null)
                {
                    RotateNpcToFaceTarget(_feanorNpc, _portalObject);
                    RotateNpcToFaceTarget(_keikoNpc, _portalObject);
                    RotatePlayerToFaceTarget(_portalObject);
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
                RotateNpcToFacePlayer(_murialNpc);
            }
        }

        private IEnumerator LookAtRabbitRoutine()
        {
            if (_ferryNpc == null || Camera.main == null) yield break;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = false;
            }

            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null) brain.enabled = false;

            Vector3 startPos = Camera.main.transform.position;
            Quaternion startRot = Camera.main.transform.rotation;

            Vector3 targetPos = _ferryNpc.transform.position + new Vector3(-6f, 4f, 5f);
            Vector3 lookDir = (_ferryNpc.transform.position + Vector3.up * 1f - targetPos).normalized;
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

            float elapsed = 0f;
            float duration = 2.5f;

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

        private void RestoreCameraToPlayer()
        {
            if (Camera.main == null) return;
            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null) brain.enabled = true;
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
            RotateNpcToFaceTarget(_keikoNpc, _feanorNpc);
            RotatePlayerToFaceTarget(_feanorNpc);

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
                        
                        var interactables = _gateController.GetComponentsInChildren<Interactable>(true);
                        foreach (var interactable in interactables)
                        {
                            interactable.enabled = true;
                        }
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
                    
                    // Reset path indices for Rona and Murial so they start walking to Feanor
                    _ronaPathIndex = 0;
                    _murialPathIndex = 0;
                    
                    _state = IntroState.WaitingForFeanor;
                    break;

                case IntroState.FourthDialogue:
                    RestoreCameraToPlayer();
                    SetPlayerMovementEnabled(true);
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(4, true); // Unlock Feanor
                    }

                    // Reset path indices for all 4 NPCs so they start walking to the Ferry (Bunny)
                    _ronaPathIndex = 0;
                    _murialPathIndex = 0;
                    _keikoPathIndex = 0;
                    _feanorPathIndex = 0;

                    _state = IntroState.WaitingForFerry;
                    break;

                case IntroState.FifthDialogue:
                    SetPlayerMovementEnabled(true);
                    SetCrystalsInteractable(true);
                    
                    // Reset path indices for all 4 NPCs so they start walking to the Gem Puzzle
                    _ronaPathIndex = 0;
                    _murialPathIndex = 0;
                    _keikoPathIndex = 0;
                    _feanorPathIndex = 0;

                    _state = IntroState.Completed;
                    IsIntroCompleted = true; // Unlock character swaps!
                    _crystalObject = FindCrystalObject();
                    
                    Debug.Log("[NocturneIntroController] Intro flow sequence fully completed! NPCs walking to Gem Puzzle.");
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
                foreach (AnimatorControllerParameter param in anim.parameters)
                {
                    if (param.name == "IsMoving")
                    {
                        anim.SetBool("IsMoving", moving);
                    }
                    if (param.name == "Speed")
                    {
                        anim.SetFloat("Speed", moving ? 1f : 0f);
                    }
                }
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

        private void RotateNpcToFaceTarget(GameObject npc, GameObject target)
        {
            if (npc == null || target == null) return;
            Vector3 dir = (target.transform.position - npc.transform.position);
            dir.y = 0f;
            dir.Normalize();
            if (dir != Vector3.zero)
            {
                npc.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        private void RotatePlayerToFaceTarget(GameObject target)
        {
            if (target == null) return;
            Transform activePlayer = FindActivePlayerTransform();
            if (activePlayer == null) return;
            Vector3 dir = (target.transform.position - activePlayer.position);
            dir.y = 0f;
            dir.Normalize();
            if (dir != Vector3.zero)
            {
                activePlayer.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        private GameObject FindPortalObject()
        {
            GameObject pg = GameObject.Find("PINEALGRAND");
            if (pg != null)
            {
                return FindChildRecursive(pg.transform, "cube.015");
            }
            return GameObject.Find("cube.015");
        }

        private GameObject FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject result = FindChildRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
