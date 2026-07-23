using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nemuri.UI
{
    public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool CanMove = true;
        public bool CanRotate = false;
        public float RotationSpeed = 15f;
        public bool ClampToParent = false;
        public float AlphaHitThreshold = 0.1f;

        private RectTransform _rectTransform;
        private RectTransform _parentContainerRect;
        private Canvas _canvas;
        private Image _image;
        private Vector2 _originalPosition;
        private Quaternion _originalRotation;
        private bool _isOriginalPositionSet = false;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _image = GetComponent<Image>();

            if (_rectTransform != null)
            {
                _originalRotation = _rectTransform.localRotation;
                if (!_isOriginalPositionSet)
                {
                    _originalPosition = _rectTransform.anchoredPosition;
                    _isOriginalPositionSet = true;
                }
            }

            ApplyAlphaThreshold();
            CacheParentContainer();
        }

        private void Start()
        {
            ApplyAlphaThreshold();
        }

        private void ApplyAlphaThreshold()
        {
            if (_image != null)
            {
                try
                {
                    _image.alphaHitTestMinimumThreshold = AlphaHitThreshold;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DraggableUIItem] Could not set alphaHitTestMinimumThreshold on {_image.name}: {ex.Message}. Make sure 'Read/Write Enabled' is checked in Texture Import Settings.");
                }
            }
        }

        private void CacheParentContainer()
        {
            if (_parentContainerRect == null && transform.parent != null)
            {
                _parentContainerRect = transform.parent.GetComponent<RectTransform>();
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
            CacheParentContainer();
            if (_rectTransform != null && !_isOriginalPositionSet)
            {
                _originalPosition = _rectTransform.anchoredPosition;
                _isOriginalPositionSet = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || _canvas == null) return;

            if (eventData.button == PointerEventData.InputButton.Left && CanMove)
            {
                Vector2 delta = eventData.delta / _canvas.scaleFactor;
                _rectTransform.anchoredPosition += delta;

                if (ClampToParent)
                {
                    ClampPositionToContainer();
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right && CanRotate)
            {
                float rotationDelta = -eventData.delta.x * 0.5f;
                _rectTransform.Rotate(0f, 0f, rotationDelta);
            }
        }

        private void ClampPositionToContainer()
        {
            CacheParentContainer();
            if (_parentContainerRect == null || _rectTransform == null) return;

            Vector2 parentSize = _parentContainerRect.rect.size;
            float halfParentW = parentSize.x * 0.5f;
            float halfParentH = parentSize.y * 0.5f;

            // Clamp the center/pivot of the item within parent bounds
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, -halfParentW, halfParentW);
            pos.y = Mathf.Clamp(pos.y, -halfParentH, halfParentH);
            _rectTransform.anchoredPosition = pos;
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
