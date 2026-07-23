using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Nemuri.UI
{
    public class OverlayRotatablePuzzleUI : MonoBehaviour
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

        [Header("Puzzle Configuration")]
        [Tooltip("Custom 9 labels for the keypad pins (e.g. A, B, C or ?, !, X).")]
        [SerializeField] private List<string> _pinLabels = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I" };
        [Tooltip("The set of pin labels that must be selected (order does not matter).")]
        [SerializeField] private List<string> _correctPins = new List<string> { "A", "G" };
        [SerializeField] private Sprite _image1Sprite;
        [SerializeField] private Sprite _image2Sprite;
        [SerializeField] private UnityEngine.Events.UnityEvent _onPuzzleSolvedEvent;

        [Header("UI References (Optional - Auto-generated if null)")]
        [SerializeField] private Canvas _puzzleCanvas;
        [SerializeField] private GameObject _overlayContainer;
        [SerializeField] private Text _sequenceDisplayText;
        [SerializeField] private Text _statusFeedbackText;
        [SerializeField] private RectTransform _draggableImage1Rect;
        [SerializeField] private RectTransform _draggableImage2Rect;

        private readonly HashSet<string> _selectedPins = new HashSet<string>();
        private readonly Dictionary<string, GameObject> _pinButtonObjects = new Dictionary<string, GameObject>();
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
            if (_enableKeyboardToggle && Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
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

        public void OnPinClicked(string pinId)
        {
            if (_statusFeedbackText != null && _statusFeedbackText.text == "wrong")
            {
                _statusFeedbackText.text = "";
            }

            _selectedPins.Add(pinId);
            UpdateSequenceDisplay();

            if (_pinButtonObjects.TryGetValue(pinId, out GameObject btnObj) && btnObj != null)
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
                if (_statusFeedbackText != null)
                {
                    _statusFeedbackText.text = "wrong";
                }
            }
        }

        public void OnClearClicked()
        {
            ResetPuzzleState();
        }

        private bool ValidatePins()
        {
            if (_selectedPins.Count != _correctPins.Count) return false;

            foreach (string pin in _correctPins)
            {
                if (!_selectedPins.Contains(pin)) return false;
            }
            return true;
        }

        private void UpdateSequenceDisplay()
        {
            if (_sequenceDisplayText == null) return;

            if (_selectedPins.Count == 0)
            {
                _sequenceDisplayText.text = "PRESSED: -";
            }
            else
            {
                _sequenceDisplayText.text = "PRESSED: " + string.Join(" ", _selectedPins);
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

            _overlayContainer = new GameObject("PuzzleOverlayPanel_G", typeof(RectTransform));
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
            _draggableImage1Rect.sizeDelta = new Vector2(320f, 320f);
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
            _draggableImage2Rect.sizeDelta = new Vector2(320f, 320f);
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
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Text titleText = titleGo.GetComponent<Text>();
            titleText.text = "PIN LOCK (G)";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontSize = 32;
            titleText.color = Color.white;
            ApplyFont(titleText);

            GameObject seqGo = new GameObject("SequenceText", typeof(RectTransform), typeof(Text));
            seqGo.transform.SetParent(rightPanel.transform, false);
            RectTransform seqRect = seqGo.GetComponent<RectTransform>();
            seqRect.anchorMin = new Vector2(0.05f, 0.72f);
            seqRect.anchorMax = new Vector2(0.95f, 0.84f);
            seqRect.offsetMin = Vector2.zero;
            seqRect.offsetMax = Vector2.zero;

            _sequenceDisplayText = seqGo.GetComponent<Text>();
            _sequenceDisplayText.text = "PRESSED: -";
            _sequenceDisplayText.alignment = TextAnchor.MiddleCenter;
            _sequenceDisplayText.fontSize = 24;
            _sequenceDisplayText.color = new Color(0.9f, 0.9f, 0.9f);
            ApplyFont(_sequenceDisplayText);

            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(rightPanel.transform, false);
            RectTransform statusRect = statusGo.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.63f);
            statusRect.anchorMax = new Vector2(0.95f, 0.71f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            _statusFeedbackText = statusGo.GetComponent<Text>();
            _statusFeedbackText.text = "";
            _statusFeedbackText.alignment = TextAnchor.MiddleCenter;
            _statusFeedbackText.fontSize = 28;
            _statusFeedbackText.color = Color.red;
            ApplyFont(_statusFeedbackText);

            List<string> labelsToUse = (_pinLabels != null && _pinLabels.Count >= 9)
                ? _pinLabels
                : new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I" };

            float startX = -90f;
            float startY = 50f;
            float spacingX = 90f;
            float spacingY = -70f;

            _pinButtonObjects.Clear();

            for (int i = 0; i < 9 && i < labelsToUse.Count; i++)
            {
                string label = labelsToUse[i];
                int row = i / 3;
                int col = i % 3;

                GameObject pinBtnGo = new GameObject("PinButton_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
                pinBtnGo.transform.SetParent(rightPanel.transform, false);

                RectTransform btnRect = pinBtnGo.GetComponent<RectTransform>();
                btnRect.anchoredPosition = new Vector2(startX + col * spacingX, startY + row * spacingY);
                btnRect.sizeDelta = new Vector2(70f, 60f);

                Image btnImage = pinBtnGo.GetComponent<Image>();
                btnImage.color = new Color(0.2f, 0.2f, 0.22f);

                Button btn = pinBtnGo.GetComponent<Button>();
                ColorBlock colors = btn.colors;
                colors.highlightedColor = new Color(0.35f, 0.35f, 0.4f);
                colors.pressedColor = new Color(0.15f, 0.5f, 0.8f);
                btn.colors = colors;

                btn.onClick.AddListener(() => OnPinClicked(label));

                _pinButtonObjects.Add(label, pinBtnGo);

                GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(pinBtnGo.transform, false);
                RectTransform txtRect = txtGo.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;

                Text btnText = txtGo.GetComponent<Text>();
                btnText.text = label;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.fontSize = 24;
                btnText.color = Color.white;
                ApplyFont(btnText);
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
            subTxtRect.offsetMin = Vector2.zero;
            subTxtRect.offsetMax = Vector2.zero;

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
            clrTxtRect.offsetMin = Vector2.zero;
            clrTxtRect.offsetMax = Vector2.zero;

            Text clrText = clrTxtGo.GetComponent<Text>();
            clrText.text = "CLEAR";
            clrText.alignment = TextAnchor.MiddleCenter;
            clrText.fontSize = 22;
            clrText.color = Color.white;
            ApplyFont(clrText);
        }

        private Texture2D CreatePlaceholderTexture(Color colorA, Color colorB)
        {
            Texture2D tex = new Texture2D(128, 128);
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    bool isEdge = x < 6 || x > 121 || y < 6 || y > 121;
                    bool isCross = Mathf.Abs(x - y) < 4 || Mathf.Abs((128 - x) - y) < 4;
                    if (isEdge || isCross)
                    {
                        tex.SetPixel(x, y, colorB);
                    }
                    else
                    {
                        tex.SetPixel(x, y, colorA);
                    }
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
