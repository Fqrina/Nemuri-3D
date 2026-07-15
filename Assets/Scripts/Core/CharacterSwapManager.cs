using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Nemuri.Dialogue;
using Nemuri.Player;

namespace Nemuri.Core
{
    public class CharacterSwapManager : MonoBehaviour
    {
        [System.Serializable]
        public class CharacterBinding
        {
            public string characterName;
            public GameObject playerObject;
            public GameObject npcObject;
        }

        public static CharacterSwapManager Instance { get; private set; }

        [Header("Character Configurations")]
        [SerializeField] private List<CharacterBinding> _characters = new List<CharacterBinding>();
        [SerializeField] private int _activeCharacterIndex = 0;

        [Header("Camera Configurations")]
        [SerializeField] private FixedWorldOffsetCamera _followCamera;

        private int _characterIndexBeforeDialogue = 0;

        [Header("Unlock States")]
        [SerializeField] private bool[] _unlockedCharacters = new bool[] { true, true, true, false, false };

        public void SetCharacterUnlocked(int index, bool unlocked)
        {
            if (index >= 0 && index < _unlockedCharacters.Length)
            {
                _unlockedCharacters[index] = unlocked;
            }
        }

        public bool IsCharacterUnlocked(int index)
        {
            if (index >= 0 && index < _unlockedCharacters.Length)
            {
                return _unlockedCharacters[index];
            }
            return false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeCharacters();
        }

        private void OnEnable()
        {
            DialogueManager.OnConversationStart += HandleConversationStart;
            DialogueManager.OnConversationEnd += HandleConversationEnd;
        }

        private void OnDisable()
        {
            DialogueManager.OnConversationStart -= HandleConversationStart;
            DialogueManager.OnConversationEnd -= HandleConversationEnd;
        }

        private void Update()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsConversationActive)
            {
                return;
            }

            HandleInput();
        }

        private void InitializeCharacters()
        {
            if (_characters == null || _characters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _characters.Count; i++)
            {
                bool isActive = (i == _activeCharacterIndex);
                
                if (_characters[i].playerObject != null)
                {
                    _characters[i].playerObject.SetActive(isActive);
                }

                // Keep all NPC GameObjects active at all times!
                if (_characters[i].npcObject != null)
                {
                    _characters[i].npcObject.SetActive(true);
                }
            }

            if (_characters[_activeCharacterIndex].playerObject != null)
            {
                UpdateCameraTargets(_characters[_activeCharacterIndex].playerObject.transform);
            }
        }

        private void HandleInput()
        {
            if (!Nemuri.Scenes.NocturneIntroController.IsIntroCompleted)
            {
                return;
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame && IsCharacterUnlocked(0)) SwapToCharacter(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame && IsCharacterUnlocked(1)) SwapToCharacter(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame && IsCharacterUnlocked(2)) SwapToCharacter(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame && IsCharacterUnlocked(3)) SwapToCharacter(3);
            else if (Keyboard.current.digit5Key.wasPressedThisFrame && IsCharacterUnlocked(4)) SwapToCharacter(4);
        }

        public void SwapToCharacter(int index, bool isInternalSwap = false)
        {
            if (_characters == null || index < 0 || index >= _characters.Count)
            {
                return;
            }

            if (index == _activeCharacterIndex)
            {
                return;
            }

            GameObject currentCharacterObj = _characters[_activeCharacterIndex].playerObject;
            GameObject targetCharacterObj = _characters[index].playerObject;

            if (currentCharacterObj == null || targetCharacterObj == null)
            {
                return;
            }

            targetCharacterObj.transform.position = currentCharacterObj.transform.position;
            targetCharacterObj.transform.rotation = currentCharacterObj.transform.rotation;

            currentCharacterObj.SetActive(false);
            targetCharacterObj.SetActive(true);

            // Do NOT deactivate target or previous npcObject when swapping! Keep all NPCs active!

            UpdateCameraTargets(targetCharacterObj.transform);

            _activeCharacterIndex = index;
        }

        private void UpdateCameraTargets(Transform targetTransform)
        {
            if (_followCamera != null)
            {
                _followCamera.target = targetTransform;
            }

            var virtualCameras = FindObjectsByType<Cinemachine.CinemachineVirtualCamera>();
            foreach (var vCam in virtualCameras)
            {
                if (vCam != null)
                {
                    vCam.Follow = targetTransform;
                    vCam.LookAt = targetTransform;
                }
            }
        }

        private void HandleConversationStart()
        {
            _characterIndexBeforeDialogue = _activeCharacterIndex;

            if (_activeCharacterIndex != 0)
            {
                SwapToCharacter(0, isInternalSwap: true);
            }
        }

        private void HandleConversationEnd()
        {
            if (_activeCharacterIndex != _characterIndexBeforeDialogue)
            {
                SwapToCharacter(_characterIndexBeforeDialogue, isInternalSwap: true);
            }
        }
    }
}
