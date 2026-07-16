using UnityEngine;
using Cinemachine;

public class CinemachineConstantYCollider : CinemachineExtension
{
    [Header("Collision Settings")]
    public LayerMask collisionLayers = ~0;
    public float cameraSphereRadius = 0.5f;
    public float playerCollisionHeightOffset = 1.0f;
    public float wallSafetyDistance = 0.3f;
    public float minDistance = 0.5f;

    protected override void Awake()
    {
        base.Awake();

        int layer = LayerMask.NameToLayer("cameraObstacle");
        if (layer == -1)
        {
            layer = LayerMask.NameToLayer("CameraObstacle");
        }

        if (layer != -1)
        {
            collisionLayers = 1 << layer;
        }
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            Transform followTarget = vcam.Follow;
            if (followTarget != null)
            {
                Vector3 targetPos = followTarget.position;
                Vector3 rawPos = state.RawPosition;

                Vector3 horizontalDiff = rawPos - targetPos;
                horizontalDiff.y = 0f;
                float horizontalDistance = horizontalDiff.magnitude;

                if (horizontalDistance > 0.001f)
                {
                    Vector3 rayDir = horizontalDiff.normalized;
                    Vector3 rayStart = targetPos + Vector3.up * playerCollisionHeightOffset;
                    RaycastHit hit;

                    if (Physics.SphereCast(rayStart, cameraSphereRadius, rayDir, out hit, horizontalDistance, collisionLayers))
                    {
                        float adjustedDistance = Mathf.Max(minDistance, hit.distance - wallSafetyDistance);
                        Vector3 newPos = targetPos + rayDir * adjustedDistance;
                        newPos.y = rawPos.y;
                        state.RawPosition = newPos;
                    }
                }
            }
        }
    }
}
