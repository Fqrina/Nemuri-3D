using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Nemuri.Dialogue;

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
            // Do not allow manual character switching if a dialogue is active
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

            // Set the initial active character and deactivate the rest
            for (int i = 0; i < _characters.Count; i++)
            {
                bool isActive = (i == _activeCharacterIndex);
                
                if (_characters[i].playerObject != null)
                {
                    _characters[i].playerObject.SetActive(isActive);
                }

                if (_characters[i].npcObject != null)
                {
                    // NPC is active only when its player counterpart is inactive
                    _characters[i].npcObject.SetActive(!isActive);
                }
            }

            if (_characters[_activeCharacterIndex].playerObject != null)
            {
                UpdateCameraTargets(_characters[_activeCharacterIndex].playerObject.transform);
            }
        }

        private void HandleInput()
        {
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

            // Transfer position and rotation from current character to the target character
            targetCharacterObj.transform.position = currentCharacterObj.transform.position;
            targetCharacterObj.transform.rotation = currentCharacterObj.transform.rotation;

            // Handle deactivating current player character and activating target player character
            currentCharacterObj.SetActive(false);
            targetCharacterObj.SetActive(true);

            // Handle active state of their NPC counterparts
            if (_characters[_activeCharacterIndex].npcObject != null)
            {
                // Previous character now coexists as an NPC at their design-placed spot
                _characters[_activeCharacterIndex].npcObject.SetActive(true);
            }

            if (_characters[index].npcObject != null)
            {
                // New character is now controlled by player, so hide their NPC version
                _characters[index].npcObject.SetActive(false);
            }

            // Update camera targets
            UpdateCameraTargets(targetCharacterObj.transform);

            _activeCharacterIndex = index;
        }

        private void UpdateCameraTargets(Transform targetTransform)
        {
            if (_followCamera != null)
            {
                _followCamera.target = targetTransform;
            }

            // Try to find and update Cinemachine virtual cameras
            var virtualCameras = FindObjectsByType<Cinemachine.CinemachineVirtualCamera>(FindObjectsSortMode.None);
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

            // Automatically switch active player character to Kiel (index 0)
            if (_activeCharacterIndex != 0)
            {
                SwapToCharacter(0, isInternalSwap: true);
            }

            // Ensure all other characters' NPC representations are active during dialogue
            for (int i = 1; i < _characters.Count; i++)
            {
                if (_characters[i].npcObject != null)
                {
                    _characters[i].npcObject.SetActive(true);
                }
            }
        }

        private void HandleConversationEnd()
        {
            // Restore character state back to how it was before dialogue
            if (_characters[_activeCharacterIndex].npcObject != null)
            {
                _characters[_activeCharacterIndex].npcObject.SetActive(true);
            }

            if (_characterIndexBeforeDialogue != _activeCharacterIndex)
            {
                SwapToCharacter(_characterIndexBeforeDialogue, isInternalSwap: true);
            }

            // Hide the NPC representation of the active character
            if (_characters[_activeCharacterIndex].npcObject != null)
            {
                _characters[_activeCharacterIndex].npcObject.SetActive(false);
            }
        }
    }
}
