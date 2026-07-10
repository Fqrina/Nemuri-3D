using UnityEngine;
using Nemuri.Player;

namespace Nemuri.Sounds
{
    [RequireComponent(typeof(Collider))]
    public class NormalFootstepTrigger : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioClip _footstepClip;
        [SerializeField] private float _stepInterval = 0.35f;
        [SerializeField, Min(0f)] private float _volume = 1f;

        private AudioSource _audioSource;
        private bool _isPlayerInside;
        private float _stepTimer;
        private Rigidbody _playerRigidbody;

        private void Awake()
        {
            // Ensure collider is set to trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Get or add AudioSource
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D sound
        }

        private void Update()
        {
            if (!_isPlayerInside || _playerRigidbody == null)
            {
                return;
            }

            // Check if the player is currently moving
            Vector3 horizontalVelocity = new Vector3(_playerRigidbody.linearVelocity.x, 0f, _playerRigidbody.linearVelocity.z);
            bool isMoving = horizontalVelocity.sqrMagnitude > 0.05f;

            if (isMoving)
            {
                _stepTimer += Time.deltaTime;
                if (_stepTimer >= _stepInterval)
                {
                    PlayFootstepSound();
                    _stepTimer = 0f;
                }
            }
            else
            {
                // Reset timer when not moving so the footstep plays immediately upon starting to move again
                _stepTimer = _stepInterval;
            }
        }

        private void PlayFootstepSound()
        {
            if (_footstepClip != null && _audioSource != null)
            {
                float volumeLeft = _volume;
                while (volumeLeft > 0f)
                {
                    float playVol = Mathf.Min(volumeLeft, 1f);
                    _audioSource.PlayOneShot(_footstepClip, playVol);
                    volumeLeft -= 1f;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
            {
                _isPlayerInside = true;
                _playerRigidbody = other.GetComponent<Rigidbody>();
                // Ready to play the first step immediately
                _stepTimer = _stepInterval;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
            {
                _isPlayerInside = false;
                _playerRigidbody = null;
            }
        }
    }
}
