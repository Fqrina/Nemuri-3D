using UnityEngine;
using System.Collections;
using Nemuri.Player;
using Nemuri.UI;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(Interactable))]
    public class Pills : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioClip _pillsSoundClip;
        [SerializeField, Min(0f)] private float _volume = 1f;

        [Header("Fade Settings")]
        [SerializeField] private float _fadeToBlackDuration = 0.25f;
        [SerializeField] private float _fadeToClearDuration = 0.5f;

        private Interactable _interactable;
        private AudioSource _audioSource;
        private bool _isEaten;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound for eating pills under black screen
        }

        private void Start()
        {
            _interactable.OnInteract.AddListener(EatPills);
            _interactable.PromptText = "E to eat";
            _interactable.HoldSeconds = 0f; // Ensure instant interaction
        }

        private void EatPills()
        {
            if (_isEaten) return;
            _isEaten = true;

            StartCoroutine(EatPillsRoutine());
        }

        private IEnumerator EatPillsRoutine()
        {
            // 1. Disable player movement
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(false);
            }

            // 2. Disable interaction prompt and deactivate interactable
            _interactable.enabled = false;

            // 3. Fast fade to black
            yield return ScreenFader.Instance.FadeToBlack(_fadeToBlackDuration);

            // 4. Hide visuals/colliders of the pills immediately under the black screen
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                c.enabled = false;
            }

            // 5. Play sound and wait for it to finish
            if (_pillsSoundClip != null && _audioSource != null)
            {
                _audioSource.clip = _pillsSoundClip;
                _audioSource.volume = _volume;
                _audioSource.Play();
                
                // Wait until the audio source finishes playing
                while (_audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                // Fallback wait if no sound clip is assigned
                yield return new WaitForSeconds(1.0f);
            }

            // 6. Fast fade out black screen
            yield return ScreenFader.Instance.FadeToClear(_fadeToClearDuration);

            // 7. Enable player movement
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.SetCanMove(true);
            }

            // 8. Finally, destroy the pills GameObject
            Destroy(gameObject);
        }
    }
}
