using System.Collections;
using UnityEngine;
using Nemuri.Dialogue;
using Nemuri.Interactions;
using Nemuri.Player;
using Nemuri.UI;

namespace Nemuri.Core
{
    public class TeleportPortal : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        public enum TeleportMode { SameScene, DifferentScene }

        [Header("Teleport Settings")]
        [SerializeField] private TeleportMode _teleportMode = TeleportMode.SameScene;
        [SerializeField] private Vector3 _targetPosition = new Vector3(100f, 0f, 0f);
        [SerializeField] private string _targetSceneName = "";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag) && 
                !other.TryGetComponent(out PlayerMovement _) && 
                !other.TryGetComponent(out PlayerMovementChapt1 _))
                return;

            if (PuzzleManager.Instance == null || !PuzzleManager.Instance.AreAllCrystalsCollected())
            {
                EnsureDialogueManager();
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowDialogue("Kael", "I need to collect all 3 crystals first.");
                }
                return;
            }

            Teleport(other.transform);
        }

        private void Teleport(Transform playerTransform)
        {
            if (_teleportMode == TeleportMode.DifferentScene)
            {
                StartCoroutine(TransitionSceneRoutine());
                return;
            }

            playerTransform.position = _targetPosition;

            if (playerTransform.TryGetComponent(out Rigidbody rb))
            {
                rb.position = _targetPosition;
                rb.linearVelocity = Vector3.zero;
            }
        }

        private IEnumerator TransitionSceneRoutine()
        {
            if (PlayerMovement.Instance != null) PlayerMovement.Instance.SetCanMove(false);
            if (PlayerMovementChapt1.Instance != null) PlayerMovementChapt1.Instance.SetCanMove(false);

            SceneTransitionState.FadeInOnLoad = true;
            SceneTransitionState.FadeInDuration = 2f;

            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToBlack(2f);
            }

            if (string.IsNullOrWhiteSpace(_targetSceneName))
            {
                Debug.LogError("[TeleportPortal] Target scene name is not assigned.", this);
                yield break;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);
        }

        private void EnsureDialogueManager()
        {
            if (DialogueManager.Instance != null) return;
            DialogueManager existingManager = FindAnyObjectByType<DialogueManager>();
            if (existingManager == null)
            {
                new GameObject("DialogueManager").AddComponent<DialogueManager>();
            }
        }
    }
}
