using UnityEngine;

namespace Nemuri.Core
{
    public class TeleportPortal : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [Header("Teleport Settings")]
        [SerializeField] private Vector3 _targetPosition = new Vector3(100f, 0f, 0f);

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PlayerTag) || other.TryGetComponent(out Nemuri.Player.PlayerMovement _))
            {
                Teleport(other.transform);
            }
        }

        private void Teleport(Transform playerTransform)
        {
            playerTransform.position = _targetPosition;

            if (playerTransform.TryGetComponent(out Rigidbody rb))
            {
                rb.position = _targetPosition;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
