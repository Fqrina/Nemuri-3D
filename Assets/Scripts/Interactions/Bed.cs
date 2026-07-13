using UnityEngine;
using Nemuri.Dialogue;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(Interactable))]
    public class Bed : MonoBehaviour
    {
        private Interactable _interactable;
        private bool _interacted;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.AddListener(UseBed);
                _interactable.PromptText = "Hold E to go to bed";
                _interactable.HoldSeconds = 1f; // Must hold 1 second like the door
            }
        }

        private void UseBed()
        {
            if (_interacted) return;

            if (WalkingSceneObjectiveManager.Instance == null) return;

            if (WalkingSceneObjectiveManager.Instance.CurrentObjective != WalkingSceneObjectiveManager.WalkingObjective.GoToBed)
            {
                // If interacted out of order, show feedback dialogue
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowDialogue("Kael", "I'm not ready for bed yet. I should check the kitchen, get some water, and take my pills first.");
                }
                return;
            }

            _interacted = true;
            _interactable.enabled = false;

            // Complete the objective
            WalkingSceneObjectiveManager.Instance.CompleteBedObjective();
        }
    }
}
