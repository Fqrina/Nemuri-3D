using UnityEngine;
using System.Collections;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(Interactable))]
    public class Door : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] private bool _disableCollisionWhenOpen = true;
        [SerializeField] private bool _createClosedDoorBlocker = true;
        [SerializeField, Min(0.05f)] private float _minimumBlockerThickness = 0.45f;

        [Header("Door Rotation")]
        [SerializeField] private Transform _doorPivot;
        [SerializeField] private float _openAngle = 90f;
        [SerializeField] private float _rotationSpeed = 3f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _openSoundClip;
        [SerializeField] private AudioClip _closeSoundClip;
        [SerializeField, Min(0f)] private float _volume = 1f;

        private AudioSource _audioSource;
        private Interactable _interactable;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _isAnimating;
        private bool _isOpen;

        private Collider[] _doorColliders;
        private BoxCollider _generatedBlocker;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            
            if (_doorPivot == null)
            {
                _doorPivot = transform;
            }

            _closedRotation = _doorPivot.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, _openAngle, 0f);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D sound
        }

        private void Start()
        {
            // Set up interaction bounds and collision
            SetupDoorCollision();
            
            // Subscribe to the interaction event
            _interactable.OnInteract.AddListener(ToggleDoor);

            // Set the initial prompt text
            UpdatePromptText();
        }

        private void SetupDoorCollision()
        {
            _doorColliders = GetComponentsInChildren<Collider>();

            if (_createClosedDoorBlocker)
            {
                EnsureClosedDoorBlocker();
                _doorColliders = GetComponentsInChildren<Collider>();
            }

            ApplyDoorCollision();
        }

        private void EnsureClosedDoorBlocker()
        {
            Bounds bounds = CalculateRenderBounds();
            if (bounds.size == Vector3.zero)
            {
                return;
            }

            _generatedBlocker = gameObject.GetComponent<BoxCollider>();
            if (_generatedBlocker == null)
            {
                _generatedBlocker = gameObject.AddComponent<BoxCollider>();
            }

            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 lossyScale = transform.lossyScale;
            Vector3 localSize = new Vector3(
                SafeDivide(bounds.size.x, Mathf.Abs(lossyScale.x)),
                SafeDivide(bounds.size.y, Mathf.Abs(lossyScale.y)),
                SafeDivide(bounds.size.z, Mathf.Abs(lossyScale.z)));

            int thinnestAxis = GetThinnestAxis(localSize);
            if (thinnestAxis == 0)
            {
                localSize.x = Mathf.Max(localSize.x, SafeDivide(_minimumBlockerThickness, Mathf.Abs(lossyScale.x)));
            }
            else if (thinnestAxis == 1)
            {
                localSize.y = Mathf.Max(localSize.y, SafeDivide(_minimumBlockerThickness, Mathf.Abs(lossyScale.y)));
            }
            else
            {
                localSize.z = Mathf.Max(localSize.z, SafeDivide(_minimumBlockerThickness, Mathf.Abs(lossyScale.z)));
            }

            _generatedBlocker.center = localCenter;
            _generatedBlocker.size = localSize;
            _generatedBlocker.isTrigger = false;
        }

        private Bounds CalculateRenderBounds()
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (!r.enabled) continue;
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds) return bounds;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                if (!c.enabled) continue;
                if (!hasBounds)
                {
                    bounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(c.bounds);
                }
            }

            return bounds;
        }

        private void ApplyDoorCollision()
        {
            if (!_disableCollisionWhenOpen || _doorColliders == null)
            {
                return;
            }

            foreach (Collider doorCollider in _doorColliders)
            {
                if (doorCollider != null && !doorCollider.isTrigger)
                {
                    doorCollider.enabled = !_isOpen;
                }
            }
        }

        private void ToggleDoor()
        {
            if (_isAnimating)
                return;

            _isOpen = !_isOpen;

            ApplyDoorCollision();

            StopAllCoroutines();
            StartCoroutine(RotateDoor());
            
            UpdatePromptText();

            PlayDoorSound();
        }

        private void PlayDoorSound()
        {
            AudioClip clip = _isOpen ? _openSoundClip : _closeSoundClip;
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, _volume);
            }
        }

        private void UpdatePromptText()
        {
            string actionText = _isOpen ? "close" : "open";
            _interactable.PromptText = $"Hold E to {actionText}";
        }

        private IEnumerator RotateDoor()
        {
            _isAnimating = true;

            Quaternion targetRotation = _isOpen
                ? _openRotation
                : _closedRotation;

            while (Quaternion.Angle(_doorPivot.localRotation, targetRotation) > 0.1f)
            {
                _doorPivot.localRotation = Quaternion.RotateTowards(
                    _doorPivot.localRotation,
                    targetRotation,
                    _rotationSpeed * 180f * Time.deltaTime);

                yield return null;
            }

            _doorPivot.localRotation = targetRotation;

            _isAnimating = false;
        }

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > 0.0001f ? value / divisor : value;
        }

        private static int GetThinnestAxis(Vector3 size)
        {
            if (size.x <= size.y && size.x <= size.z)
            {
                return 0;
            }

            return size.y <= size.z ? 1 : 2;
        }
    }
}
