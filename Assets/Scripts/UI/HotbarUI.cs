using UnityEngine;
using UnityEngine.UI;
using Nemuri.Inventory;

namespace Nemuri.UI
{
    public class HotbarUI : MonoBehaviour
    {
        [Header("UI Aesthetics")]
        [SerializeField] private Color _slotNormalColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color _slotSelectedColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
        [SerializeField] private Color _highlightBorderColor = new Color(0.95f, 0.8f, 0.3f, 1f);

        private Canvas _canvas;
        private RectTransform _containerRect;
        private Image[] _slotImages;
        private Image[] _slotIconImages;
        private Image _highlightBorder;
        private Text _itemNameLabel;

        // static reference so external scripts (e.g. HippocampusIntroController) can
        // show/hide the hotbar canvas without relying on GameObject.Find
        public static GameObject CanvasObject { get; private set; }

        private void Start()
        {
            // automatically ensure the inspect script is attached
            if (GetComponent<ItemInspector>() == null)
            {
                gameObject.AddComponent<ItemInspector>();
            }
            // automatically ensure the vision manager is attached
            if (GetComponent<VisionManager>() == null)
            {
                gameObject.AddComponent<VisionManager>();
            }
            // automatically ensure the vision puzzle manager is attached
            if (GetComponent<VisionPuzzleManager>() == null)
            {
                gameObject.AddComponent<VisionPuzzleManager>();
            }

            InitializeUI();

            if (HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.OnSlotSelected += UpdateSelection;
                HotbarInventory.Instance.OnInventoryUpdated += RefreshInventory;
                UpdateSelection(HotbarInventory.Instance.SelectedIndex);
                RefreshInventory();
            }
        }

        private void OnDestroy()
        {
            if (HotbarInventory.Instance != null)
            {
                HotbarInventory.Instance.OnSlotSelected -= UpdateSelection;
                HotbarInventory.Instance.OnInventoryUpdated -= RefreshInventory;
            }
        }

        private void InitializeUI()
        {
            // find or create canvas
            GameObject canvasObj = GameObject.Find("Hotbar Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Hotbar Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                _canvas = canvasObj.GetComponent<Canvas>();
            }

            // hidden until first item is picked up
            canvasObj.SetActive(false);

            // expose the canvas reference statically so external scripts don't rely on GameObject.Find
            CanvasObject = canvasObj;

            // create item label text above the hotbar
            GameObject labelObj = new GameObject("SelectedItemLabel");
            labelObj.transform.SetParent(canvasObj.transform, false);
            _itemNameLabel = labelObj.AddComponent<Text>();
            _itemNameLabel.alignment = TextAnchor.MiddleCenter;
            _itemNameLabel.color = Color.white;
            _itemNameLabel.fontSize = 24;
            _itemNameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_itemNameLabel.font == null)
            {
                _itemNameLabel.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.13f);
            labelRect.anchorMax = new Vector2(0.5f, 0.13f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(600f, 40f);

            // create hotbar container panel
            GameObject containerObj = new GameObject("HotbarContainer");
            containerObj.transform.SetParent(canvasObj.transform, false);
            Image containerBg = containerObj.AddComponent<Image>();
            containerBg.color = new Color(0.05f, 0.05f, 0.05f, 0.6f);

            _containerRect = containerObj.GetComponent<RectTransform>();
            _containerRect.anchorMin = new Vector2(0.5f, 0.05f);
            _containerRect.anchorMax = new Vector2(0.5f, 0.05f);
            _containerRect.pivot = new Vector2(0.5f, 0.5f);
            _containerRect.sizeDelta = new Vector2(612f, 76f);

            // layout group for slots
            HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // initialize slot arrays
            int totalSlots = HotbarInventory.Instance != null ? HotbarInventory.Instance.SlotCount : 9;
            _slotImages = new Image[totalSlots];
            _slotIconImages = new Image[totalSlots];

            for (int i = 0; i < totalSlots; i++)
            {
                // create individual slot panel
                GameObject slotObj = new GameObject("Slot_" + i);
                slotObj.transform.SetParent(containerObj.transform, false);
                _slotImages[i] = slotObj.AddComponent<Image>();
                _slotImages[i].color = _slotNormalColor;

                RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(60f, 60f);

                // create icon image inside slot
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slotObj.transform, false);
                _slotIconImages[i] = iconObj.AddComponent<Image>();
                _slotIconImages[i].color = Color.clear; // hidden by default

                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(6f, 6f);
                iconRect.offsetMax = new Vector2(-6f, -6f);

                var dragHandler = slotObj.AddComponent<HotbarSlotDragHandler>();
                dragHandler.Initialize(i);
            }

            // create selection highlight border overlay
            GameObject highlightObj = new GameObject("SelectionHighlight");
            highlightObj.transform.SetParent(containerObj.transform, false);
            _highlightBorder = highlightObj.AddComponent<Image>();
            _highlightBorder.color = Color.clear; // transparent center

            // ignore layout group positioning
            LayoutElement layoutElement = highlightObj.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            RectTransform highlightRect = highlightObj.GetComponent<RectTransform>();
            highlightRect.sizeDelta = new Vector2(60f, 60f); // match slot size

            // create hollow border lines
            float borderWidth = 3f;
            CreateBorderLine(highlightObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -borderWidth), _highlightBorderColor); // top
            CreateBorderLine(highlightObj.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, borderWidth), _highlightBorderColor); // bottom
            CreateBorderLine(highlightObj.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(borderWidth, 0f), _highlightBorderColor); // left
            CreateBorderLine(highlightObj.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-borderWidth, 0f), _highlightBorderColor); // right
        }

        private void CreateBorderLine(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Color color)
        {
            GameObject line = new GameObject("BorderLine");
            line.transform.SetParent(parent, false);
            Image img = line.AddComponent<Image>();
            img.color = color;
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;

            if (anchorMin.x == anchorMax.x) // vertical line
            {
                rt.offsetMin = new Vector2(Mathf.Min(0f, offset.x), 0f);
                rt.offsetMax = new Vector2(Mathf.Max(0f, offset.x), 0f);
            }
            else // horizontal line
            {
                rt.offsetMin = new Vector2(0f, Mathf.Min(0f, offset.y));
                rt.offsetMax = new Vector2(0f, Mathf.Max(0f, offset.y));
            }
        }

        private void UpdateSelection(int selectedIndex)
        {
            if (_slotImages == null || selectedIndex < 0 || selectedIndex >= _slotImages.Length)
            {
                return;
            }

            for (int i = 0; i < _slotImages.Length; i++)
            {
                bool isSelected = (i == selectedIndex);
                _slotImages[i].color = _slotNormalColor;

                if (isSelected && _highlightBorder != null)
                {
                    _highlightBorder.transform.position = _slotImages[i].transform.position;
                    _highlightBorder.transform.SetAsLastSibling();
                }
            }

            // update item label text
            if (HotbarInventory.Instance != null && _itemNameLabel != null)
            {
                var selectedItem = HotbarInventory.Instance.GetSelectedItem();
                if (selectedItem != null)
                {
                    _itemNameLabel.text = selectedItem.displayName;
                }
                else
                {
                    _itemNameLabel.text = "";
                }
            }
        }

        private void LateUpdate()
        {
            if (HotbarInventory.Instance != null && _slotImages != null && _highlightBorder != null)
            {
                int index = HotbarInventory.Instance.SelectedIndex;
                if (index >= 0 && index < _slotImages.Length)
                {
                    _highlightBorder.transform.position = _slotImages[index].transform.position;
                }
            }
        }

        private void RefreshInventory()
        {
            if (HotbarInventory.Instance == null || _slotIconImages == null)
            {
                return;
            }

            bool hasAnyItem = false;
            for (int i = 0; i < _slotIconImages.Length; i++)
            {
                var item = HotbarInventory.Instance.GetItemAt(i);
                if (item != null)
                {
                    hasAnyItem = true;
                    if (item.icon != null)
                    {
                        _slotIconImages[i].sprite = item.icon;
                        _slotIconImages[i].color = Color.white;
                    }
                    else
                    {
                        // fallback display for item without icon
                        _slotIconImages[i].sprite = null;
                        _slotIconImages[i].color = new Color(0.9f, 0.9f, 0.9f, 0.4f);
                    }
                }
                else
                {
                    _slotIconImages[i].sprite = null;
                    _slotIconImages[i].color = Color.clear;
                }
            }

            // reveal the hotbar the moment the first item is collected
            if (hasAnyItem && _canvas != null && !_canvas.gameObject.activeSelf)
            {
                _canvas.gameObject.SetActive(true);
            }

            // refresh selection text label
            UpdateSelection(HotbarInventory.Instance.SelectedIndex);
        }

        public void SetSlotIconVisible(int index, bool visible)
        {
            if (_slotIconImages != null && index >= 0 && index < _slotIconImages.Length)
            {
                if (_slotIconImages[index] != null)
                {
                    var item = HotbarInventory.Instance.GetItemAt(index);
                    if (item != null)
                    {
                        _slotIconImages[index].color = visible ? Color.white : Color.clear;
                    }
                    else
                    {
                        _slotIconImages[index].color = Color.clear;
                    }
                }
            }
        }
    }
}
