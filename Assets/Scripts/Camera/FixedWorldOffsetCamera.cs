using UnityEngine;

public class FixedWorldOffsetCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 worldOffset = new Vector3(-10, 3, 0);

    [Header("Collision Settings")]
    public bool enableCollision = true;
    public LayerMask collisionLayers = ~0;
    public float cameraSphereRadius = 0.5f;
    public float playerCollisionHeightOffset = 1.0f;
    public float wallSafetyDistance = 0.3f;
    public float minDistance = 0.5f;

    [Header("Rotation Settings")]
    public bool updateRotation = true;
    public Vector3 lookAtOffset = new Vector3(0, 1.0f, 0);
    public float rotationSmoothSpeed = 10.0f;

    [Header("Smoothing")]
    public bool smoothMovement = false;
    public float movementSmoothSpeed = 10.0f;

    private void Start()
    {
        if (collisionLayers.value == ~0 || collisionLayers.value == 0)
        {
            int layer = LayerMask.NameToLayer("cameraCollision");
            if (layer == -1)
            {
                layer = LayerMask.NameToLayer("CameraCollision");
            }

            if (layer != -1)
            {
                collisionLayers = 1 << layer;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + worldOffset;

        if (enableCollision)
        {
            Vector3 rayStart = target.position + Vector3.up * playerCollisionHeightOffset;
            Vector3 horizontalOffset = new Vector3(worldOffset.x, 0f, worldOffset.z);
            float horizontalDistance = horizontalOffset.magnitude;

            if (horizontalDistance > 0.001f)
            {
                Vector3 rayDir = horizontalOffset.normalized;
                RaycastHit hit;

                if (Physics.SphereCast(rayStart, cameraSphereRadius, rayDir, out hit, horizontalDistance, collisionLayers))
                {
                    float adjustedDistance = Mathf.Max(minDistance, hit.distance - wallSafetyDistance);
                    desiredPosition = target.position + rayDir * adjustedDistance;
                    desiredPosition.y = target.position.y + worldOffset.y;
                }
            }
        }

        if (smoothMovement)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, movementSmoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = desiredPosition;
        }

        if (updateRotation)
        {
            Vector3 lookTarget = target.position + lookAtOffset;
            Vector3 lookDirection = lookTarget - transform.position;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                if (smoothMovement)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = targetRotation;
                }
            }
        }
    }
}
