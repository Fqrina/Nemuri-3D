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
    public float lookAtHeightOffset = 1.0f;

    [Header("Smoothing Settings")]
    public float smoothSpeedIn = 20.0f;
    public float smoothSpeedOut = 5.0f;

    private float m_CurrentDistance = -1f;

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
        if (stage == CinemachineCore.Stage.Aim)
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

                    float targetDistance = horizontalDistance;

                    if (Physics.SphereCast(rayStart, cameraSphereRadius, rayDir, out hit, horizontalDistance, collisionLayers))
                    {
                        targetDistance = Mathf.Max(minDistance, hit.distance - wallSafetyDistance);
                    }

                    if (m_CurrentDistance < 0f || deltaTime < 0f)
                    {
                        m_CurrentDistance = targetDistance;
                    }
                    else
                    {
                        float speed = (targetDistance < m_CurrentDistance) ? smoothSpeedIn : smoothSpeedOut;
                        m_CurrentDistance = Mathf.Lerp(m_CurrentDistance, targetDistance, speed * deltaTime);
                    }

                    Vector3 newPos = targetPos + rayDir * m_CurrentDistance;
                    newPos.y = rawPos.y;
                    state.RawPosition = newPos;

                    Transform lookAtTarget = vcam.LookAt;
                    Vector3 lookPoint = lookAtTarget != null
                        ? lookAtTarget.position
                        : targetPos + Vector3.up * lookAtHeightOffset;

                    Vector3 lookDir = lookPoint - newPos;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        state.RawOrientation = Quaternion.LookRotation(lookDir, Vector3.up);
                    }
                }
                else
                {
                    m_CurrentDistance = -1f;
                }
            }
        }
    }
}
