using UnityEngine;

namespace Nemuri.Core
{
    public class KillTriggerPuzzle3 : MonoBehaviour
    {
        private Vector3 _targetPosition;

        private void Awake()
        {
            var spawn = GameObject.Find("Puzzle3SpawnLoc");
            if (spawn != null)
                _targetPosition = spawn.transform.position;
            else
                Debug.LogError("KillTriggerPuzzle3: Puzzle3SpawnLoc not found in scene!");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.TryGetComponent(out Nemuri.Player.PlayerMovement _))
                return;

            other.transform.position = _targetPosition;

            if (other.TryGetComponent(out Rigidbody rb))
            {
                rb.position = _targetPosition;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
