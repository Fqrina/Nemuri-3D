using UnityEngine;

namespace Nemuri.Player
{
    public class PlayerMovementCenterPivot : PlayerMovement
    {
        [Header("Center Pivot Settings")]
        [SerializeField] private Transform _centerPivot;

        protected override void Awake()
        {
            base.Awake();

            if (_centerPivot == null)
            {
                GameObject pivotObj = GameObject.Find("CenterPivot");
                if (pivotObj != null)
                {
                    _centerPivot = pivotObj.transform;
                }
            }
        }

        protected override void Move()
        {
            if (_centerPivot == null)
            {
                base.Move();
                return;
            }

            Vector3 radialDir = transform.position - _centerPivot.position;
            radialDir.y = 0f;

            Vector3 forward = radialDir.sqrMagnitude > 0.001f ? radialDir.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 moveDirection = (forward * _moveInput.y + right * _moveInput.x);
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

            Vector3 targetVelocity = moveDirection * _moveSpeed;
            targetVelocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = targetVelocity;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
            }
        }
    }
}
