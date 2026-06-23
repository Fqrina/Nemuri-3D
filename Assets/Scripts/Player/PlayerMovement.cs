using UnityEngine;
using UnityEngine.InputSystem;

namespace Nemuri.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int LastMoveXHash = Animator.StringToHash("LastMoveX");
        private static readonly int LastMoveYHash = Animator.StringToHash("LastMoveY");

        public static PlayerMovement Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;

        private Rigidbody _rb;
        private Animator _animator;
        private Vector2 _moveInput;
        private Vector2 _lastMoveDirection = Vector2.down;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private bool _canMove = true;

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
            _playerInput = GetComponent<PlayerInput>();

            _rb.useGravity = false;
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

            if (_moveAction == null)
            {
                Debug.LogWarning("[PlayerMovement] Move action was not found on the Player action map.", this);
            }
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

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            _rb.MovePosition(_rb.position + moveDirection * _moveSpeed * Time.fixedDeltaTime);
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
            if (_moveInput != Vector2.zero)
            {
                _animator.SetBool(IsMovingHash, true);
                _animator.SetFloat(MoveXHash, _moveInput.x);
                _animator.SetFloat(MoveYHash, _moveInput.y);
                _lastMoveDirection = _moveInput;
            }
            else
            {
                _animator.SetBool(IsMovingHash, false);
            }

            _animator.SetFloat(LastMoveXHash, _lastMoveDirection.x);
            _animator.SetFloat(LastMoveYHash, _lastMoveDirection.y);
        }
    }
}
