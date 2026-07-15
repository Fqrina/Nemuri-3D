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
                
                // Automatically find npcObject if null in Inspector
                if (_characters[i].npcObject == null)
                {
                    string searchName = i == 0 ? "KAELNPC" : _characters[i].characterName.Replace("CHARA", "").Trim() + "NPC";
                    GameObject found = GameObject.Find(searchName);
                    if (found == null) found = GameObject.Find(searchName.Replace("NPC", " NPC"));
                    if (found != null) _characters[i].npcObject = found;
                }

                if (_characters[i].playerObject != null)
                {
                    _characters[i].playerObject.SetActive(isActive);
                }

                if (_characters[i].npcObject != null)
                {
                    _characters[i].npcObject.SetActive(!isActive);
                    if (!isActive)
                    {
                        SnapToGround(_characters[i].npcObject);
                    }
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

            if (Keyboard.current.digit1Key.wasPressedThisFrame && IsCharacterUnlocked(0) && Nemuri.Scenes.NocturneIntroController.CanSwapTo(0)) SwapToCharacter(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame && IsCharacterUnlocked(1) && Nemuri.Scenes.NocturneIntroController.CanSwapTo(1)) SwapToCharacter(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame && IsCharacterUnlocked(2) && Nemuri.Scenes.NocturneIntroController.CanSwapTo(2)) SwapToCharacter(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame && IsCharacterUnlocked(3) && Nemuri.Scenes.NocturneIntroController.CanSwapTo(3)) SwapToCharacter(3);
            else if (Keyboard.current.digit5Key.wasPressedThisFrame && IsCharacterUnlocked(4) && Nemuri.Scenes.NocturneIntroController.CanSwapTo(4)) SwapToCharacter(4);
        }

        public void SwapToCharacter(int index, bool isDialogueSwap = false)
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

            // Automatically find target npcObject if null in Inspector
            if (_characters[index].npcObject == null)
            {
                string searchName = index == 0 ? "KAELNPC" : _characters[index].characterName.Replace("CHARA", "").Trim() + "NPC";
                GameObject found = GameObject.Find(searchName);
                if (found == null) found = GameObject.Find(searchName.Replace("NPC", " NPC"));
                if (found != null) _characters[index].npcObject = found;
            }

            // Automatically find previous npcObject if null in Inspector
            if (_characters[_activeCharacterIndex].npcObject == null)
            {
                string searchName = _activeCharacterIndex == 0 ? "KAELNPC" : _characters[_activeCharacterIndex].characterName.Replace("CHARA", "").Trim() + "NPC";
                GameObject found = GameObject.Find(searchName);
                if (found == null) found = GameObject.Find(searchName.Replace("NPC", " NPC"));
                if (found != null) _characters[_activeCharacterIndex].npcObject = found;
            }

            Transform walkingPlayer = currentCharacterObj.transform.parent;
            if (walkingPlayer == null) walkingPlayer = transform;

            // Temporarily disable CharacterController on Walking Player during teleportation
            var pcCc = walkingPlayer.GetComponent<CharacterController>();
            bool pcCcWasEnabled = false;
            if (pcCc != null)
            {
                pcCcWasEnabled = pcCc.enabled;
                pcCc.enabled = false;
            }

            if (isDialogueSwap)
            {
                // Dialogue Mode: local swap at current location (appear where active)
                currentCharacterObj.SetActive(false);
                targetCharacterObj.SetActive(true);

                GameObject previousNpc = _characters[_activeCharacterIndex].npcObject;
                if (previousNpc != null)
                {
                    SetNpcPositionAndRotation(previousNpc, walkingPlayer.position, walkingPlayer.rotation);
                    SnapToGround(previousNpc);
                    previousNpc.SetActive(true);
                }

                GameObject targetNpc = _characters[index].npcObject;
                if (targetNpc != null)
                {
                    targetNpc.SetActive(false);
                }
            }
            else
            {
                // Play Mode: teleport to target NPC position, swap active bodies
                Vector3 oldPlayerPos = walkingPlayer.position;
                Quaternion oldPlayerRot = walkingPlayer.rotation;

                GameObject targetNpc = _characters[index].npcObject;
                if (targetNpc != null)
                {
                    walkingPlayer.position = targetNpc.transform.position;
                    walkingPlayer.rotation = targetNpc.transform.rotation;
                    targetNpc.SetActive(false);
                }

                currentCharacterObj.SetActive(false);
                targetCharacterObj.SetActive(true);

                GameObject previousNpc = _characters[_activeCharacterIndex].npcObject;
                if (previousNpc != null)
                {
                    SetNpcPositionAndRotation(previousNpc, oldPlayerPos, oldPlayerRot);
                    SnapToGround(previousNpc);
                    previousNpc.SetActive(true);
                }
            }

            // Restore CharacterController on Walking Player
            if (pcCc != null)
            {
                pcCc.enabled = pcCcWasEnabled;
            }

            UpdateCameraTargets(targetCharacterObj.transform);

            _activeCharacterIndex = index;
        }

        private void SetNpcPositionAndRotation(GameObject npc, Vector3 position, Quaternion rotation)
        {
            if (npc == null) return;
            var cc = npc.GetComponent<CharacterController>();
            if (cc == null) cc = npc.GetComponentInChildren<CharacterController>();

            bool ccWasEnabled = false;
            if (cc != null)
            {
                ccWasEnabled = cc.enabled;
                cc.enabled = false;
            }

            npc.transform.position = position;
            npc.transform.rotation = rotation;

            if (cc != null)
            {
                cc.enabled = ccWasEnabled;
            }
        }

        private void SnapToGround(GameObject npc)
        {
            if (npc == null) return;
            Vector3 pos = npc.transform.position;
            Ray ray = new Ray(pos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 30f, 1 << 0))
            {
                pos.y = hit.point.y;
                npc.transform.position = pos;
            }
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

        public void ResetSwapStateToKael()
        {
            _characterIndexBeforeDialogue = 0;
            if (_activeCharacterIndex != 0)
            {
                SwapToCharacter(0, isDialogueSwap: true);
            }
        }

        private void HandleConversationStart()
        {
            _characterIndexBeforeDialogue = _activeCharacterIndex;

            if (_activeCharacterIndex != 0)
            {
                SwapToCharacter(0, isDialogueSwap: true);
            }
        }

        private void HandleConversationEnd()
        {
            if (_activeCharacterIndex != _characterIndexBeforeDialogue)
            {
                SwapToCharacter(_characterIndexBeforeDialogue, isDialogueSwap: true);
            }
        }
    }
}
