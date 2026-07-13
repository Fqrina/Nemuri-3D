using UnityEngine;

namespace Nemuri.Interactions
{
    public class KitchenFloorTrigger : MonoBehaviour
    {
        private bool _hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered) return;

            // Check if player touched the kitchen floor
            if (other.CompareTag("Player") || other.GetComponent<Nemuri.Player.PlayerMovement>() != null)
            {
                if (WalkingSceneObjectiveManager.Instance != null && 
                    WalkingSceneObjectiveManager.Instance.CurrentObjective == WalkingSceneObjectiveManager.WalkingObjective.GoToKitchen)
                {
                    _hasTriggered = true;
                    WalkingSceneObjectiveManager.Instance.CompleteKitchenObjective();
                }
            }
        }
    }
}
