using UnityEngine;

public class KillPart : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        TryKillPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKillPlayer(collision.gameObject);
    }

    private void TryKillPlayer(GameObject obj)
    {
        if (obj == null) return;

        PlayerHealth ph = obj.GetComponent<PlayerHealth>();
        if (ph == null && obj.transform.parent != null)
        {
            ph = obj.transform.parent.GetComponent<PlayerHealth>();
        }

        if (ph != null)
        {
            Debug.Log("[KillPart] Player touched " + gameObject.name + "! Instantly killing player...");
            ph.TakeDamage(99999f);
        }
    }
}
