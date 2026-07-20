using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Nemuri.Inventory;
using Nemuri.Scenes;
using Nemuri.Dialogue;
using Nemuri.Player;

namespace Nemuri.UI
{
    public class VisionPuzzleManager : MonoBehaviour
    {
        public static VisionPuzzleManager Instance { get; private set; }

        private bool _isPuzzleActive = false;
        private GameObject _promptCanvas;
        private Text _promptText;
        private readonly System.Collections.Generic.HashSet<ItemGroup> _completedGroups = new System.Collections.Generic.HashSet<ItemGroup>();

        public bool IsPuzzleActive => _isPuzzleActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePromptUI();
        }

        private void Start()
        {
            // ensure the UI script is attached to our Canvas / children
            if (GetComponent<VisionPuzzleUI>() == null)
            {
                gameObject.AddComponent<VisionPuzzleUI>();
            }
        }

        private void InitializePromptUI()
        {
            _promptCanvas = new GameObject("VisionPuzzlePromptCanvas");
            Canvas canvas = _promptCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // below dialogue panel, above normal HUD
            
            CanvasScaler scaler = _promptCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject textGo = new GameObject("PromptText");
            textGo.transform.SetParent(_promptCanvas.transform, false);
            _promptText = textGo.AddComponent<Text>();
            _promptText.text = "Press [C] to reconstruct memory vision";
            _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize = 32;
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.color = new Color(0.9f, 0.85f, 0.7f, 1f); // warm cream
            
            // outline effect
            Outline outline = textGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform txtRect = textGo.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0.5f, 0.5f);
            txtRect.anchorMax = new Vector2(0.5f, 0.5f);
            txtRect.pivot = new Vector2(0.5f, 0.5f);
            txtRect.anchoredPosition = new Vector2(0f, 0f); // middle of the screen
            txtRect.sizeDelta = new Vector2(800f, 80f);

            DontDestroyOnLoad(_promptCanvas);
            _promptCanvas.SetActive(false);
        }

        private bool _wasEligibleLastFrame = false;
        private int _lastCollectedCount = -1;

        private void Update()
        {
            // Old Vision Puzzle UI (full.png / slots.png) and middle-screen prompt are disabled.
            if (_promptCanvas != null && _promptCanvas.activeSelf)
            {
                _promptCanvas.SetActive(false);
            }
        }
        }

        private void LogEligibilityState()
        {
            ItemGroup activeGroup = HippocampusIntroController.Instance != null ? HippocampusIntroController.Instance.CurrentActiveGroup : ItemGroup.Group1;
            bool controllerExists = HippocampusIntroController.Instance != null;
            bool isAtStage = controllerExists && HippocampusIntroController.Instance.IsAtReconstructStage;
            bool inventoryExists = HotbarInventory.Instance != null;
            bool groupCollected = inventoryExists && HotbarInventory.Instance.IsGroupFullyCollected(activeGroup);
            bool dialogueExists = DialogueManager.Instance != null;
            bool dialogueActive = dialogueExists && DialogueManager.Instance.IsConversationActive;

            Debug.Log(string.Format("[VisionPuzzleManager] State - Controller: {0} (Stage ok: {1}), Inventory: {2} (Group {3} Collected: {4}), Dialogue active: {5}",
                controllerExists ? "Found" : "Null",
                isAtStage,
                inventoryExists ? "Found" : "Null",
                activeGroup,
                groupCollected,
                dialogueActive));
        }

        private bool IsPuzzleEligible()
        {
            // check stage in Hippocampus scene
            if (HippocampusIntroController.Instance == null || !HippocampusIntroController.Instance.IsAtReconstructStage)
            {
                return false;
            }

            // check if active group is already completed
            ItemGroup activeGroup = HippocampusIntroController.Instance.CurrentActiveGroup;
            if (_completedGroups.Contains(activeGroup))
            {
                return false;
            }

            // check if active group is fully collected
            if (HotbarInventory.Instance == null || !HotbarInventory.Instance.IsGroupFullyCollected(activeGroup))
            {
                return false;
            }

            // ensure dialog isn't active
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsConversationActive)
            {
                return false;
            }

            return true;
        }

        public void OpenPuzzle()
        {
            if (_isPuzzleActive) return;

            _isPuzzleActive = true;
            _promptCanvas.SetActive(false);

            // unlock and reveal cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // lock player movement and character switching
            SetPlayerMovementEnabled(false);

            // hide the objective panel
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ToggleDialoguePanel(false);
            }

            // Bring hotbar to front on top of puzzle overlay so slots receive drag inputs
            if (HotbarUI.CanvasObject != null)
            {
                Canvas hc = HotbarUI.CanvasObject.GetComponent<Canvas>();
                if (hc != null)
                {
                    hc.sortingOrder = 1100; // Above puzzle canvas (1000)
                }
            }

            // show puzzle UI panel
            if (VisionPuzzleUI.Instance != null)
            {
                VisionPuzzleUI.Instance.SetVisible(true);
            }
        }

        public void ClosePuzzle()
        {
            if (!_isPuzzleActive) return;

            _isPuzzleActive = false;

            // lock cursor back
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // hide puzzle UI panel
            if (VisionPuzzleUI.Instance != null)
            {
                VisionPuzzleUI.Instance.SetVisible(false);
            }

            // Restore hotbar sorting back to normal
            if (HotbarUI.CanvasObject != null)
            {
                Canvas hc = HotbarUI.CanvasObject.GetComponent<Canvas>();
                if (hc != null)
                {
                    hc.sortingOrder = 100;
                }
            }

            // show the objective panel again
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ToggleDialoguePanel(true);
            }

            // restore player movement and character switching
            SetPlayerMovementEnabled(true);
        }

        public void OnPuzzleCompleted()
        {
            if (!_isPuzzleActive) return;

            _isPuzzleActive = false;
            
            ItemGroup activeGroup = HippocampusIntroController.Instance != null ? HippocampusIntroController.Instance.CurrentActiveGroup : ItemGroup.Group1;
            _completedGroups.Add(activeGroup);

            // lock cursor back
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // hide puzzle UI
            if (VisionPuzzleUI.Instance != null)
            {
                VisionPuzzleUI.Instance.SetVisible(false);
            }

            // Restore hotbar sorting back to normal
            if (HotbarUI.CanvasObject != null)
            {
                Canvas hc = HotbarUI.CanvasObject.GetComponent<Canvas>();
                if (hc != null)
                {
                    hc.sortingOrder = 100;
                }
            }

            // Keep the items in the inventory so the player can inspect them later
            activeGroup = HippocampusIntroController.Instance != null ? HippocampusIntroController.Instance.CurrentActiveGroup : ItemGroup.Group1;
            Debug.Log("[VisionPuzzleManager] Keep items in inventory for active group: " + activeGroup);

            // trigger memory reconstruction cutscene and dialogue
            if (HippocampusIntroController.Instance != null)
            {
                HippocampusIntroController.Instance.OnMemoryReconstructed();
            }
        }

        private void SetPlayerMovementEnabled(bool enabled)
        {
            var move1 = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
            foreach (var m in move1)
            {
                m.SetCanMove(enabled);
            }

            var move2 = FindObjectsByType<PlayerMovementChapt1>(FindObjectsInactive.Include);
            foreach (var m in move2)
            {
                m.SetCanMove(enabled);
            }
        }
    }
}
