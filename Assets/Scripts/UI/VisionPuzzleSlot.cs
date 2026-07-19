using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Nemuri.Inventory;

namespace Nemuri.UI
{
    public class VisionPuzzleSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField] private int _expectedItemId;
        
        private Image _slotImage;
        private Text _placeholderText;
        private HotbarItem _placedItem;
        private int _originalHotbarIndex = -1;

        public int ExpectedItemId => _expectedItemId;
        public HotbarItem PlacedItem => _placedItem;
        public int OriginalHotbarIndex => _originalHotbarIndex;

        private void Awake()
        {
            _slotImage = GetComponent<Image>();
            _placeholderText = GetComponentInChildren<Text>();
        }

        public void Setup(int expectedItemId)
        {
            _expectedItemId = expectedItemId;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (HotbarSlotDragHandler.DraggedSlot == null) return;

            HotbarSlotDragHandler dragHandler = HotbarSlotDragHandler.DraggedSlot;
            HotbarItem item = dragHandler.Item;

            if (item == null) return;

            // if slot is already occupied, return that item back to its hotbar slot
            if (_placedItem != null)
            {
                ReturnItemToHotbar();
            }

            // place new item
            _placedItem = item;
            _originalHotbarIndex = dragHandler.SlotIndex;

            if (_slotImage != null)
            {
                _slotImage.sprite = item.icon;
                _slotImage.color = Color.white;
            }

            if (_placeholderText != null)
            {
                _placeholderText.text = "";
            }

            // let the drag handler know it was successfully placed
            dragHandler.OnPlacedInPuzzle();
        }

        public void Clear()
        {
            _placedItem = null;
            _originalHotbarIndex = -1;

            if (_slotImage != null)
            {
                _slotImage.sprite = null;
                _slotImage.color = Color.clear;
            }

            if (_placeholderText != null)
            {
                _placeholderText.text = "";
            }
        }

        public void ReturnItemToHotbar()
        {
            if (_placedItem == null || _originalHotbarIndex == -1) return;

            // notify UI to restore hotbar icon visibility
            if (VisionPuzzleUI.Instance != null)
            {
                VisionPuzzleUI.Instance.RestoreHotbarSlotIcon(_originalHotbarIndex);
            }

            Clear();
        }
    }
}
