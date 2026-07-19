using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Nemuri.Inventory;

namespace Nemuri.UI
{
    public class HotbarSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static HotbarSlotDragHandler DraggedSlot { get; private set; }

        private int _slotIndex;
        private GameObject _dragVisual;
        private bool _isDragging = false;

        public int SlotIndex => _slotIndex;
        public HotbarItem Item => HotbarInventory.Instance != null ? HotbarInventory.Instance.GetItemAt(_slotIndex) : null;

        public void Initialize(int index)
        {
            _slotIndex = index;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log(string.Format("[HotbarSlotDragHandler] OnBeginDrag detected on slot: {0}. Puzzle active: {1}. Item: {2}", 
                _slotIndex, 
                VisionPuzzleManager.Instance != null && VisionPuzzleManager.Instance.IsPuzzleActive, 
                Item != null ? Item.displayName : "null"));

            if (VisionPuzzleManager.Instance == null || !VisionPuzzleManager.Instance.IsPuzzleActive) return;
            if (Item == null) return;

            DraggedSlot = this;
            _isDragging = true;

            // spawn visual duplicate
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                _dragVisual = new GameObject("DragVisual");
                _dragVisual.transform.SetParent(parentCanvas.transform, false);
                
                Image img = _dragVisual.AddComponent<Image>();
                img.sprite = Item.icon;
                img.raycastTarget = false; // ensure drop target receives drop event

                RectTransform rt = _dragVisual.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100f, 100f);

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint
                );
                rt.anchoredPosition = localPoint;
            }

            // disable original icon visually
            if (VisionPuzzleUI.Instance != null)
            {
                VisionPuzzleUI.Instance.HideHotbarSlotIcon(_slotIndex);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _dragVisual == null) return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                RectTransform rt = _dragVisual.GetComponent<RectTransform>();
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint
                );
                rt.anchoredPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("[HotbarSlotDragHandler] OnEndDrag detected.");
            if (!_isDragging) return;

            _isDragging = false;
            DraggedSlot = null;

            if (_dragVisual != null)
            {
                Destroy(_dragVisual);
                _dragVisual = null;
            }

            // check if drop target handled placement
            bool isPlaced = false;
            if (VisionPuzzleUI.Instance != null)
            {
                isPlaced = VisionPuzzleUI.Instance.IsItemPlacedInPuzzle(_slotIndex);
            }

            if (!isPlaced && VisionPuzzleUI.Instance != null)
            {
                VisionPuzzleUI.Instance.RestoreHotbarSlotIcon(_slotIndex);
            }
        }

        public void OnPlacedInPuzzle()
        {
            // Placement visual copy is created on the board
        }
    }
}
