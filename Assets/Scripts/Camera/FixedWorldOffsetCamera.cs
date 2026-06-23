using UnityEngine;

public class FixedWorldOffsetCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 worldOffset = new Vector3(-10, 3, 0);

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + worldOffset;
        }
    }
}
