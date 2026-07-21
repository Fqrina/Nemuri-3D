using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Nemuri.UI
{
    [System.Serializable]
    public class DraggableImageConfig
    {
        [Tooltip("Sprite texture for this overlay layer.")]
        public Sprite Sprite;

        [Tooltip("Can this image be dragged around?")]
        public bool CanMove = true;

        [Tooltip("Can this image be rotated 90 degrees on click?")]
        public bool CanRotate = true;

        [Tooltip("Initial spawn/anchored position relative to center of left panel. If (0,0), an automatic offset will be applied.")]
        public Vector2 SpawnPosition = Vector2.zero;

        [Tooltip("Width and Height (sizeDelta) of this image.")]
        public Vector2 SizeDelta = new Vector2(300f, 300f);

        [Tooltip("Initial rotation angle in degrees (e.g., 0, 90, 180, 270).")]
        public float SpawnRotationAngle = 0f;
    }

    public class OverlayImageMultiRotatablePuzzleUI : MonoBehaviour
    {
        public System.Action OnPuzzleSolved;

        [Header("Canvas Settings")]
        [SerializeField] private int _sortingOrder = 1400;
        [SerializeField] private bool _enableKeyboardToggle = true;

        [Header("Dynamic Overlay Images List (Type B)")]
        [Tooltip("Add as many overlay images as you want! Each can have its own position, size, move/rotate settings, and sprite.")]
        [SerializeField] private List<DraggableImageConfig> _overlayImages = new List<DraggableImageConfig>();

        [Header("Puzzle Configuration (Custom Image Sprites for Slots 1-9)")]
        [Tooltip("Custom Sprites for Keypad Slots 1 to 9.")]
        [SerializeField] private List<Sprite> _pinSprites = new List<Sprite>(9);
        [Tooltip("The set of slot numbers (1-indexed e.g. 1 for Slot 1, 7 for Slot 7, or 0-indexed 0 to 8) that must be selected.")]
        [SerializeField] private List<int> _correctPinIndices = new List<int> { 1, 7 };
        [SerializeField] private UnityEngine.Events.UnityEvent _onPuzzleSolvedEvent;

        [Header("UI Appearance & Background (Optional)")]
        [Tooltip("Custom 1920x1080 background sprite. Drag any background image file here to change the overlay background easily!")]
        [SerializeField] private Sprite _customBackgroundImage;

        [Header("UI References (Optional - Auto-generated if null)")]
        [SerializeField] private Canvas _puzzleCanvas;
        [SerializeField] private GameObject _overlayContainer;
        [SerializeField] private Transform _selectedSequenceContainer;
        [SerializeField] private Text _statusFeedbackText;

        private readonly HashSet<int> _selectedPins = new HashSet<int>();
        private readonly Dictionary<int, GameObject> _pinButtonObjects = new Dictionary<int, GameObject>();
        private readonly List<GameObject> _spawnedSequenceIcons = new List<GameObject>();
        private readonly List<RectTransform> _spawnedDraggableRects = new List<RectTransform>();
        private readonly List<DraggableUIItem> _spawnedDraggableItems = new List<DraggableUIItem>();
        
        private struct ListDraggableItemData
        {
            public DraggableUIItem Item;
            public Vector2 InitialPos;
            public float InitialRot;
        }
        private readonly List<ListDraggableItemData> _itemDataList = new List<ListDraggableItemData>();

        private Font _spinnenkopFont;
        private bool _isOpen;

        private void Awake()
        {
            LoadFont();
            EnsureUIBuilt();

            if (_overlayContainer != null)
            {
                _overlayContainer.SetActive(false);
            }
        }

        private void Update()
        {
            if (_enableKeyboardToggle && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                TogglePuzzleUI();
            }
        }

        public void TogglePuzzleUI()
        {
            _isOpen = !_isOpen;
            if (_overlayContainer != null)
            {
                _overlayContainer.SetActive(_isOpen);
            }

            if (_isOpen)
            {
                ResetPuzzleState();
            }
        }

        public void OpenPuzzleUI()
        {
            _isOpen = true;
            if (_overlayContainer != null)
            {
                _overlayContainer.SetActive(true);
            }
            ResetPuzzleState();
        }

        public void ClosePuzzleUI()
        {
            _isOpen = false;
            if (_overlayContainer != null)
            {
                _overlayContainer.SetActive(false);
            }
        }

        private void ResetPuzzleState()
        {
            _selectedPins.Clear();
            UpdateSequenceDisplay();

            if (_statusFeedbackText != null)
            {
                _statusFeedbackText.text = "";
            }

            foreach (var kvp in _pinButtonObjects)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(true);
                }
            }

            foreach (var itemData in _itemDataList)
            {
                if (itemData.Item != null)
                {
                    itemData.Item.ResetTransform();
                }
            }
        }

        public void OnPinClicked(int pinIndex)
        {
            if (_statusFeedbackText != null && _statusFeedbackText.text == "wrong")
            {
                _statusFeedbackText.text = "";
            }

            _selectedPins.Add(pinIndex);
            UpdateSequenceDisplay();

            if (_pinButtonObjects.TryGetValue(pinIndex, out GameObject btnObj) && btnObj != null)
            {
                btnObj.SetActive(false);
            }
        }

        public void OnSubmitClicked()
        {
            if (ValidatePins())
            {
                if (_statusFeedbackText != null)
                {
                    _statusFeedbackText.text = "correct";
                    _statusFeedbackText.color = Color.green;
                }
                OnPuzzleSolved?.Invoke();
                _onPuzzleSolvedEvent?.Invoke();
                Invoke(nameof(ClosePuzzleUI), 0.8f);
            }
            else
            {
                if (_statusFeedbackText != null)
                {
                    _statusFeedbackText.text = "wrong";
                    _statusFeedbackText.color = Color.red;
                }
                ResetPuzzleState();
            }
        }

        public void OnClearClicked()
        {
            ResetPuzzleState();
        }

        private bool ValidatePins()
        {
            if (_selectedPins.Count != _correctPinIndices.Count) return false;

            foreach (int rawVal in _correctPinIndices)
            {
                // Support both 1-based (1..9 -> 0..8) and 0-based (0..8)
                int index = (rawVal >= 1 && rawVal <= 9) ? rawVal - 1 : rawVal;
                if (!_selectedPins.Contains(index)) return false;
            }
            return true;
        }

        private void UpdateSequenceDisplay()
        {
            foreach (var icon in _spawnedSequenceIcons)
            {
                if (icon != null) Destroy(icon);
            }
            _spawnedSequenceIcons.Clear();

            if (_selectedSequenceContainer == null) return;

            foreach (int index in _selectedPins)
            {
                GameObject iconGo = new GameObject("SelectedSymbol_" + index, typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(_selectedSequenceContainer, false);

                RectTransform r = iconGo.GetComponent<RectTransform>();
                r.sizeDelta = new Vector2(40f, 40f);

                Image img = iconGo.GetComponent<Image>();
                if (index >= 0 && index < _pinSprites.Count && _pinSprites[index] != null)
                {
                    img.sprite = _pinSprites[index];
                    img.color = Color.white;
                }
                else
                {
                    img.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
                }
                _spawnedSequenceIcons.Add(iconGo);
            }
        }

        private void LoadFont()
        {
            _spinnenkopFont = Resources.Load<Font>("Spinnenkop DEMO");
            if (_spinnenkopFont == null)
            {
                _spinnenkopFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private void ApplyFont(Text textComponent)
        {
            if (textComponent != null && _spinnenkopFont != null)
            {
                textComponent.font = _spinnenkopFont;
            }
        }

        private void EnsureUIBuilt()
        {
            if (_puzzleCanvas == null)
            {
                _puzzleCanvas = GetComponent<Canvas>();
            }

            if (_puzzleCanvas == null)
            {
                _puzzleCanvas = gameObject.AddComponent<Canvas>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            _puzzleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _puzzleCanvas.overrideSorting = true;
            _puzzleCanvas.sortingOrder = _sortingOrder;

            if (_overlayContainer != null) return;

            _overlayContainer = new GameObject("PuzzleOverlayPanel_B", typeof(RectTransform));
            _overlayContainer.transform.SetParent(_puzzleCanvas.transform, false);

            RectTransform overlayRect = _overlayContainer.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image bgImage = _overlayContainer.AddComponent<Image>();
            if (_customBackgroundImage != null)
            {
                bgImage.sprite = _customBackgroundImage;
                bgImage.color = Color.white;
            }
            else
            {
                bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            }

            BuildLeftCenterArea(_overlayContainer.transform);
            BuildRightPinPanel(_overlayContainer.transform);
        }

        private void BuildLeftCenterArea(Transform parent)
        {
            GameObject leftContainer = new GameObject("LeftCenterArea", typeof(RectTransform));
            leftContainer.transform.SetParent(parent, false);

            RectTransform rect = leftContainer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0.68f, 1f);
            rect.offsetMin = new Vector2(30f, 30f);
            rect.offsetMax = new Vector2(-15f, -30f);

            _itemDataList.Clear();

            // Fallback default list if empty
            if (_overlayImages == null || _overlayImages.Count == 0)
            {
                _overlayImages = new List<DraggableImageConfig>
                {
                    new DraggableImageConfig { CanMove = true, CanRotate = true, SpawnPosition = new Vector2(-60f, 20f), SizeDelta = new Vector2(300f, 300f) },
                    new DraggableImageConfig { CanMove = true, CanRotate = true, SpawnPosition = new Vector2(60f, -30f), SizeDelta = new Vector2(300f, 300f) }
                };
            }

            for (int i = 0; i < _overlayImages.Count; i++)
            {
                DraggableImageConfig config = _overlayImages[i];
                if (config == null) continue;

                Texture2D tex = CreatePlaceholderTexture(i % 2 == 0 ? Color.white : Color.black, i % 2 == 0 ? Color.black : Color.white);
                Sprite sprite = config.Sprite != null ? config.Sprite : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

                GameObject imgGo = new GameObject("DraggableImage_" + (i + 1), typeof(RectTransform), typeof(Image), typeof(DraggableUIItem));
                imgGo.transform.SetParent(leftContainer.transform, false);

                RectTransform imgRect = imgGo.GetComponent<RectTransform>();
                
                // Automatic non-overlapping spawn offset if SpawnPosition is zero
                Vector2 spawnPos = config.SpawnPosition;
                if (spawnPos == Vector2.zero && _overlayImages.Count > 1)
                {
                    spawnPos = new Vector2(-60f + (i * 50f), 20f - (i * 40f));
                }

                Vector2 sizeDelta = config.SizeDelta == Vector2.zero ? new Vector2(300f, 300f) : config.SizeDelta;

                imgRect.anchoredPosition = spawnPos;
                imgRect.sizeDelta = sizeDelta;
                imgRect.localRotation = Quaternion.Euler(0f, 0f, config.SpawnRotationAngle);

                Image img = imgGo.GetComponent<Image>();
                img.sprite = sprite;
                img.color = new Color(1f, 1f, 1f, 0.85f);

                DraggableUIItem drag = imgGo.GetComponent<DraggableUIItem>();
                drag.CanMove = config.CanMove;
                drag.CanRotate = config.CanRotate;
                drag.ClampToParent = false;

                _itemDataList.Add(new ListDraggableItemData
                {
                    Item = drag,
                    InitialPos = spawnPos,
                    InitialRot = config.SpawnRotationAngle
                });
            }
        }

        private void BuildRightPinPanel(Transform parent)
        {
            GameObject rightPanel = new GameObject("RightPinPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(parent, false);

            RectTransform panelRect = rightPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.68f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = new Vector2(15f, 30f);
            panelRect.offsetMax = new Vector2(-30f, -30f);

            Image panelBg = rightPanel.AddComponent<Image>();
            panelBg.color = new Color(0.04f, 0.04f, 0.04f, 0.95f);

            GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(rightPanel.transform, false);
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.85f);
            titleRect.anchorMax = new Vector2(1f, 0.98f);

            Text titleText = titleGo.GetComponent<Text>();
            titleText.text = "MULTI IMAGE PUZZLE (B)";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontSize = 26;
            titleText.color = Color.white;
            ApplyFont(titleText);

            GameObject seqGo = new GameObject("SelectedSequenceBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            seqGo.transform.SetParent(rightPanel.transform, false);
            RectTransform seqRect = seqGo.GetComponent<RectTransform>();
            seqRect.anchorMin = new Vector2(0.05f, 0.72f);
            seqRect.anchorMax = new Vector2(0.95f, 0.84f);
            seqRect.offsetMin = Vector2.zero;
            seqRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = seqGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            _selectedSequenceContainer = seqGo.transform;

            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(rightPanel.transform, false);
            RectTransform statusRect = statusGo.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.63f);
            statusRect.anchorMax = new Vector2(0.95f, 0.71f);

            _statusFeedbackText = statusGo.GetComponent<Text>();
            _statusFeedbackText.text = "";
            _statusFeedbackText.alignment = TextAnchor.MiddleCenter;
            _statusFeedbackText.fontSize = 28;
            _statusFeedbackText.color = Color.red;
            ApplyFont(_statusFeedbackText);

            float startX = -90f;
            float startY = 50f;
            float spacingX = 90f;
            float spacingY = -70f;

            _pinButtonObjects.Clear();

            for (int i = 0; i < 9; i++)
            {
                int pinIndex = i;
                int row = i / 3;
                int col = i % 3;

                GameObject pinBtnGo = new GameObject("PinButton_" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button));
                pinBtnGo.transform.SetParent(rightPanel.transform, false);

                RectTransform btnRect = pinBtnGo.GetComponent<RectTransform>();
                btnRect.anchoredPosition = new Vector2(startX + col * spacingX, startY + row * spacingY);
                btnRect.sizeDelta = new Vector2(70f, 60f);

                Image btnImage = pinBtnGo.GetComponent<Image>();
                btnImage.color = Color.white;

                if (i < _pinSprites.Count && _pinSprites[i] != null)
                {
                    btnImage.sprite = _pinSprites[i];
                }
                else
                {
                    btnImage.color = new Color(0.2f, 0.2f, 0.22f);
                    GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    txtGo.transform.SetParent(pinBtnGo.transform, false);
                    RectTransform txtRect = txtGo.GetComponent<RectTransform>();
                    txtRect.anchorMin = Vector2.zero;
                    txtRect.anchorMax = Vector2.one;
                    Text t = txtGo.GetComponent<Text>();
                    t.text = (i + 1).ToString();
                    t.alignment = TextAnchor.MiddleCenter;
                    t.fontSize = 22;
                    t.color = Color.white;
                    ApplyFont(t);
                }

                Button btn = pinBtnGo.GetComponent<Button>();
                ColorBlock colors = btn.colors;
                colors.highlightedColor = new Color(0.8f, 0.8f, 0.9f);
                colors.pressedColor = new Color(0.5f, 0.8f, 1f);
                btn.colors = colors;

                btn.onClick.AddListener(() => OnPinClicked(pinIndex));

                _pinButtonObjects.Add(pinIndex, pinBtnGo);
            }

            GameObject submitGo = new GameObject("SubmitButton", typeof(RectTransform), typeof(Image), typeof(Button));
            submitGo.transform.SetParent(rightPanel.transform, false);
            RectTransform subRect = submitGo.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(50f, -170f);
            subRect.sizeDelta = new Vector2(130f, 50f);

            Image subImg = submitGo.GetComponent<Image>();
            subImg.color = new Color(0.15f, 0.45f, 0.2f);
            Button subBtn = submitGo.GetComponent<Button>();
            subBtn.onClick.AddListener(OnSubmitClicked);

            GameObject subTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            subTxtGo.transform.SetParent(submitGo.transform, false);
            RectTransform subTxtRect = subTxtGo.GetComponent<RectTransform>();
            subTxtRect.anchorMin = Vector2.zero;
            subTxtRect.anchorMax = Vector2.one;

            Text subText = subTxtGo.GetComponent<Text>();
            subText.text = "SUBMIT";
            subText.alignment = TextAnchor.MiddleCenter;
            subText.fontSize = 22;
            subText.color = Color.white;
            ApplyFont(subText);

            GameObject clearGo = new GameObject("ClearButton", typeof(RectTransform), typeof(Image), typeof(Button));
            clearGo.transform.SetParent(rightPanel.transform, false);
            RectTransform clrRect = clearGo.GetComponent<RectTransform>();
            clrRect.anchoredPosition = new Vector2(-90f, -170f);
            clrRect.sizeDelta = new Vector2(110f, 50f);

            Image clrImg = clearGo.GetComponent<Image>();
            clrImg.color = new Color(0.45f, 0.15f, 0.15f);
            Button clrBtn = clearGo.GetComponent<Button>();
            clrBtn.onClick.AddListener(OnClearClicked);

            GameObject clrTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            clrTxtGo.transform.SetParent(clearGo.transform, false);
            RectTransform clrTxtRect = clrTxtGo.GetComponent<RectTransform>();
            clrTxtRect.anchorMin = Vector2.zero;
            clrTxtRect.anchorMax = Vector2.one;

            Text clrText = clrTxtGo.GetComponent<Text>();
            clrText.text = "CLEAR";
            clrText.alignment = TextAnchor.MiddleCenter;
            clrText.fontSize = 22;
            clrText.color = Color.white;
            ApplyFont(clrText);
        }

        private Texture2D CreatePlaceholderTexture(Color colorA, Color colorB)
        {
            Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    bool isBorder = (x < 6 || x > 121 || y < 6 || y > 121);
                    tex.SetPixel(x, y, isBorder ? colorB : colorA);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
