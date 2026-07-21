using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nemuri.UI
{
    public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool CanMove = true;
        public bool CanRotate = false;
        public float RotationSpeed = 15f;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Vector2 _originalPosition;
        private Quaternion _originalRotation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (_rectTransform != null)
            {
                _originalRotation = _rectTransform.localRotation;
            }
        }

        private void Update()
        {
            if (CanRotate && _rectTransform != null)
            {
                if (Mouse.current != null)
                {
                    float scrollY = Mouse.current.scroll.ReadValue().y;
                    if (Mathf.Abs(scrollY) > 0.01f)
                    {
                        _rectTransform.Rotate(0f, 0f, scrollY * 0.1f * RotationSpeed);
                    }
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
            if (_rectTransform != null)
            {
                _originalPosition = _rectTransform.anchoredPosition;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || _canvas == null) return;

            if (eventData.button == PointerEventData.InputButton.Left && CanMove)
            {
                Vector2 delta = eventData.delta / _canvas.scaleFactor;
                _rectTransform.anchoredPosition += delta;
            }
            else if (eventData.button == PointerEventData.InputButton.Right && CanRotate)
            {
                float rotationDelta = -eventData.delta.x * 0.5f;
                _rectTransform.Rotate(0f, 0f, rotationDelta);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        public void ResetTransform()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _originalPosition;
                _rectTransform.localRotation = _originalRotation;
            }
        }
    }
}
