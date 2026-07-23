using UnityEngine;
using UnityEngine.EventSystems;

namespace Nemuri.UI
{
    public class DroppedPuzzleItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private int _itemId;
        private RectTransform _rectTransform;
        private RectTransform _boardRect;
        private Canvas _parentCanvas;

        public void Initialize(int itemId, RectTransform boardRect)
        {
            _itemId = itemId;
            _boardRect = boardRect;
            _rectTransform = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("[DroppedPuzzleItemDragHandler] Resuming drag for item ID: " + _itemId);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || _parentCanvas == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boardRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            _rectTransform.anchoredPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_rectTransform == null) return;
            Vector2 finalPos = _rectTransform.anchoredPosition;

            Debug.Log(string.Format("[DroppedPuzzleItemDragHandler] Dropped item {0} at new board position: {1}", _itemId, finalPos));

            if (VisionPuzzleUI.Instance != null)
            {
                VisionPuzzleUI.Instance.UpdatePlacedItemPosition(_itemId, finalPos);
            }
        }
    }
}
