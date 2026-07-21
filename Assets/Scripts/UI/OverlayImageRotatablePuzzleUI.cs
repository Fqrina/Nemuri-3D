using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Nemuri.UI
{
    public class OverlayImageRotatablePuzzleUI : MonoBehaviour
    {
        public System.Action OnPuzzleSolved;

        [Header("Canvas Settings")]
        [SerializeField] private int _sortingOrder = 1400;
        [SerializeField] private bool _enableKeyboardToggle = true;

        [Header("Image Movement & Rotation Settings")]
        [SerializeField] private bool _canMoveImage1 = false;
        [SerializeField] private bool _canRotateImage1 = false;
        [SerializeField] private bool _canMoveImage2 = true;
        [SerializeField] private bool _canRotateImage2 = true;

        [Header("Puzzle Configuration (Custom Image Sprites for Slots 1-9)")]
        [Tooltip("Custom Sprites for Keypad Slots 1 to 9.")]
        [SerializeField] private List<Sprite> _pinSprites = new List<Sprite>(9);
        [Tooltip("The set of slot indices (0-indexed, e.g. 0 for Slot 1, 6 for Slot 7) that must be selected.")]
        [SerializeField] private List<int> _correctPinIndices = new List<int> { 0, 6 };
        [SerializeField] private Sprite _image1Sprite;
        [SerializeField] private Sprite _image2Sprite;
        [Tooltip("Custom size (width, height) for Image 1. Defaults to (320, 320) if (0, 0).")]
        [SerializeField] private Vector2 _image1Size = new Vector2(320f, 320f);
        [Tooltip("Custom size (width, height) for Image 2. Defaults to (320, 320) if (0, 0).")]
        [SerializeField] private Vector2 _image2Size = new Vector2(320f, 320f);
        [SerializeField] private UnityEngine.Events.UnityEvent _onPuzzleSolvedEvent;

        [Header("UI References (Optional - Auto-generated if null)")]
        [SerializeField] private Canvas _puzzleCanvas;
        [SerializeField] private GameObject _overlayContainer;
        [SerializeField] private Transform _selectedSequenceContainer;
        [SerializeField] private Text _statusFeedbackText;
        [SerializeField] private RectTransform _draggableImage1Rect;
        [SerializeField] private RectTransform _draggableImage2Rect;

        private readonly HashSet<int> _selectedPins = new HashSet<int>();
        private readonly Dictionary<int, GameObject> _pinButtonObjects = new Dictionary<int, GameObject>();
        private readonly List<GameObject> _spawnedSequenceIcons = new List<GameObject>();
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

            if (_draggableImage1Rect != null)
            {
                DraggableUIItem drag1 = _draggableImage1Rect.GetComponent<DraggableUIItem>();
                if (drag1 != null) drag1.ResetTransform();
            }
            if (_draggableImage2Rect != null)
            {
                DraggableUIItem drag2 = _draggableImage2Rect.GetComponent<DraggableUIItem>();
                if (drag2 != null) drag2.ResetTransform();
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

            _overlayContainer = new GameObject("PuzzleOverlayPanel_F", typeof(RectTransform));
            _overlayContainer.transform.SetParent(_puzzleCanvas.transform, false);

            RectTransform overlayRect = _overlayContainer.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image bgImage = _overlayContainer.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

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

            Texture2D tex1 = CreatePlaceholderTexture(Color.white, Color.black);
            Sprite sprite1 = _image1Sprite != null ? _image1Sprite : Sprite.Create(tex1, new Rect(0, 0, tex1.width, tex1.height), new Vector2(0.5f, 0.5f));

            GameObject img1Go = new GameObject("DraggableImage_Bottom", typeof(RectTransform), typeof(Image), typeof(DraggableUIItem));
            img1Go.transform.SetParent(leftContainer.transform, false);
            _draggableImage1Rect = img1Go.GetComponent<RectTransform>();
            Vector2 size1 = _image1Size == Vector2.zero ? new Vector2(320f, 320f) : _image1Size;
            _draggableImage1Rect.sizeDelta = size1;
            _draggableImage1Rect.anchoredPosition = new Vector2(-80f, 20f);

            Image img1 = img1Go.GetComponent<Image>();
            img1.sprite = sprite1;
            img1.color = new Color(1f, 1f, 1f, 0.85f);

            DraggableUIItem drag1 = img1Go.GetComponent<DraggableUIItem>();
            drag1.CanMove = _canMoveImage1;
            drag1.CanRotate = _canRotateImage1;

            Texture2D tex2 = CreatePlaceholderTexture(Color.black, Color.white);
            Sprite sprite2 = _image2Sprite != null ? _image2Sprite : Sprite.Create(tex2, new Rect(0, 0, tex2.width, tex2.height), new Vector2(0.5f, 0.5f));

            GameObject img2Go = new GameObject("DraggableImage_Top", typeof(RectTransform), typeof(Image), typeof(DraggableUIItem));
            img2Go.transform.SetParent(leftContainer.transform, false);
            _draggableImage2Rect = img2Go.GetComponent<RectTransform>();
            Vector2 size2 = _image2Size == Vector2.zero ? new Vector2(320f, 320f) : _image2Size;
            _draggableImage2Rect.sizeDelta = size2;
            _draggableImage2Rect.anchoredPosition = new Vector2(60f, -30f);

            Image img2 = img2Go.GetComponent<Image>();
            img2.sprite = sprite2;
            img2.color = new Color(1f, 1f, 1f, 0.85f);

            DraggableUIItem drag2 = img2Go.GetComponent<DraggableUIItem>();
            drag2.CanMove = _canMoveImage2;
            drag2.CanRotate = _canRotateImage2;
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
            titleText.text = "IMAGE ROTATABLE (F)";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontSize = 28;
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
