using UnityEngine;
using Nemuri.Dialogue;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(Interactable))]
    public class GlassOfWater : MonoBehaviour
    {
        private Interactable _interactable;
        private bool _isTaken;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.AddListener(TakeGlass);
                _interactable.PromptText = "E to take glass of water";
                _interactable.HoldSeconds = 0f; // Instant interaction
            }
        }

        private void TakeGlass()
        {
            if (_isTaken) return;

            if (WalkingSceneObjectiveManager.Instance == null) return;

            if (WalkingSceneObjectiveManager.Instance.CurrentObjective != WalkingSceneObjectiveManager.WalkingObjective.TakeWater)
            {
                // If interacted out of order, show feedback dialogue
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowDialogue("Kael", "I don't need water right now. I should find the kitchen first.");
                }
                return;
            }

            _isTaken = true;
            _interactable.enabled = false;

            // Complete the objective
            WalkingSceneObjectiveManager.Instance.CompleteWaterObjective();

            // Hide the glass immediately
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

            Destroy(gameObject, 0.5f);
        }
    }
}
