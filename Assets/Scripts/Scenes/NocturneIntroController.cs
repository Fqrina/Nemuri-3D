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
            Completed,
            CrescentTearPart1,
            CrescentTearPart2,
            CrescentTearPart3,
            CrescentTearCollectedDialogue,
            WaitingForCrescentDialogue,
            SomniaSeedPart1,
            SomniaSeedPart2,
            SomniaSeedCollectedDialogue,
            BridgeIntroDialogue,
            BridgeSuccessDialogue,
            Puzzle3IntroDialogue,
            Puzzle3SuccessDialogue,
            Puzzle3CollectedDialogue,
            WaitingForBridge1Dialogue,
            WaitingForBunnyDialoguePostDreampearl,
            BunnyDialoguePostDreampearl,
            WaitingForPortalDialogue,
            PortalDialoguePartA,
            PortalDialoguePartB,
            PortalDialoguePartC
        }

        public static NocturneIntroController Instance { get; private set; }
        public static bool IsIntroCompleted { get; private set; } = false;
        public bool IsPlayerMovementActive { get; private set; } = true;

        public static bool CanSwapTo(int characterIndex)
        {
            if (Instance != null)
            {
                // Lock character swaps during Somnia Seed puzzle approach/walk and dialogue phase
                if (Instance._startGemPuzzleWalk && !Instance.HasSomniaSeedPart1Ended)
                {
                    return false;
                }

                if (Instance._startCrescentWalk && !Instance.HasCrescentTearPart1Ended)
                {
                    return false;
                }

                if (Instance._state == IntroState.WaitingForGate)
                {
                    // Allow Kael (0), Rona (1), and Murial (2) when waiting to lower the gate
                    return characterIndex == 0 || characterIndex == 1 || characterIndex == 2;
                }
            }

            if (IsIntroCompleted)
            {
                return true;
            }

            // Lock manual swaps completely after the gate is opened until intro is completed
            return false;
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
            new Vector2(-26.44f, 74.89f)
        };

        private List<Vector2> _murialPathToFeanor = new List<Vector2>()
        {
            new Vector2(-13.21f, 103.38f),
            new Vector2(-10.42f, 99.42f),
            new Vector2(-10.42f, 95.54f),
            new Vector2(-16.94f, 91.69f),
            new Vector2(-23.42f, 88.76f),
            new Vector2(-26.11f, 83.72f),
            new Vector2(-23.44f, 73.89f)
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

        private TextAsset _dialogueJsonSomnia;
        private bool _startGemPuzzleWalk = false;
        private bool _dialogueSomniaStarted = false;
        public bool HasDialogueSomniaEnded { get; private set; } = false;
        public bool HasCrescentTearPart1Started { get; private set; } = false;
        public bool HasCrescentTearPart1Ended { get; private set; } = false;
        public bool HasMurialInteracted { get; private set; } = false;
        public bool HasCrescentTearPart2Ended { get; private set; } = false;
        public bool HasFeanorInteracted { get; private set; } = false;
        public bool HasCrescentTearPart3Ended { get; private set; } = false;
        public bool HasCrescentTearCollected { get; private set; } = false;
        public bool HasSomniaSeedPart1Started { get; private set; } = false;
        public bool HasSomniaSeedPart1Ended { get; private set; } = false;
        public bool HasSomniaSeedPuzzleCompleted { get; private set; } = false;
        public bool HasBridgeIntroStarted { get; private set; } = false;
        public bool HasBridgeIntroEnded { get; private set; } = false;
        public bool HasBridgeFixed { get; private set; } = false;
        public bool HasPuzzle3IntroStarted { get; private set; } = false;
        public bool HasPuzzle3IntroEnded { get; private set; } = false;
        public bool HasPuzzle3BridgeCreated { get; private set; } = false;
        public bool HasPuzzle3Collected { get; private set; } = false;

        private bool _startCrescentWalk = false;
        private bool _crescentDialogueStarted = false;

        private bool _startBridge1Walk = false;
        private bool _bridge1DialogueStarted = false;

        private List<Vector2> _ronaPathToBridge1 = new List<Vector2>()
        {
            new Vector2(-10.86f + 0.15f, 104.96f - 0.1f),
            new Vector2(-12.57f + 0.15f, 104.03f - 0.1f),
            new Vector2(-14.04f + 0.15f, 103.06f - 0.1f),
            new Vector2(-16.73f, 102.8f)
        };

        private List<Vector2> _murialPathToBridge1 = new List<Vector2>()
        {
            new Vector2(-10.86f - 0.15f, 104.96f + 0.1f),
            new Vector2(-12.57f - 0.15f, 104.03f + 0.1f),
            new Vector2(-14.04f - 0.15f, 103.06f + 0.1f),
            new Vector2(-16.77f, 101.65f)
        };

        private List<Vector2> _keikoPathToBridge1 = new List<Vector2>()
        {
            new Vector2(-10.86f + 0.1f, 104.96f + 0.15f),
            new Vector2(-12.57f + 0.1f, 104.03f + 0.15f),
            new Vector2(-14.04f + 0.1f, 103.06f + 0.15f),
            new Vector2(-16.88f, 100.33f)
        };

        private List<Vector2> _feanorPathToBridge1 = new List<Vector2>()
        {
            new Vector2(-10.86f - 0.1f, 104.96f - 0.15f),
            new Vector2(-12.57f - 0.1f, 104.03f - 0.15f),
            new Vector2(-14.04f - 0.1f, 103.06f - 0.15f),
            new Vector2(-17.0f, 98.61f)
        };

        public bool HasBunnyDialogueEnded { get; private set; } = false;
        public bool HasPortalFixed { get; private set; } = false;
        public bool HasMurialFallen { get; private set; } = false;

        private bool _startBunnyWalkPostDreampearl = false;
        private bool _bunnyDialoguePostDreampearlStarted = false;
        private bool _startPortalWalk = false;
        private bool _portalDialogueStarted = false;

        private List<Vector2> _ronaPathToBunnyPostDreampearl = new List<Vector2>()
        {
            new Vector2(-13.23f + 0.15f, 100.15f - 0.1f),
            new Vector2(-13.22f + 0.15f, 96.45f - 0.1f),
            new Vector2(-16.43f + 0.15f, 93.87f - 0.1f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-15.78f + 0.15f, 87.5f - 0.1f),
            new Vector2(-15.86f, 84.95f)
        };

        private List<Vector2> _murialPathToBunnyPostDreampearl = new List<Vector2>()
        {
            new Vector2(-13.23f - 0.15f, 100.15f + 0.1f),
            new Vector2(-13.22f - 0.15f, 96.45f + 0.1f),
            new Vector2(-16.43f - 0.15f, 93.87f + 0.1f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-15.78f - 0.15f, 87.5f + 0.1f),
            new Vector2(-14.6f, 84.8f)
        };

        private List<Vector2> _keikoPathToBunnyPostDreampearl = new List<Vector2>()
        {
            new Vector2(-13.23f + 0.1f, 100.15f + 0.15f),
            new Vector2(-13.22f + 0.1f, 96.45f + 0.15f),
            new Vector2(-16.43f + 0.1f, 93.87f + 0.15f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-15.78f + 0.1f, 87.5f + 0.15f),
            new Vector2(-17.57f, 84.38f)
        };

        private List<Vector2> _feanorPathToBunnyPostDreampearl = new List<Vector2>()
        {
            new Vector2(-13.23f - 0.1f, 100.15f - 0.15f),
            new Vector2(-13.22f - 0.1f, 96.45f - 0.15f),
            new Vector2(-16.43f - 0.1f, 93.87f - 0.15f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-15.78f - 0.1f, 87.5f - 0.15f),
            new Vector2(-19.48f, 83.77f)
        };

        private List<Vector2> _ronaPathToPortal = new List<Vector2>()
        {
            new Vector2(-15.78f + 0.15f, 87.5f - 0.1f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-16.43f + 0.15f, 93.87f - 0.1f),
            new Vector2(-18.81f + 0.15f, 91.75f - 0.1f),
            new Vector2(-22.58f + 0.15f, 90.05f - 0.1f),
            new Vector2(-26.24f + 0.15f, 87.11f - 0.1f),
            new Vector2(-27.48f + 0.15f, 83.05f - 0.1f),
            new Vector2(-27.39f + 0.15f, 79.41f - 0.1f),
            new Vector2(-27.12f, 75.26f)
        };

        private List<Vector2> _murialPathToPortal = new List<Vector2>()
        {
            new Vector2(-15.78f - 0.15f, 87.5f + 0.1f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-16.43f - 0.15f, 93.87f + 0.1f),
            new Vector2(-18.81f - 0.15f, 91.75f + 0.1f),
            new Vector2(-22.58f - 0.15f, 90.05f + 0.1f),
            new Vector2(-26.24f - 0.15f, 87.11f + 0.1f),
            new Vector2(-27.48f - 0.15f, 83.05f + 0.1f),
            new Vector2(-27.39f - 0.15f, 79.41f + 0.1f),
            new Vector2(-28.27f, 74.9f)
        };

        private List<Vector2> _keikoPathToPortal = new List<Vector2>()
        {
            new Vector2(-15.78f + 0.1f, 87.5f + 0.15f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-16.43f + 0.1f, 93.87f + 0.15f),
            new Vector2(-18.81f + 0.1f, 91.75f + 0.15f),
            new Vector2(-22.58f + 0.1f, 90.05f + 0.15f),
            new Vector2(-26.24f + 0.1f, 87.11f + 0.15f),
            new Vector2(-27.48f + 0.1f, 83.05f + 0.15f),
            new Vector2(-27.39f + 0.1f, 79.41f + 0.15f),
            new Vector2(-25.76f, 72.42f)
        };

        private List<Vector2> _feanorPathToPortal = new List<Vector2>()
        {
            new Vector2(-15.78f - 0.1f, 87.5f - 0.15f),
            new Vector2(-15.66f, 90.12f),
            new Vector2(-16.43f - 0.1f, 93.87f - 0.15f),
            new Vector2(-18.81f - 0.1f, 91.75f - 0.15f),
            new Vector2(-22.58f - 0.1f, 90.05f - 0.15f),
            new Vector2(-26.24f - 0.1f, 87.11f - 0.15f),
            new Vector2(-27.48f - 0.1f, 83.05f - 0.15f),
            new Vector2(-27.39f - 0.1f, 79.41f - 0.15f),
            new Vector2(-29.84f, 72.5f)
        };

        private List<Vector2> _ronaPathToCrescent = new List<Vector2>()
        {
            new Vector2(-12.085f, 111.217f),
            new Vector2(-11.86f, 109.19f),
            new Vector2(-11.86f, 105.35f),
            new Vector2(-8.12f, 105.99f)
        };

        private List<Vector2> _murialPathToCrescent = new List<Vector2>()
        {
            new Vector2(-14.361f, 112.13f),
            new Vector2(-12.085f, 111.217f),
            new Vector2(-11.86f, 109.19f),
            new Vector2(-11.56f, 105.05f),
            new Vector2(-8.73f, 104.27f)
        };

        private List<Vector2> _keikoPathToCrescent = new List<Vector2>()
        {
            new Vector2(-15.43f, 107.92f),
            new Vector2(-12.16f, 105.65f),
            new Vector2(-10.13f, 101.96f)
        };

        private List<Vector2> _feanorPathToCrescent = new List<Vector2>()
        {
            new Vector2(-14.984f, 111.964f),
            new Vector2(-15.43f, 107.92f),
            new Vector2(-11.86f, 105.35f),
            new Vector2(-9.17f, 105.59f)
        };

        private Vector3 _ferryInitialPosition;

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

            // Dynamically configure rockpuzzle1 (stone) interaction
            GameObject rock1 = GameObject.Find("rockpuzzle1");
            if (rock1 != null)
            {
                var interactable = rock1.GetComponent<Interactable>();
                if (interactable == null) interactable = rock1.AddComponent<Interactable>();
                interactable.PromptText = "Interact (E)";
                interactable.InteractionRange = 4.0f;
                interactable.HoldSeconds = 0f;
                if (interactable.OnInteract == null) interactable.OnInteract = new UnityEngine.Events.UnityEvent();
                interactable.OnInteract.RemoveAllListeners();
                interactable.OnInteract.AddListener(OnRock1Interacted);
                interactable.enabled = true; // Enabled at start!
            }

            // Dynamically configure Puzzle2InteractionPoint
            GameObject p2Ip = GameObject.Find("Puzzle2InteractionPoint");
            if (p2Ip != null)
            {
                var interactable = p2Ip.GetComponent<Interactable>();
                if (interactable == null) interactable = p2Ip.AddComponent<Interactable>();
                interactable.PromptText = "Press E to Resonance";
                interactable.InteractionRange = 4.0f;
                interactable.HoldSeconds = 0f;
                if (interactable.OnInteract == null) interactable.OnInteract = new UnityEngine.Events.UnityEvent();
                interactable.OnInteract.RemoveAllListeners();
                interactable.OnInteract.AddListener(OnPuzzle2Interacted);
                interactable.enabled = false; // Initially inactive until Somnia Seed dialogue ends
            }

            // Dynamically configure Puzzle3InteractionPoint
            GameObject p3Ip = GameObject.Find("Puzzle3InteractionPoint");
            if (p3Ip != null)
            {
                var interactable = p3Ip.GetComponent<Interactable>();
                if (interactable == null) interactable = p3Ip.AddComponent<Interactable>();
                interactable.PromptText = "Investigate Shell (E)";
                interactable.InteractionRange = 4.0f;
                interactable.HoldSeconds = 0f;
                if (interactable.OnInteract == null) interactable.OnInteract = new UnityEngine.Events.UnityEvent();
                interactable.OnInteract.RemoveAllListeners();
                interactable.OnInteract.AddListener(OnPuzzle3Interacted);
                interactable.enabled = false; // Initially inactive until Crescent Tear is collected
            }

            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SetCharacterUnlocked(0, true);  // Kiel (Unlocked)
                CharacterSwapManager.Instance.SetCharacterUnlocked(1, false); // Rona (Locked)
                CharacterSwapManager.Instance.SetCharacterUnlocked(2, false); // Murial (Locked)
                CharacterSwapManager.Instance.SetCharacterUnlocked(3, false); // Keiko (Locked)
                CharacterSwapManager.Instance.SetCharacterUnlocked(4, false); // Feanor (Locked)
            }

            _portalObject = FindPortalObject();

            // Speed up all animators in the scene by 1.5x
            Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsInactive.Include);
            foreach (var anim in allAnimators)
            {
                if (anim != null)
                {
                    anim.speed = 1.5f;
                }
            }

            if (_ferryNpc != null)
            {
                _ferryInitialPosition = _ferryNpc.transform.position;
            }

            // Submerge and hide Puzzle3 Bridge on start
            GameObject startP3Bridge = FindPuzzle3Bridge();
            if (startP3Bridge != null)
            {
                Vector3 pos = startP3Bridge.transform.position;
                pos.y = 0.92f;
                startP3Bridge.transform.position = pos;

                Renderer[] renderers = startP3Bridge.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r != null && r.material != null)
                    {
                        Material mat = r.material;
                        mat.SetFloat("_Surface", 1f); // 1 is transparent
                        mat.SetFloat("_Blend", 0f);   // 0 is alpha blend
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                        Color c = mat.color;
                        c.a = 0f;
                        mat.color = c;
                    }
                }
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
                        ronaTarget.y = GetGroundHeight(ronaTarget);

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
                        ronaTarget.y = GetGroundHeight(ronaTarget);

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
                        murialTarget.y = GetGroundHeight(murialTarget);

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
                        keikoTarget.y = GetGroundHeight(keikoTarget);

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
                        ronaTarget.y = GetGroundHeight(ronaTarget);

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
                        murialTarget.y = GetGroundHeight(murialTarget);

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
                        ronaTarget.y = GetGroundHeight(ronaTarget);

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
                        murialTarget.y = GetGroundHeight(murialTarget);

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
                        keikoTarget.y = GetGroundHeight(keikoTarget);

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
                        feanorTarget.y = GetGroundHeight(feanorTarget);

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

                    bool allNpcsArrived = true;
                    if (_ronaNpc != null && _ronaPathIndex < _ronaPathToFerry.Count) allNpcsArrived = false;
                    if (_murialNpc != null && _murialPathIndex < _murialPathToFerry.Count) allNpcsArrived = false;
                    if (_keikoNpc != null && _keikoPathIndex < _keikoPathToFerry.Count) allNpcsArrived = false;
                    if (_feanorNpc != null && _feanorPathIndex < _feanorPathToFerry.Count) allNpcsArrived = false;

                    if (allNpcsArrived)
                    {
                        CheckApproach(_ferryNpc, () => TriggerFifthDialogue());
                    }
                    break;

                case IntroState.FifthDialogue:
                    break;

                case IntroState.Completed:
                    if (_crystalObject == null)
                    {
                        _crystalObject = FindCrystalObject();
                    }

                    // Only move NPCs if the walking sequence has been started by the player's interaction
                    if (_startGemPuzzleWalk)
                    {
                        // 1. Rona NPC path movement to Gem Puzzle
                        if (_ronaNpc != null && _ronaPathIndex < _ronaPathToGem.Count)
                        {
                            Vector2 target2D = _ronaPathToGem[_ronaPathIndex];
                            float currentY = _ronaNpc.transform.position.y;
                            Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);
                            ronaTarget.y = GetGroundHeight(ronaTarget);

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
                            murialTarget.y = GetGroundHeight(murialTarget);

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
                            keikoTarget.y = GetGroundHeight(keikoTarget);

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
                            feanorTarget.y = GetGroundHeight(feanorTarget);

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

                        // Start dialogue only after all NPCs have reached their targets!
                        if (!_dialogueSomniaStarted)
                        {
                            bool allArrived = true;
                            if (_ronaNpc != null && _ronaPathIndex < _ronaPathToGem.Count) allArrived = false;
                            if (_murialNpc != null && _murialPathIndex < _murialPathToGem.Count) allArrived = false;
                            if (_keikoNpc != null && _keikoPathIndex < _keikoPathToGem.Count) allArrived = false;
                            if (_feanorNpc != null && _feanorPathIndex < _feanorPathToGem.Count) allArrived = false;

                            if (allArrived)
                            {
                                _dialogueSomniaStarted = true;
                                TriggerSomniaSeedPart1Dialogue();
                            }
                        }
                    }

                    if (HasPuzzle3Collected && !_startBunnyWalkPostDreampearl)
                    {
                        GameObject bunnyObj = null;
                        GameObject pg = GameObject.Find("PINEALGRAND");
                        if (pg != null)
                        {
                            Transform go1 = pg.transform.Find("GameObject (1)");
                            if (go1 != null)
                            {
                                Transform metarig = go1.Find("metarig");
                                if (metarig != null) bunnyObj = metarig.gameObject;
                            }
                        }
                        if (bunnyObj != null)
                        {
                            CheckApproach(bunnyObj, () => TriggerBunnyWalkPostDreampearlSequence());
                        }
                    }

                    // Proximity trigger removed: walk sequence is now triggered explicitly via interaction
                    break;

                case IntroState.WaitingForCrescentDialogue:
                    if (_startCrescentWalk)
                    {
                        GameObject p2Ip = GameObject.Find("Puzzle2InteractionPoint");

                        // 1. Rona
                        if (_ronaNpc != null && _ronaPathIndex < _ronaPathToCrescent.Count)
                        {
                            Vector2 target2D = _ronaPathToCrescent[_ronaPathIndex];
                            float currentY = _ronaNpc.transform.position.y;
                            Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);
                            ronaTarget.y = GetGroundHeight(ronaTarget);

                            float dist = Vector3.Distance(_ronaNpc.transform.position, ronaTarget);
                            if (dist > 0.2f)
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
                                if (_ronaPathIndex >= _ronaPathToCrescent.Count) SetNpcMoving(_ronaNpc, false);
                            }
                        }
                        else if (_ronaNpc != null)
                        {
                            SetNpcMoving(_ronaNpc, false);
                            if (p2Ip != null) RotateNpcToFaceTarget(_ronaNpc, p2Ip);
                        }

                        // 2. Murial
                        if (_murialNpc != null && _murialPathIndex < _murialPathToCrescent.Count)
                        {
                            Vector2 target2D = _murialPathToCrescent[_murialPathIndex];
                            float currentY = _murialNpc.transform.position.y;
                            Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);
                            murialTarget.y = GetGroundHeight(murialTarget);

                            float dist = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                            if (dist > 0.2f)
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
                                if (_murialPathIndex >= _murialPathToCrescent.Count) SetNpcMoving(_murialNpc, false);
                            }
                        }
                        else if (_murialNpc != null)
                        {
                            SetNpcMoving(_murialNpc, false);
                            if (p2Ip != null) RotateNpcToFaceTarget(_murialNpc, p2Ip);
                        }

                        // 3. Keiko
                        if (_keikoNpc != null && _keikoPathIndex < _keikoPathToCrescent.Count)
                        {
                            Vector2 target2D = _keikoPathToCrescent[_keikoPathIndex];
                            float currentY = _keikoNpc.transform.position.y;
                            Vector3 keikoTarget = new Vector3(target2D.x, currentY, target2D.y);
                            keikoTarget.y = GetGroundHeight(keikoTarget);

                            float dist = Vector3.Distance(_keikoNpc.transform.position, keikoTarget);
                            if (dist > 0.2f)
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
                                if (_keikoPathIndex >= _keikoPathToCrescent.Count) SetNpcMoving(_keikoNpc, false);
                            }
                        }
                        else if (_keikoNpc != null)
                        {
                            SetNpcMoving(_keikoNpc, false);
                            if (p2Ip != null) RotateNpcToFaceTarget(_keikoNpc, p2Ip);
                        }

                        // 4. Feanor
                        if (_feanorNpc != null && _feanorPathIndex < _feanorPathToCrescent.Count)
                        {
                            Vector2 target2D = _feanorPathToCrescent[_feanorPathIndex];
                            float currentY = _feanorNpc.transform.position.y;
                            Vector3 feanorTarget = new Vector3(target2D.x, currentY, target2D.y);
                            feanorTarget.y = GetGroundHeight(feanorTarget);

                            float dist = Vector3.Distance(_feanorNpc.transform.position, feanorTarget);
                            if (dist > 0.2f)
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
                                if (_feanorPathIndex >= _feanorPathToCrescent.Count) SetNpcMoving(_feanorNpc, false);
                            }
                        }
                        else if (_feanorNpc != null)
                        {
                            SetNpcMoving(_feanorNpc, false);
                            if (p2Ip != null) RotateNpcToFaceTarget(_feanorNpc, p2Ip);
                        }

                        // Check if all arrived to trigger Dialogue Part 1
                        if (!_crescentDialogueStarted)
                        {
                            bool allArrived = true;
                            if (_ronaNpc != null && _ronaPathIndex < _ronaPathToCrescent.Count) allArrived = false;
                            if (_murialNpc != null && _murialPathIndex < _murialPathToCrescent.Count) allArrived = false;
                            if (_keikoNpc != null && _keikoPathIndex < _keikoPathToCrescent.Count) allArrived = false;
                            if (_feanorNpc != null && _feanorPathIndex < _feanorPathToCrescent.Count) allArrived = false;

                            if (allArrived)
                            {
                                _crescentDialogueStarted = true;
                                TriggerCrescentTearPart1Dialogue();
                            }
                        }
                    }
                    break;

                case IntroState.WaitingForBridge1Dialogue:
                    if (_startBridge1Walk)
                    {
                        Transform activePlayer = FindActivePlayerTransform();

                        // 1. Rona
                        if (_ronaNpc != null && _ronaPathIndex < _ronaPathToBridge1.Count)
                        {
                            Vector2 target2D = _ronaPathToBridge1[_ronaPathIndex];
                            float currentY = _ronaNpc.transform.position.y;
                            Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);
                            ronaTarget.y = GetGroundHeight(ronaTarget);

                            float dist = Vector3.Distance(_ronaNpc.transform.position, ronaTarget);
                            if (dist > 0.2f)
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
                                if (_ronaPathIndex >= _ronaPathToBridge1.Count) SetNpcMoving(_ronaNpc, false);
                            }
                        }
                        else if (_ronaNpc != null)
                        {
                            SetNpcMoving(_ronaNpc, false);
                            if (activePlayer != null) RotateNpcToFaceTarget(_ronaNpc, activePlayer.gameObject);
                        }

                        // 2. Murial
                        if (_murialNpc != null && _murialPathIndex < _murialPathToBridge1.Count)
                        {
                            Vector2 target2D = _murialPathToBridge1[_murialPathIndex];
                            float currentY = _murialNpc.transform.position.y;
                            Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);
                            murialTarget.y = GetGroundHeight(murialTarget);

                            float dist = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                            if (dist > 0.2f)
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
                                if (_murialPathIndex >= _murialPathToBridge1.Count) SetNpcMoving(_murialNpc, false);
                            }
                        }
                        else if (_murialNpc != null)
                        {
                            SetNpcMoving(_murialNpc, false);
                            if (activePlayer != null) RotateNpcToFaceTarget(_murialNpc, activePlayer.gameObject);
                        }

                        // 3. Keiko
                        if (_keikoNpc != null && _keikoPathIndex < _keikoPathToBridge1.Count)
                        {
                            Vector2 target2D = _keikoPathToBridge1[_keikoPathIndex];
                            float currentY = _keikoNpc.transform.position.y;
                            Vector3 keikoTarget = new Vector3(target2D.x, currentY, target2D.y);
                            keikoTarget.y = GetGroundHeight(keikoTarget);

                            float dist = Vector3.Distance(_keikoNpc.transform.position, keikoTarget);
                            if (dist > 0.2f)
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
                                if (_keikoPathIndex >= _keikoPathToBridge1.Count) SetNpcMoving(_keikoNpc, false);
                            }
                        }
                        else if (_keikoNpc != null)
                        {
                            SetNpcMoving(_keikoNpc, false);
                            if (activePlayer != null) RotateNpcToFaceTarget(_keikoNpc, activePlayer.gameObject);
                        }

                        // 4. Feanor
                        if (_feanorNpc != null && _feanorPathIndex < _feanorPathToBridge1.Count)
                        {
                            Vector2 target2D = _feanorPathToBridge1[_feanorPathIndex];
                            float currentY = _feanorNpc.transform.position.y;
                            Vector3 feanorTarget = new Vector3(target2D.x, currentY, target2D.y);
                            feanorTarget.y = GetGroundHeight(feanorTarget);

                            float dist = Vector3.Distance(_feanorNpc.transform.position, feanorTarget);
                            if (dist > 0.2f)
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
                                if (_feanorPathIndex >= _feanorPathToBridge1.Count) SetNpcMoving(_feanorNpc, false);
                            }
                        }
                        else if (_feanorNpc != null)
                        {
                            SetNpcMoving(_feanorNpc, false);
                            if (activePlayer != null) RotateNpcToFaceTarget(_feanorNpc, activePlayer.gameObject);
                        }

                        // Check if all arrived to trigger Bridge Dialogue
                        if (!_bridge1DialogueStarted)
                        {
                            bool allArrived = true;
                            if (_ronaNpc != null && _ronaPathIndex < _ronaPathToBridge1.Count) allArrived = false;
                            if (_murialNpc != null && _murialPathIndex < _murialPathToBridge1.Count) allArrived = false;
                            if (_keikoNpc != null && _keikoPathIndex < _keikoPathToBridge1.Count) allArrived = false;
                            if (_feanorNpc != null && _feanorPathIndex < _feanorPathToBridge1.Count) allArrived = false;

                            if (allArrived)
                            {
                                _bridge1DialogueStarted = true;
                                TriggerBridge1Dialogue();
                            }
                        }
                    }
                    break;

                case IntroState.WaitingForBunnyDialoguePostDreampearl:
                    if (_startBunnyWalkPostDreampearl)
                    {
                        GameObject bunnyObj = null;
                        GameObject pg = GameObject.Find("PINEALGRAND");
                        if (pg != null)
                        {
                            Transform go1 = pg.transform.Find("GameObject (1)");
                            if (go1 != null)
                            {
                                Transform metarig = go1.Find("metarig");
                                if (metarig != null) bunnyObj = metarig.gameObject;
                            }
                        }

                        // 1. Rona
                        if (_ronaNpc != null && _ronaPathIndex < _ronaPathToBunnyPostDreampearl.Count)
                        {
                            Vector2 target2D = _ronaPathToBunnyPostDreampearl[_ronaPathIndex];
                            float currentY = _ronaNpc.transform.position.y;
                            Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);
                            ronaTarget.y = GetGroundHeight(ronaTarget);

                            float dist = Vector3.Distance(_ronaNpc.transform.position, ronaTarget);
                            if (dist > 0.2f)
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
                                if (_ronaPathIndex >= _ronaPathToBunnyPostDreampearl.Count) SetNpcMoving(_ronaNpc, false);
                            }
                        }
                        else if (_ronaNpc != null)
                        {
                            SetNpcMoving(_ronaNpc, false);
                            if (bunnyObj != null) RotateNpcToFaceTarget(_ronaNpc, bunnyObj);
                        }

                        // 2. Murial
                        if (_murialNpc != null && _murialPathIndex < _murialPathToBunnyPostDreampearl.Count)
                        {
                            Vector2 target2D = _murialPathToBunnyPostDreampearl[_murialPathIndex];
                            float currentY = _murialNpc.transform.position.y;
                            Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);
                            murialTarget.y = GetGroundHeight(murialTarget);

                            float dist = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                            if (dist > 0.2f)
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
                                if (_murialPathIndex >= _murialPathToBunnyPostDreampearl.Count) SetNpcMoving(_murialNpc, false);
                            }
                        }
                        else if (_murialNpc != null)
                        {
                            SetNpcMoving(_murialNpc, false);
                            if (bunnyObj != null) RotateNpcToFaceTarget(_murialNpc, bunnyObj);
                        }

                        // 3. Keiko
                        if (_keikoNpc != null && _keikoPathIndex < _keikoPathToBunnyPostDreampearl.Count)
                        {
                            Vector2 target2D = _keikoPathToBunnyPostDreampearl[_keikoPathIndex];
                            float currentY = _keikoNpc.transform.position.y;
                            Vector3 keikoTarget = new Vector3(target2D.x, currentY, target2D.y);
                            keikoTarget.y = GetGroundHeight(keikoTarget);

                            float dist = Vector3.Distance(_keikoNpc.transform.position, keikoTarget);
                            if (dist > 0.2f)
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
                                if (_keikoPathIndex >= _keikoPathToBunnyPostDreampearl.Count) SetNpcMoving(_keikoNpc, false);
                            }
                        }
                        else if (_keikoNpc != null)
                        {
                            SetNpcMoving(_keikoNpc, false);
                            if (bunnyObj != null) RotateNpcToFaceTarget(_keikoNpc, bunnyObj);
                        }

                        // 4. Feanor
                        if (_feanorNpc != null && _feanorPathIndex < _feanorPathToBunnyPostDreampearl.Count)
                        {
                            Vector2 target2D = _feanorPathToBunnyPostDreampearl[_feanorPathIndex];
                            float currentY = _feanorNpc.transform.position.y;
                            Vector3 feanorTarget = new Vector3(target2D.x, currentY, target2D.y);
                            feanorTarget.y = GetGroundHeight(feanorTarget);

                            float dist = Vector3.Distance(_feanorNpc.transform.position, feanorTarget);
                            if (dist > 0.2f)
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
                                if (_feanorPathIndex >= _feanorPathToBunnyPostDreampearl.Count) SetNpcMoving(_feanorNpc, false);
                            }
                        }
                        else if (_feanorNpc != null)
                        {
                            SetNpcMoving(_feanorNpc, false);
                            if (bunnyObj != null) RotateNpcToFaceTarget(_feanorNpc, bunnyObj);
                        }

                        // Check if all arrived to trigger Bunny Dialogue
                        if (!_bunnyDialoguePostDreampearlStarted)
                        {
                            bool allArrived = true;
                            if (_ronaNpc != null && _ronaPathIndex < _ronaPathToBunnyPostDreampearl.Count) allArrived = false;
                            if (_murialNpc != null && _murialPathIndex < _murialPathToBunnyPostDreampearl.Count) allArrived = false;
                            if (_keikoNpc != null && _keikoPathIndex < _keikoPathToBunnyPostDreampearl.Count) allArrived = false;
                            if (_feanorNpc != null && _feanorPathIndex < _feanorPathToBunnyPostDreampearl.Count) allArrived = false;

                            if (allArrived)
                            {
                                _bunnyDialoguePostDreampearlStarted = true;
                                TriggerBunnyDialoguePostDreampearl();
                            }
                        }
                    }
                    break;

                case IntroState.WaitingForPortalDialogue:
                    if (_startPortalWalk)
                    {
                        GameObject portalObj = GameObject.Find("cube 015");

                        // 1. Rona
                        if (_ronaNpc != null && _ronaPathIndex < _ronaPathToPortal.Count)
                        {
                            Vector2 target2D = _ronaPathToPortal[_ronaPathIndex];
                            float currentY = _ronaNpc.transform.position.y;
                            Vector3 ronaTarget = new Vector3(target2D.x, currentY, target2D.y);
                            ronaTarget.y = GetGroundHeight(ronaTarget);

                            float dist = Vector3.Distance(_ronaNpc.transform.position, ronaTarget);
                            if (dist > 0.2f)
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
                                if (_ronaPathIndex >= _ronaPathToPortal.Count) SetNpcMoving(_ronaNpc, false);
                            }
                        }
                        else if (_ronaNpc != null)
                        {
                            SetNpcMoving(_ronaNpc, false);
                            if (portalObj != null) RotateNpcToFaceTarget(_ronaNpc, portalObj);
                        }

                        // 2. Murial
                        if (_murialNpc != null && _murialPathIndex < _murialPathToPortal.Count)
                        {
                            Vector2 target2D = _murialPathToPortal[_murialPathIndex];
                            float currentY = _murialNpc.transform.position.y;
                            Vector3 murialTarget = new Vector3(target2D.x, currentY, target2D.y);
                            murialTarget.y = GetGroundHeight(murialTarget);

                            float dist = Vector3.Distance(_murialNpc.transform.position, murialTarget);
                            if (dist > 0.2f)
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
                                if (_murialPathIndex >= _murialPathToPortal.Count) SetNpcMoving(_murialNpc, false);
                            }
                        }
                        else if (_murialNpc != null)
                        {
                            SetNpcMoving(_murialNpc, false);
                            if (portalObj != null) RotateNpcToFaceTarget(_murialNpc, portalObj);
                        }

                        // 3. Keiko
                        if (_keikoNpc != null && _keikoPathIndex < _keikoPathToPortal.Count)
                        {
                            Vector2 target2D = _keikoPathToPortal[_keikoPathIndex];
                            float currentY = _keikoNpc.transform.position.y;
                            Vector3 keikoTarget = new Vector3(target2D.x, currentY, target2D.y);
                            keikoTarget.y = GetGroundHeight(keikoTarget);

                            float dist = Vector3.Distance(_keikoNpc.transform.position, keikoTarget);
                            if (dist > 0.2f)
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
                                if (_keikoPathIndex >= _keikoPathToPortal.Count) SetNpcMoving(_keikoNpc, false);
                            }
                        }
                        else if (_keikoNpc != null)
                        {
                            SetNpcMoving(_keikoNpc, false);
                            if (portalObj != null) RotateNpcToFaceTarget(_keikoNpc, portalObj);
                        }

                        // 4. Feanor
                        if (_feanorNpc != null && _feanorPathIndex < _feanorPathToPortal.Count)
                        {
                            Vector2 target2D = _feanorPathToPortal[_feanorPathIndex];
                            float currentY = _feanorNpc.transform.position.y;
                            Vector3 feanorTarget = new Vector3(target2D.x, currentY, target2D.y);
                            feanorTarget.y = GetGroundHeight(feanorTarget);

                            float dist = Vector3.Distance(_feanorNpc.transform.position, feanorTarget);
                            if (dist > 0.2f)
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
                                if (_feanorPathIndex >= _feanorPathToPortal.Count) SetNpcMoving(_feanorNpc, false);
                            }
                        }
                        else if (_feanorNpc != null)
                        {
                            SetNpcMoving(_feanorNpc, false);
                            if (portalObj != null) RotateNpcToFaceTarget(_feanorNpc, portalObj);
                        }

                        // Check if all arrived to trigger Portal Dialogue Part A
                        if (!_portalDialogueStarted)
                        {
                            bool allArrived = true;
                            if (_ronaNpc != null && _ronaPathIndex < _ronaPathToPortal.Count) allArrived = false;
                            if (_murialNpc != null && _murialPathIndex < _murialPathToPortal.Count) allArrived = false;
                            if (_keikoNpc != null && _keikoPathIndex < _keikoPathToPortal.Count) allArrived = false;
                            if (_feanorNpc != null && _feanorPathIndex < _feanorPathToPortal.Count) allArrived = false;

                            if (allArrived)
                            {
                                _portalDialogueStarted = true;
                                TriggerPortalDialoguePartA();
                            }
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
                HasMurialFallen = true;
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
            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.ResetSwapStateToKael();
            }
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
                    
                    // Set path indices to 99 to prevent walking until player interacts with dobj.001
                    _ronaPathIndex = 99;
                    _murialPathIndex = 99;
                    _keikoPathIndex = 99;
                    _feanorPathIndex = 99;

                    _state = IntroState.Completed;
                    IsIntroCompleted = true; // Unlock character swaps!
                    _crystalObject = FindCrystalObject();
                    
                    Debug.Log("[NocturneIntroController] Intro flow sequence completed! Waiting for player to interact with dobj.001.");
                    break;

                case IntroState.SomniaSeedPart1:
                    HasSomniaSeedPart1Ended = true;
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(2, true);
                        CharacterSwapManager.Instance.SwapToCharacter(2);
                    }
                    SetPlayerMovementEnabled(true);
                    
                    GameObject rock1 = GameObject.Find("rockpuzzle1");
                    if (rock1 != null)
                    {
                        var inter = rock1.GetComponent<Interactable>();
                        if (inter != null)
                        {
                            inter.PromptText = "Remove Stone (E)";
                            inter.enabled = true;
                        }
                    }
                    Debug.Log("[NocturneIntroController] Somnia Seed Part 1 ended. Forced swap to Murial.");
                    break;

                case IntroState.SomniaSeedPart2:
                    SetPlayerMovementEnabled(true);
                    HasDialogueSomniaEnded = true;

                    GameObject dobj001 = FindCrystalByName("dobj.001");
                    if (dobj001 != null)
                    {
                        var col = dobj001.GetComponent<Collider>();
                        if (col != null) col.enabled = true;
                        
                        var inter = dobj001.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = true;
                    }

                    GameObject rock1Obj = GameObject.Find("rockpuzzle1");
                    if (rock1Obj != null)
                    {
                        var inter = rock1Obj.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = false;
                    }

                    SetPuzzle2InteractionActive(true);
                    Debug.Log("[NocturneIntroController] Somnia Seed Part 2 ended. Crystal is now collectable.");
                    break;

                case IntroState.CrescentTearPart1:
                    HasCrescentTearPart1Ended = true;
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(2, true);
                        CharacterSwapManager.Instance.SwapToCharacter(2);
                    }
                    SetPlayerMovementEnabled(true);
                    
                    GameObject p2Ip1 = GameObject.Find("Puzzle2InteractionPoint");
                    if (p2Ip1 != null)
                    {
                        var inter = p2Ip1.GetComponent<Interactable>();
                        if (inter != null)
                        {
                            inter.PromptText = "Rip Vines (E)";
                            inter.HoldSeconds = 3.0f;
                            inter.enabled = true;
                        }
                    }
                    Debug.Log("[NocturneIntroController] Crescent Tear Part 1 ended. Forced swap to Murial.");
                    break;

                case IntroState.CrescentTearPart2:
                    HasCrescentTearPart2Ended = true;
                    if (CharacterSwapManager.Instance != null)
                    {
                        CharacterSwapManager.Instance.SetCharacterUnlocked(4, true);
                        CharacterSwapManager.Instance.SwapToCharacter(4);
                    }
                    SetPlayerMovementEnabled(true);
                    
                    GameObject p2Ip2 = GameObject.Find("Puzzle2InteractionPoint");
                    if (p2Ip2 != null)
                    {
                        var inter = p2Ip2.GetComponent<Interactable>();
                        if (inter != null)
                        {
                            inter.PromptText = "Untangle Vines (E)";
                            inter.HoldSeconds = 3.0f;
                            inter.enabled = true;
                        }
                    }
                    Debug.Log("[NocturneIntroController] Crescent Tear Part 2 ended. Forced swap to Feanor.");
                    break;

                case IntroState.CrescentTearPart3:
                    SetPlayerMovementEnabled(true);
                    HasCrescentTearPart3Ended = true;

                    GameObject p2Ip3 = GameObject.Find("Puzzle2InteractionPoint");
                    if (p2Ip3 != null)
                    {
                        var inter = p2Ip3.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = false;
                    }

                    GameObject dobjObj = GameObject.Find("dobj");
                    if (dobjObj != null)
                    {
                        var col = dobjObj.GetComponent<Collider>();
                        if (col != null) col.enabled = true;
                        
                        var inter = dobjObj.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = true;
                    }

                    Debug.Log("[NocturneIntroController] Crescent Tear Part 3 ended. Crystal is now collectable.");
                    break;

                case IntroState.CrescentTearCollectedDialogue:
                    SetPlayerMovementEnabled(true);
                    SetBridgeInteractionActive(true, "Press E to Interact", 0f);
                    Debug.Log("[NocturneIntroController] Crescent Tear collected dialogue ended. Enabled first bridge interaction.");
                    break;

                case IntroState.SomniaSeedCollectedDialogue:
                    SetPlayerMovementEnabled(true);
                    Debug.Log("[NocturneIntroController] Somnia Seed collected dialogue ended.");
                    break;

                case IntroState.BridgeIntroDialogue:
                    HasBridgeIntroEnded = true;
                    SetPlayerMovementEnabled(true);
                    SetBridgeInteractionActive(true, "Press E to fix bridge", 3.0f);
                    Debug.Log("[NocturneIntroController] Bridge intro dialogue ended. Enabled fix bridge interaction.");
                    break;

                case IntroState.BridgeSuccessDialogue:
                    SetPlayerMovementEnabled(true);
                    SetBridgeInteractionActive(false, "", 0f);
                    GameObject p3Ip = GameObject.Find("Puzzle3InteractionPoint");
                    if (p3Ip != null)
                    {
                        var inter = p3Ip.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = true;
                    }
                    Debug.Log("[NocturneIntroController] Bridge success dialogue ended. Activated Puzzle3InteractionPoint.");
                    break;

                case IntroState.Puzzle3IntroDialogue:
                    HasPuzzle3IntroEnded = true;
                    SetPlayerMovementEnabled(true);
                    GameObject p3IpObj = GameObject.Find("Puzzle3InteractionPoint");
                    if (p3IpObj != null)
                    {
                        var inter = p3IpObj.GetComponent<Interactable>();
                        if (inter != null)
                        {
                            inter.PromptText = "Press E to create bridge";
                            inter.HoldSeconds = 3.0f;
                            inter.enabled = true;
                        }
                    }
                    Debug.Log("[NocturneIntroController] Puzzle 3 intro dialogue ended. Prompt changed to Press E to create bridge.");
                    break;

                case IntroState.Puzzle3SuccessDialogue:
                    SetPlayerMovementEnabled(true);
                    GameObject p3IpObj2 = GameObject.Find("Puzzle3InteractionPoint");
                    if (p3IpObj2 != null)
                    {
                        var inter = p3IpObj2.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = false;
                    }

                    GameObject dobj002 = FindCrystalByName("dobj.002");
                    if (dobj002 != null)
                    {
                        var col = dobj002.GetComponent<Collider>();
                        if (col != null) col.enabled = true;
                        var inter = dobj002.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = true;
                    }
                    Debug.Log("[NocturneIntroController] Puzzle 3 success dialogue ended. Dreampearl is now collectable.");
                    break;

                case IntroState.Puzzle3CollectedDialogue:
                    SetPlayerMovementEnabled(true);
                    Debug.Log("[NocturneIntroController] Puzzle 3 collected dialogue ended.");
                    break;

                case IntroState.BunnyDialoguePostDreampearl:
                    OnBunnyDialoguePostDreampearlEnded();
                    break;

                case IntroState.PortalDialoguePartA:
                    StartCoroutine(SmoothPanToEmptyTableRoutine());
                    break;

                case IntroState.PortalDialoguePartB:
                    RestoreCameraToPlayer();
                    TriggerPortalDialoguePartC();
                    break;

                case IntroState.PortalDialoguePartC:
                    HasPortalFixed = true;
                    SetPlayerMovementEnabled(true);
                    Debug.Log("[NocturneIntroController] Final portal dialogue completed. Free roaming enabled.");
                    break;
            }
        }

        private void SetPuzzle2InteractionActive(bool active)
        {
            GameObject p2Ip = GameObject.Find("Puzzle2InteractionPoint");
            if (p2Ip != null)
            {
                var inter = p2Ip.GetComponent<Interactable>();
                if (inter != null) inter.enabled = active;
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

        private void OnRock1Interacted()
        {
            GameObject rock1 = GameObject.Find("rockpuzzle1");

            if (!HasSomniaSeedPart1Started)
            {
                TriggerSomniaSeedWalkSequence();
            }
            else if (HasSomniaSeedPart1Ended && !HasSomniaSeedPuzzleCompleted)
            {
                // If they interact with the rock again, and they are Murial:
                if (CharacterSwapManager.Instance != null && CharacterSwapManager.Instance.ActiveCharacterIndex == 2)
                {
                    GameObject dobj001 = FindCrystalByName("dobj.001");
                    if (dobj001 != null)
                    {
                        var minigame = dobj001.GetComponent<Nemuri.Interactions.CrystalMinigame>();
                        if (minigame != null)
                        {
                            minigame.StartMinigame();
                        }
                    }
                }
                else
                {
                    if (rock1 != null)
                    {
                        var inter = rock1.GetComponent<Interactable>();
                        if (inter != null)
                        {
                            inter.DisplayInteraction("You must use Murial as player to interact", 0f);
                        }
                    }
                    Debug.Log("[NocturneIntroController] Only Murial can remove the stone!");
                }

                if (rock1 != null)
                {
                    var inter = rock1.GetComponent<Interactable>();
                    if (inter != null) inter.DismissInteraction();
                }
            }
        }

        public void TriggerSomniaSeedWalkSequence()
        {
            if (HasSomniaSeedPart1Started) return;
            HasSomniaSeedPart1Started = true;
            SetPlayerMovementEnabled(false);
            
            // Perform local swap to Kael so Kael appears at the activation spot
            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SwapToCharacter(0, isDialogueSwap: true);
            }

            // Reset path indices to 0 to start NPC walk sequence
            _ronaPathIndex = 0;
            _murialPathIndex = 0;
            _keikoPathIndex = 0;
            _feanorPathIndex = 0;

            _startGemPuzzleWalk = true;
            Debug.Log("[NocturneIntroController] Player interacted with rockpuzzle1! Commencing NPC walking sequence.");
        }

        private void TriggerSomniaSeedPart1Dialogue()
        {
            if (_dialogueJsonSomnia == null)
            {
                _dialogueJsonSomnia = Resources.Load<TextAsset>("Dialogue/nocturne_somnia_seed");
            }
            DialogueSequence seq = JsonUtility.FromJson<DialogueSequence>(_dialogueJsonSomnia.text);
            if (seq == null || seq.nodes == null) return;

            // Part 1: nodes 0 to 5 (D53 - T7)
            List<DialogueNode> part1Nodes = seq.nodes.GetRange(0, 6);

            _state = IntroState.SomniaSeedPart1;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part1Nodes);
            }
        }

        public void OnSomniaSeedPuzzleSuccess()
        {
            if (HasSomniaSeedPuzzleCompleted) return;
            HasSomniaSeedPuzzleCompleted = true;

            SetPlayerMovementEnabled(false);

            GameObject rock1 = GameObject.Find("rockpuzzle1");
            StartCoroutine(SmoothMoveRock1Routine(rock1));
        }

        private IEnumerator SmoothMoveRock1Routine(GameObject rockObj)
        {
            float duration = 2.0f; // Smoothly lower over 2 seconds
            float elapsed = 0f;

            Vector3 startPos = rockObj != null ? rockObj.transform.position : Vector3.zero;
            float startY = startPos.y;
            float endY = startY - 1.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float tSmooth = Mathf.SmoothStep(0f, 1f, t);

                if (rockObj != null)
                {
                    Vector3 currentPos = rockObj.transform.position;
                    currentPos.y = Mathf.Lerp(startY, endY, tSmooth);
                    rockObj.transform.position = currentPos;
                }

                yield return null;
            }

            if (rockObj != null)
            {
                Vector3 finalPos = rockObj.transform.position;
                finalPos.y = endY;
                rockObj.transform.position = finalPos;
            }

            TriggerSomniaSeedPart2Dialogue();
        }

        private void TriggerSomniaSeedPart2Dialogue()
        {
            if (_dialogueJsonSomnia == null)
            {
                _dialogueJsonSomnia = Resources.Load<TextAsset>("Dialogue/nocturne_somnia_seed");
            }
            DialogueSequence seq = JsonUtility.FromJson<DialogueSequence>(_dialogueJsonSomnia.text);
            if (seq == null || seq.nodes == null) return;

            // Part 2: nodes 6 to 9 (N11 - T8)
            List<DialogueNode> part2Nodes = seq.nodes.GetRange(6, seq.nodes.Count - 6);

            _state = IntroState.SomniaSeedPart2;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part2Nodes);
            }
        }

        public void TriggerSomniaSeedCollectedDialogue()
        {
            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode()
                {
                    speaker = "Kael",
                    text = "Nice! We got the Somnia Seed. Now, let's look for the next source of resonance... the Crescent Tear.",
                    portraitName = "Kael",
                    typingSpeed = 0.05f
                }
            };
            
            _state = IntroState.SomniaSeedCollectedDialogue;
            SetPlayerMovementEnabled(false);

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        private void OnPuzzle2Interacted()
        {
            GameObject p2Ip = GameObject.Find("Puzzle2InteractionPoint");
            Interactable inter = null;
            if (p2Ip != null)
            {
                inter = p2Ip.GetComponent<Interactable>();
            }

            if (!HasCrescentTearPart1Started)
            {
                TriggerCrescentTearWalkSequence();
            }
            else if (HasCrescentTearPart1Ended && !HasMurialInteracted)
            {
                // Must be Murial to rip the vines
                if (CharacterSwapManager.Instance != null && CharacterSwapManager.Instance.ActiveCharacterIndex == 2)
                {
                    HasMurialInteracted = true;
                    
                    // Disable interactable temporarily
                    if (inter != null) inter.enabled = false;
                    
                    TriggerCrescentTearPart2Dialogue();
                }
                else
                {
                    if (inter != null)
                    {
                        inter.DisplayInteraction("You must use Murial as player to interact", 0f);
                    }
                    Debug.Log("[NocturneIntroController] Only Murial can rip the vines!");
                }

                if (inter != null) inter.DismissInteraction();
            }
            else if (HasCrescentTearPart2Ended && !HasFeanorInteracted)
            {
                // Must be Feanor to untangle the vines
                if (CharacterSwapManager.Instance != null && CharacterSwapManager.Instance.ActiveCharacterIndex == 4)
                {
                    HasFeanorInteracted = true;

                    // Disable interactable temporarily
                    if (inter != null) inter.enabled = false;

                    TriggerFeanorPuzzle2Interaction();
                }
                else
                {
                    if (inter != null)
                    {
                        inter.DisplayInteraction("You must use Feanor as player to interact", 0f);
                    }
                    Debug.Log("[NocturneIntroController] Only Feanor can untangle the vines!");
                }

                if (inter != null) inter.DismissInteraction();
            }
        }

        private void TriggerCrescentTearWalkSequence()
        {
            if (_startCrescentWalk) return;
            _startCrescentWalk = true;

            SetPlayerMovementEnabled(false);

            // Swap player to Kael (index 0) in-place during sequence
            if (CharacterSwapManager.Instance != null)
            {
                CharacterSwapManager.Instance.SwapToCharacter(0, isDialogueSwap: true);
            }

            // Reset path indices to 0 for the crescent path walk
            _ronaPathIndex = 0;
            _murialPathIndex = 0;
            _keikoPathIndex = 0;
            _feanorPathIndex = 0;

            _state = IntroState.WaitingForCrescentDialogue;

            GameObject p2Ip = GameObject.Find("Puzzle2InteractionPoint");
            if (p2Ip != null)
            {
                var inter = p2Ip.GetComponent<Interactable>();
                if (inter != null) inter.DismissInteraction();
            }

            Debug.Log("[NocturneIntroController] Player interacted with Puzzle2InteractionPoint! Commencing NPC crescent walk sequence.");
        }

        public void TriggerCrescentTearPart1Dialogue()
        {
            if (HasCrescentTearPart1Started) return;
            HasCrescentTearPart1Started = true;

            TextAsset dialogueJson = Resources.Load<TextAsset>("Dialogue/nocturne_crescent_tear");
            if (dialogueJson == null) return;

            DialogueSequence seq = JsonUtility.FromJson<DialogueSequence>(dialogueJson.text);
            if (seq == null || seq.nodes == null) return;

            // Part 1: nodes 0 to 4 (N12 - D61 + T9)
            List<DialogueNode> part1Nodes = seq.nodes.GetRange(0, 5);

            _state = IntroState.CrescentTearPart1;
            SetPlayerMovementEnabled(false);

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part1Nodes);
            }
        }

        public void TriggerFeanorPuzzle2Interaction()
        {
            SetPlayerMovementEnabled(false);

            GameObject cubeObj = null;
            GameObject dobjObj = null;

            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name == "Cube.001 (1)") cubeObj = obj;
                if (obj.name == "dobj") dobjObj = obj;
            }

            if (cubeObj == null) cubeObj = GameObject.Find("Cube.001 (1)");
            if (dobjObj == null) dobjObj = GameObject.Find("dobj");

            StartCoroutine(SmoothMovePuzzle2Routine(cubeObj, dobjObj));
        }

        private void TriggerCrescentTearPart2Dialogue()
        {
            TextAsset dialogueJson = Resources.Load<TextAsset>("Dialogue/nocturne_crescent_tear");
            if (dialogueJson == null) return;

            DialogueSequence seq = JsonUtility.FromJson<DialogueSequence>(dialogueJson.text);
            if (seq == null || seq.nodes == null) return;

            // Part 2: nodes 5 to 14 (SFX: CRACK - T10)
            List<DialogueNode> part2Nodes = seq.nodes.GetRange(5, 10);

            _state = IntroState.CrescentTearPart2;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part2Nodes);
            }
        }

        private IEnumerator SmoothMovePuzzle2Routine(GameObject cubeObj, GameObject dobjObj)
        {
            float duration = 2.0f; // Smooth move over 2 seconds
            float elapsed = 0f;

            Vector3 startCubePos = cubeObj != null ? cubeObj.transform.position : Vector3.zero;
            Vector3 startDobjPos = dobjObj != null ? dobjObj.transform.position : Vector3.zero;

            float startX = startCubePos.x;
            float endX = startX + 5.0f; // Cube.001 (1) X koordinatnya +5 Unit smooth
            float startY = startDobjPos.y;
            float endY = startY - 1.0f; // Y koordinat dari dobj turun 1 unit secara smooth

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float tSmooth = Mathf.SmoothStep(0f, 1f, t);

                if (cubeObj != null)
                {
                    Vector3 currentCubePos = cubeObj.transform.position;
                    currentCubePos.x = Mathf.Lerp(startX, endX, tSmooth);
                    cubeObj.transform.position = currentCubePos;
                }

                if (dobjObj != null)
                {
                    Vector3 currentDobjPos = dobjObj.transform.position;
                    currentDobjPos.y = Mathf.Lerp(startY, endY, tSmooth);
                    dobjObj.transform.position = currentDobjPos;
                }

                yield return null;
            }

            if (cubeObj != null)
            {
                Vector3 finalCubePos = cubeObj.transform.position;
                finalCubePos.x = endX;
                cubeObj.transform.position = finalCubePos;
            }

            if (dobjObj != null)
            {
                Vector3 finalDobjPos = dobjObj.transform.position;
                finalDobjPos.y = endY;
                dobjObj.transform.position = finalDobjPos;
            }

            TriggerCrescentTearPart3Dialogue();
        }

        private void TriggerCrescentTearPart3Dialogue()
        {
            TextAsset dialogueJson = Resources.Load<TextAsset>("Dialogue/nocturne_crescent_tear");
            if (dialogueJson == null) return;

            DialogueSequence seq = JsonUtility.FromJson<DialogueSequence>(dialogueJson.text);
            if (seq == null || seq.nodes == null) return;

            // Part 3: nodes 15 to end (N15 - T11)
            List<DialogueNode> part3Nodes = seq.nodes.GetRange(15, seq.nodes.Count - 15);

            _state = IntroState.CrescentTearPart3;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(part3Nodes);
            }
        }

        public void TriggerCrescentTearCollectedDialogue()
        {
            if (HasCrescentTearCollected) return;
            HasCrescentTearCollected = true;

            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode()
                {
                    speaker = "Kael",
                    text = "Nice! We obtained the Crescent Tear. Next, let's search for the third source... the Dreampearl.",
                    portraitName = "Kael",
                    typingSpeed = 0.05f
                }
            };
            
            _state = IntroState.CrescentTearCollectedDialogue;
            SetPlayerMovementEnabled(false);

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        public void SetBridgeInteractionActive(bool active, string promptText, float holdSeconds)
        {
            BridgeController bridge = FindAnyObjectByType<BridgeController>();
            if (bridge != null)
            {
                var inter = bridge.GetComponent<Interactable>();
                if (inter != null)
                {
                    inter.PromptText = promptText;
                    inter.HoldSeconds = holdSeconds;
                    inter.enabled = active;
                }
            }
        }

        public void OnBridgeInteracted()
        {
            if (!_startBridge1Walk)
            {
                _startBridge1Walk = true;
                
                // Set movement disabled to lock Kael in place
                SetPlayerMovementEnabled(false);

                // Reset indices for the bridge path walk
                _ronaPathIndex = 0;
                _murialPathIndex = 0;
                _keikoPathIndex = 0;
                _feanorPathIndex = 0;

                _state = IntroState.WaitingForBridge1Dialogue;
            }
        }

        public void TriggerBridge1Dialogue()
        {
            if (!HasBridgeIntroStarted)
            {
                HasBridgeIntroStarted = true;
                
                // Show bridge intro dialogue
                List<DialogueNode> nodes = new List<DialogueNode>()
                {
                    new DialogueNode() { speaker = "Narrator", text = "Following the resonance, you arrive at a tranquil lake. At its center rests a giant seashell, tightly closed around a faintly glowing Dreampearl.", portraitName = "", typingSpeed = 0.05f },
                    new DialogueNode() { speaker = "Keiko", text = "It's there... I can hear it.", portraitName = "Keiko", typingSpeed = 0.05f },
                    new DialogueNode() { speaker = "Kael", text = "But there's no way to reach it.", portraitName = "Kael", typingSpeed = 0.05f },
                    new DialogueNode() { speaker = "Narrator", text = "The lake stretches endlessly before you. Every stepping stone has crumbled into the water, leaving the Dreampearl completely inaccessible.", portraitName = "", typingSpeed = 0.05f },
                    new DialogueNode() { speaker = "Feanor", text = "The tremors destroyed the path long ago.", portraitName = "Feanor", typingSpeed = 0.05f },
                    new DialogueNode() { speaker = "Rona", text = "Then we'll make our own.", portraitName = "Rona", typingSpeed = 0.05f }
                };

                _state = IntroState.BridgeIntroDialogue;
                SetPlayerMovementEnabled(false);
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartConversation(nodes);
                }
            }
        }

        public void OnBridgeFixedByRona(BridgeController bridge)
        {
            if (HasBridgeFixed) return;
            HasBridgeFixed = true;

            SetPlayerMovementEnabled(false);
            SetBridgeInteractionActive(false, "", 0f);

            // Call the TriggerBridge on the controller to trigger bridge animators
            if (bridge != null)
            {
                bridge.TriggerBridgePublic();
            }

            StartCoroutine(WaitAndLowerBridgeRoutine());
        }

        private IEnumerator WaitAndLowerBridgeRoutine()
        {
            // Delay 6 seconds for bridge animation
            yield return new WaitForSeconds(6.0f);

            // Trigger Rona dialogue
            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode() { speaker = "Rona", text = "A path isn't something you wait for...", portraitName = "Rona", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Rona", text = "It's something you create.", portraitName = "Rona", typingSpeed = 0.05f }
            };

            _state = IntroState.BridgeSuccessDialogue;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        private void OnPuzzle3Interacted()
        {
            GameObject p3Ip = GameObject.Find("Puzzle3InteractionPoint");

            if (!HasPuzzle3IntroStarted)
            {
                HasPuzzle3IntroStarted = true;

                // Show Puzzle 3 intro dialogue
                List<DialogueNode> nodes = new List<DialogueNode>()
                {
                    new DialogueNode() { speaker = "Kael", text = "Oh, the pearl is still too far. We need another bridge.", portraitName = "Kael", typingSpeed = 0.05f },
                    new DialogueNode() { speaker = "Rona", text = "Fine, I will prove that we're worthy.", portraitName = "Rona", typingSpeed = 0.05f }
                };

                _state = IntroState.Puzzle3IntroDialogue;
                SetPlayerMovementEnabled(false);
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartConversation(nodes);
                }

                if (p3Ip != null)
                {
                    var inter = p3Ip.GetComponent<Interactable>();
                    if (inter != null) inter.DismissInteraction();
                }
            }
            else if (HasPuzzle3IntroEnded && !HasPuzzle3BridgeCreated)
            {
                // Must be Rona to build the second bridge
                if (CharacterSwapManager.Instance != null && CharacterSwapManager.Instance.ActiveCharacterIndex == 1)
                {
                    HasPuzzle3BridgeCreated = true;

                    // Disable interactable temporarily
                    if (p3Ip != null)
                    {
                        var inter = p3Ip.GetComponent<Interactable>();
                        if (inter != null) inter.enabled = false;
                    }

                    TriggerPuzzle3BridgeSuccess();
                }
                else
                {
                    Debug.Log("[NocturneIntroController] Only Rona can create the second bridge!");
                }

                if (p3Ip != null)
                {
                    var inter = p3Ip.GetComponent<Interactable>();
                    if (inter != null) inter.DismissInteraction();
                }
            }
        }

        private GameObject FindPuzzle3Bridge()
        {
            GameObject bridge = GameObject.Find("PuzzleBridge");
            if (bridge == null) bridge = GameObject.Find("puzzle bridge");
            return bridge;
        }

        private void TriggerPuzzle3BridgeSuccess()
        {
            SetPlayerMovementEnabled(false);

            GameObject p3Bridge = FindPuzzle3Bridge();
            StartCoroutine(SmoothMovePuzzle3BridgeRoutine(p3Bridge));
        }

        private struct RendererMatInfo
        {
            public Renderer r;
            public Material m;
            public Color origColor;
        }

        private IEnumerator SmoothMovePuzzle3BridgeRoutine(GameObject p3Bridge)
        {
            float duration = 3.0f; // Smooth move and fade-in over 3 seconds
            float elapsed = 0f;

            Vector3 startPos = p3Bridge != null ? p3Bridge.transform.position : Vector3.zero;
            float startY = 0.92f;
            float endY = 2.985f; // puzzle bridge Y dari 0.92 ke 2.985

            // Setup fade-in materials if they are URP
            Renderer[] renderers = p3Bridge != null ? p3Bridge.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            
            // First pass: store original materials/colors and set to transparent blend
            List<RendererMatInfo> matInfos = new List<RendererMatInfo>();
            
            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                {
                    Material mat = r.material;
                    // Switch to transparent mode in URP Lit
                    mat.SetFloat("_Surface", 1f); // 1 is transparent
                    mat.SetFloat("_Blend", 0f);   // 0 is alpha blend
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    Color c = mat.color;
                    c.a = 0f;
                    mat.color = c;

                    matInfos.Add(new RendererMatInfo { r = r, m = mat, origColor = mat.color });
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float tSmooth = Mathf.SmoothStep(0f, 1f, t);

                if (p3Bridge != null)
                {
                    Vector3 currentPos = p3Bridge.transform.position;
                    currentPos.y = Mathf.Lerp(startY, endY, tSmooth);
                    p3Bridge.transform.position = currentPos;
                }

                // Smoothly fade-in renderers
                foreach (var info in matInfos)
                {
                    if (info.m != null)
                    {
                        Color c = info.origColor;
                        c.a = Mathf.Lerp(0f, 1f, tSmooth);
                        info.m.color = c;
                    }
                }

                yield return null;
            }

            if (p3Bridge != null)
            {
                Vector3 finalPos = p3Bridge.transform.position;
                finalPos.y = endY;
                p3Bridge.transform.position = finalPos;
            }

            // Restore opaque shaders and full alpha
            foreach (var info in matInfos)
            {
                if (info.m != null)
                {
                    Color c = info.origColor;
                    c.a = 1f;
                    info.m.color = c;
                    
                    // Switch back to opaque mode
                    info.m.SetFloat("_Surface", 0f); // 0 is opaque
                    info.m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    info.m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    info.m.SetInt("_ZWrite", 1);
                    info.m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    info.m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                }
            }

            // Trigger Puzzle 3 success dialogue: Keiko & Objective
            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode() { speaker = "Keiko", text = "The bridge appeared, let's get the pearl.", portraitName = "Keiko", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Objective", text = "Obtain the Dreampearl.", portraitName = "", typingSpeed = 0.05f }
            };

            _state = IntroState.Puzzle3SuccessDialogue;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        public void TriggerPuzzle3CollectedDialogue()
        {
            if (HasPuzzle3Collected) return;
            HasPuzzle3Collected = true;

            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode()
                {
                    speaker = "Rona",
                    text = "We're finally done! Let's go back now!",
                    portraitName = "Rona",
                    typingSpeed = 0.05f
                }
            };
            
            _state = IntroState.Puzzle3CollectedDialogue;
            SetPlayerMovementEnabled(false);

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        private GameObject FindCrystalObject()
        {
            GameObject pg = GameObject.Find("PINEALGRAND");
            if (pg != null)
            {
                return FindChildRecursive(pg.transform, "dobj.001");
            }
            return GameObject.Find("dobj.001");
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
            string[] names = { "dobj.001", "dobj", "dobj.002" };
            foreach (var name in names)
            {
                GameObject crystal = FindCrystalByName(name);
                if (crystal != null)
                {
                    bool isCompleted = false;
                    if (name == "dobj.001") isCompleted = HasSomniaSeedPuzzleCompleted;
                    else if (name == "dobj") isCompleted = HasCrescentTearPart3Ended;
                    else if (name == "dobj.002") isCompleted = HasCrescentTearCollected;

                    bool shouldEnable = active && isCompleted;

                    var colliders = crystal.GetComponentsInChildren<Collider>(true);
                    foreach (var col in colliders)
                    {
                        col.enabled = shouldEnable;
                    }

                    var interactable = crystal.GetComponent<Interactable>();
                    if (interactable != null)
                    {
                        interactable.enabled = shouldEnable;
                    }
                }
            }
        }

        private GameObject FindCrystalByName(string name)
        {
            GameObject pg = GameObject.Find("PINEALGRAND");
            if (pg != null)
            {
                GameObject found = FindChildRecursive(pg.transform, name);
                if (found != null) return found;
            }
            return GameObject.Find(name);
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
            IsPlayerMovementActive = enabled;
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

            var pm = npc.GetComponent<Nemuri.Player.PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = false;
            }

            var pmC1 = npc.GetComponent<Nemuri.Player.PlayerMovementChapt1>();
            if (pmC1 != null)
            {
                pmC1.enabled = false;
            }
        }

        public static float GetGroundHeight(Vector3 position)
        {
            Ray ray = new Ray(new Vector3(position.x, position.y + 15f, position.z), Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 40f, 1 << 0);
            
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
                    nameLower.Contains("waypoint"))
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

        private void SnapToGround(GameObject npc)
        {
            if (npc == null) return;
            Vector3 pos = npc.transform.position;
            pos.y = GetGroundHeight(pos);
            npc.transform.position = pos;
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

        public void TriggerBunnyWalkPostDreampearlSequence()
        {
            if (_startBunnyWalkPostDreampearl) return;
            _startBunnyWalkPostDreampearl = true;

            SetPlayerMovementEnabled(false);

            // Reset path indices to 0 for the bunny walk
            _ronaPathIndex = 0;
            _murialPathIndex = 0;
            _keikoPathIndex = 0;
            _feanorPathIndex = 0;

            _state = IntroState.WaitingForBunnyDialoguePostDreampearl;
            Debug.Log("[NocturneIntroController] Player approached bunny post-Dreampearl! NPCs commencing walk sequence.");
        }

        public void TriggerBunnyDialoguePostDreampearl()
        {
            if (_bunnyDialoguePostDreampearlStarted) return;
            _bunnyDialoguePostDreampearlStarted = true;

            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode() { speaker = "Ferry", text = "I’m surprised you got it all back, wonderful!", portraitName = "", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Rona", text = "Will this fix the Nocturne heart?", portraitName = "Rona", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Ferry", text = "It’s not that simple dear…", portraitName = "", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Murial", text = "What do you mean?", portraitName = "Murial", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Ferry", text = "Just go ahead and put the crystals and you’ll find out by yourself", portraitName = "", typingSpeed = 0.05f }
            };

            _state = IntroState.BunnyDialoguePostDreampearl;
            SetPlayerMovementEnabled(false);
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        private void OnBunnyDialoguePostDreampearlEnded()
        {
            SetPlayerMovementEnabled(true);
            HasBunnyDialogueEnded = true;
            _state = IntroState.Completed; // Return to Completed state for free roaming

            // Configure the portal interactable!
            GameObject cube015 = GameObject.Find("cube 015");
            if (cube015 == null) cube015 = GameObject.Find("cube.015");
            if (cube015 == null) cube015 = FindPortalObject();

            if (cube015 != null)
            {
                var inter = cube015.GetComponent<Interactable>();
                if (inter == null) inter = cube015.AddComponent<Interactable>();
                inter.PromptText = "Fix Portal (E)";
                inter.InteractionRange = 4.0f;
                inter.HoldSeconds = 0f;
                if (inter.OnInteract == null) inter.OnInteract = new UnityEngine.Events.UnityEvent();
                inter.OnInteract.RemoveAllListeners();
                inter.OnInteract.AddListener(OnPortalInteracted);
                inter.enabled = true;
            }
            Debug.Log("[NocturneIntroController] Bunny dialogue post-Dreampearl ended. Portal interactable enabled.");
        }

        private void OnPortalInteracted()
        {
            // Only Kael can fix the portal
            if (CharacterSwapManager.Instance != null && CharacterSwapManager.Instance.ActiveCharacterIndex == 0)
            {
                // Disable interactable
                GameObject cube015 = GameObject.Find("cube 015");
                if (cube015 == null) cube015 = GameObject.Find("cube.015");
                if (cube015 == null) cube015 = FindPortalObject();

                if (cube015 != null)
                {
                    var inter = cube015.GetComponent<Interactable>();
                    if (inter != null) inter.enabled = false;
                }

                // Hide the metarig bunny NPC (Ferry disappears!)
                GameObject pg = GameObject.Find("PINEALGRAND");
                if (pg != null)
                {
                    Transform go1 = pg.transform.Find("GameObject (1)");
                    if (go1 != null)
                    {
                        Transform metarig = go1.Find("metarig");
                        if (metarig != null) metarig.gameObject.SetActive(false);
                    }
                }

                // Start portal walk sequence!
                TriggerPortalWalkSequence();
            }
            else
            {
                GameObject cube015 = GameObject.Find("cube 015");
                if (cube015 == null) cube015 = GameObject.Find("cube.015");
                if (cube015 == null) cube015 = FindPortalObject();

                if (cube015 != null)
                {
                    var inter = cube015.GetComponent<Interactable>();
                    if (inter != null)
                    {
                        inter.DisplayInteraction("You must use Kael as player to interact", 0f);
                    }
                }
            }
        }

        public void TriggerPortalWalkSequence()
        {
            if (_startPortalWalk) return;
            _startPortalWalk = true;

            SetPlayerMovementEnabled(false);

            // Reset path indices to 0 for the portal walk
            _ronaPathIndex = 0;
            _murialPathIndex = 0;
            _keikoPathIndex = 0;
            _feanorPathIndex = 0;

            _state = IntroState.WaitingForPortalDialogue;
            Debug.Log("[NocturneIntroController] Kael fixed portal! NPCs commencing walk sequence to portal.");
        }

        public void TriggerPortalDialoguePartA()
        {
            if (_portalDialogueStarted) return;
            _portalDialogueStarted = true;

            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode() { speaker = "Keiko", text = "We did it!", portraitName = "Keiko", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Kael", text = "I feel a lot better now!", portraitName = "Kael", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Rona", text = "It really did fix something inside of you Kael!", portraitName = "Rona", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Feanor", text = "Hm… Looking at the portal it seems were not done yet.", portraitName = "Feanor", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Kael", text = "Let’s just ask Ferry!", portraitName = "Kael", typingSpeed = 0.05f }
            };

            _state = IntroState.PortalDialoguePartA;
            SetPlayerMovementEnabled(false);
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        private IEnumerator SmoothPanToEmptyTableRoutine()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.canProceed = false;
            }

            var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null) brain.enabled = false;

            Vector3 startPos = Camera.main.transform.position;
            Quaternion startRot = Camera.main.transform.rotation;

            Vector3 targetPos = _ferryInitialPosition + new Vector3(-6f, 4f, 5f);
            Vector3 lookDir = (_ferryInitialPosition + Vector3.up * 1f - targetPos).normalized;
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

            // Start Part B of dialogue!
            TriggerPortalDialoguePartB();
        }

        public void TriggerPortalDialoguePartB()
        {
            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode() { speaker = "Narrator", text = "You looked at the table, it was not floating anymore and there is no signs of Ferry", portraitName = "", typingSpeed = 0.05f }
            };

            _state = IntroState.PortalDialoguePartB;
            SetPlayerMovementEnabled(false);
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }

        public void TriggerPortalDialoguePartC()
        {
            List<DialogueNode> nodes = new List<DialogueNode>()
            {
                new DialogueNode() { speaker = "Kael", text = "Where did he go?", portraitName = "Kael", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Feanor", text = "We never know…", portraitName = "Feanor", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Rona", text = "Let’s just go into the portal and see what it does", portraitName = "Rona", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Keiko", text = "Are we just going to leave Ferry?", portraitName = "Keiko", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Murial", text = "He will be fine!", portraitName = "Murial", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Kael", text = "Yeah, I got a feeling he knows more than we do… I’m sure he was telling us to go further into the portal!", portraitName = "Kael", typingSpeed = 0.05f },
                new DialogueNode() { speaker = "Rona", text = "Let’s go then!", portraitName = "Rona", typingSpeed = 0.05f }
            };

            _state = IntroState.PortalDialoguePartC;
            SetPlayerMovementEnabled(false);
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(nodes);
            }
        }
    }
}
