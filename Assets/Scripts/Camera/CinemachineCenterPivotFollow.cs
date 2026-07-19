using UnityEngine;
using Cinemachine;

[ExecuteAlways]
public class CinemachineCenterPivotFollow : CinemachineExtension
{
    [Header("References")]
    [Tooltip("The center pivot point to align the camera with.")]
    public Transform centerPivot;

    [Header("Camera Positioning")]
    [Tooltip("Horizontal distance to maintain behind the target relative to the CenterPivot.")]
    public float distance = 6f;
    [Tooltip("Height offset above the target's position.")]
    public float height = 5f;

    [Header("LookAt Settings")]
    [Tooltip("Vertical and horizontal offset to apply to the target LookAt position.")]
    public Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    protected override void Awake()
    {
        base.Awake();

        if (centerPivot == null)
        {
            GameObject pivotObj = GameObject.Find("CenterPivot");
            if (pivotObj != null)
            {
                centerPivot = pivotObj.transform;
            }
        }
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (centerPivot == null)
        {
            return;
        }

        if (stage == CinemachineCore.Stage.Body)
        {
            Transform followTarget = vcam.Follow;
            if (followTarget != null)
            {
                Vector3 targetPos = followTarget.position;
                Vector3 radialDir = targetPos - centerPivot.position;
                radialDir.y = 0f;

                Vector3 forward = radialDir.sqrMagnitude > 0.001f ? radialDir.normalized : Vector3.forward;

                Vector3 desiredPos = targetPos - forward * distance;
                desiredPos.y = targetPos.y + height;

                state.RawPosition = desiredPos;

                Vector3 lookTarget = targetPos + lookAtOffset;
                Vector3 lookDir = lookTarget - desiredPos;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    state.RawOrientation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
            }
        }
    }
}
