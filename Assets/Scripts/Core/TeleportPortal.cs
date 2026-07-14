using UnityEngine;
using Nemuri.Dialogue;
using Nemuri.Interactions;

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
            if (!other.CompareTag(PlayerTag) && !other.TryGetComponent(out Nemuri.Player.PlayerMovement _))
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
                // todo: uncomment this when the target scene is ready
                // UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);
                return;
            }

            playerTransform.position = _targetPosition;

            if (playerTransform.TryGetComponent(out Rigidbody rb))
            {
                rb.position = _targetPosition;
                rb.linearVelocity = Vector3.zero;
            }
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
