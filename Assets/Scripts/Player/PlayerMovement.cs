using UnityEngine;
using UnityEngine.InputSystem;

namespace Nemuri.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int LastMoveXHash = Animator.StringToHash("LastMoveX");
        private static readonly int LastMoveYHash = Animator.StringToHash("LastMoveY");
        private static readonly int SkillHash = Animator.StringToHash("Skill");

        public static PlayerMovement Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 12f;
        [SerializeField] private string _skillActionName = "Attack";

        private Rigidbody _rb;
        private Animator _animator;
        private Vector2 _moveInput;
        private Vector2 _lastMoveDirection = Vector2.down;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _skillAction;
        private bool _canMove = true;
        private bool _hasSpeedParameter;
        private bool _hasIsMovingParameter;
        private bool _hasMoveXParameter;
        private bool _hasMoveYParameter;
        private bool _hasLastMoveXParameter;
        private bool _hasLastMoveYParameter;
        private bool _hasSkillParameter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _rb = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            _playerInput = GetComponent<PlayerInput>();
            CacheAnimatorParameters();

            _rb.useGravity = true;
            _rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            if (_playerInput == null)
            {
                return;
            }

            _playerInput.actions.Enable();
            _playerInput.SwitchCurrentActionMap("Player");
            _moveAction = _playerInput.currentActionMap?.FindAction("Move");
            _skillAction = _playerInput.currentActionMap?.FindAction(_skillActionName);

            if (_moveAction == null)
            {
                Debug.LogWarning("[PlayerMovement] Move action was not found on the Player action map.", this);
            }

            if (_skillAction != null)
            {
                _skillAction.performed += OnSkillPerformed;
            }
        }

        private void OnDisable()
        {
            if (_skillAction != null)
            {
                _skillAction.performed -= OnSkillPerformed;
            }
        }

        private bool IsGrounded()
        {
            // Highly robust ground check: check raycast up to 2.5m or if vertical velocity has settled near 0
            bool hasRaycastHit = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 2.5f);
            bool isVelocityZero = Mathf.Abs(_rb.linearVelocity.y) < 0.15f;
            return hasRaycastHit || isVelocityZero;
        }

        private void Update()
        {
            if (!_canMove)
            {
                _moveInput = Vector2.zero;
            }
            else if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            // Rona-specific jump ability (Index 1 is Rona)
            if (_canMove && Nemuri.Core.CharacterSwapManager.Instance != null && Nemuri.Core.CharacterSwapManager.Instance.ActiveCharacterIndex == 1)
            {
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded())
                {
                    Vector3 vel = _rb.linearVelocity;
                    vel.y = 8f; // Jump high velocity
                    _rb.linearVelocity = vel;
                }
            }

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            Vector3 moveDirection = Vector3.ClampMagnitude(new Vector3(_moveInput.x, 0f, _moveInput.y), 1f);
            
            float speed = _moveSpeed;
            if (Nemuri.Core.CharacterSwapManager.Instance != null && Nemuri.Core.CharacterSwapManager.Instance.ActiveCharacterIndex == 1)
            {
                speed = 8f; // Rona moves at speed 8
            }

            Vector3 targetVelocity = moveDirection * speed;
            targetVelocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = targetVelocity;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
            }
        }

        public void SetCanMove(bool canMove)
        {
            _canMove = canMove;
            if (!canMove)
            {
                _moveInput = Vector2.zero;
            }
        }

        private void UpdateAnimation()
        {
            if (_animator == null)
            {
                return;
            }

            float speed = Mathf.Clamp01(_moveInput.magnitude);

            if (_hasSpeedParameter)
            {
                _animator.SetFloat(SpeedHash, speed);
            }

            if (_moveInput != Vector2.zero)
            {
                if (_hasIsMovingParameter)
                {
                    _animator.SetBool(IsMovingHash, true);
                }

                if (_hasMoveXParameter)
                {
                    _animator.SetFloat(MoveXHash, _moveInput.x);
                }

                if (_hasMoveYParameter)
                {
                    _animator.SetFloat(MoveYHash, _moveInput.y);
                }

                _lastMoveDirection = _moveInput;
            }
            else
            {
                if (_hasIsMovingParameter)
                {
                    _animator.SetBool(IsMovingHash, false);
                }
            }

            if (_hasLastMoveXParameter)
            {
                _animator.SetFloat(LastMoveXHash, _lastMoveDirection.x);
            }

            if (_hasLastMoveYParameter)
            {
                _animator.SetFloat(LastMoveYHash, _lastMoveDirection.y);
            }
        }

        private void OnSkillPerformed(InputAction.CallbackContext context)
        {
            if (_animator != null && _hasSkillParameter)
            {
                _animator.SetTrigger(SkillHash);
            }
        }

        private void CacheAnimatorParameters()
        {
            if (_animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == SpeedHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    _hasSpeedParameter = true;
                }
                else if (parameter.nameHash == IsMovingHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    _hasIsMovingParameter = true;
                }
                else if (parameter.nameHash == MoveXHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    _hasMoveXParameter = true;
                }
                else if (parameter.nameHash == MoveYHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    _hasMoveYParameter = true;
                }
                else if (parameter.nameHash == LastMoveXHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    _hasLastMoveXParameter = true;
                }
                else if (parameter.nameHash == LastMoveYHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    _hasLastMoveYParameter = true;
                }
                else if (parameter.nameHash == SkillHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    _hasSkillParameter = true;
                }
            }
        }
    }
}