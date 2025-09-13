using System;
using Assets.Scripts.Player.States;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField, Range(1f, 5f)] private float _moveSpeed;
        [SerializeField] private float _jumpForce;
        [SerializeField] private SpriteRenderer _charSprite;
        [SerializeField] private bool _isFlipx;

        [Header("Ground Settings")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Dash Settings")]
        public float _dashDuration = 0.2f;
        public float _dashSpeed = 20f;
        public float _cooldownTime;

        [Header("Climb Settings")]
        public float _climbDuration;

        #region Events
        public static event Action OnDeath;
        public static event Action OnPickupCoin;
        #endregion
        public Rigidbody2D Rb { get; private set; }
        public PlayerStateMachine StateMachine { get; private set; }
        public bool CanMove { get; private set; }

        void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            StateMachine = new();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            CanMove = true;
            _charSprite.flipX = _isFlipx;
            StateMachine.Initialize(new WalkState(this, StateMachine));
        }

        // Update is called once per frame
        private void Update()
        {
            if (!CanMove) return;
            StateMachine.CurrentState.HandleInput();
            StateMachine.CurrentState.LogicUpdate();
        }

        private void FixedUpdate()
        {
            if (!CanMove) return;
            StateMachine.CurrentState.PhysicsUpdate();
        }

        #region Helper Functions
        public void SetMove()
        {
            _isFlipx = !_isFlipx;
            if (_charSprite != null)
                _charSprite.flipX = _isFlipx;
        }
        public void EnableMove(bool value) => CanMove = value;
        public bool IsFlipX() => _isFlipx;

        public bool IsGrounded()
        {
            return Physics2D.OverlapCapsule(_groundCheck.position, new Vector2(0.7f, 0.1f),
                CapsuleDirection2D.Horizontal, 0, _groundLayer);
        }

        public float MoveSpeed => _moveSpeed;
        public float JumpForce => _jumpForce;
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Bouncable"))
            {
                SetMove();
            }
            else if (other.gameObject.CompareTag("DeathZone"))
            {
                OnDeath?.Invoke();
            }
            else if (other.gameObject.CompareTag("Pickable"))
            {
                OnPickupCoin?.Invoke();
            }
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.gameObject.CompareTag("Ladder"))
            {
                StateMachine.ChangeState(new ClimbState(this, StateMachine, collider.gameObject));
            }
        }
        #endregion
    }
}
