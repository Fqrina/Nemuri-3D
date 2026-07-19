using UnityEngine;
using Nemuri.Player;

namespace Nemuri.Interactions
{
    public class KillTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject spawnLoc;

        private void OnTriggerEnter(Collider other)
        {
            if (spawnLoc == null)
            {
                return;
            }

            GameObject playerObj = null;

            GameObject walkingPlayer = GameObject.Find("Walking Player");
            if (walkingPlayer != null)
            {
                playerObj = walkingPlayer;
            }
            else if (PlayerMovementChapt1.Instance != null)
            {
                playerObj = PlayerMovementChapt1.Instance.gameObject;
            }
            else if (PlayerMovement.Instance != null)
            {
                playerObj = PlayerMovement.Instance.gameObject;
            }
            else if (other.CompareTag("Player"))
            {
                playerObj = other.transform.root.gameObject;
            }

            if (playerObj != null && (other.transform.root.gameObject == playerObj || other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovementChapt1>() != null))
            {
                TeleportPlayer(playerObj);
            }
        }

        private void TeleportPlayer(GameObject playerObj)
        {
            playerObj.transform.position = spawnLoc.transform.position;
            playerObj.transform.rotation = spawnLoc.transform.rotation;

            Rigidbody[] rigidbodies = playerObj.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.position = spawnLoc.transform.position;
                rb.rotation = spawnLoc.transform.rotation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Physics.SyncTransforms();
        }
    }
}
