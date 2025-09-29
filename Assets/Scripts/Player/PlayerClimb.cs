using System.Collections;
using Assets.Scripts.Core.Managers;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(PlayerMove))]
    public class PlayerClimb : MonoBehaviour, IPlayerAbility
    {
        private static readonly WaitForSeconds _waitForSeconds0_25 = new(0.25f);
        #region SerializeFields
        [SerializeField]
        private float _climbDuration;
        #endregion

        private PlayerMove _playerMove;
        private PlayerDash _playerDash;
        private bool _isClimbing;
        private Rigidbody2D _rb;
        private PlayerStateManager _stateManager;
        private PlayerCollisionHandler _collisionHandler;

        #region IPlayerAbility Implementation
        public bool IsActive { get; private set; }
        
        public bool CanActivate()
        {
            return _stateManager == null || 
                   (_stateManager.CurrentState != PlayerState.Preview &&
                    _stateManager.CurrentState != PlayerState.Dead && !_isClimbing);
        }
        
        public void Activate()
        {
            IsActive = true;
        }
        
        public void Deactivate()
        {
            IsActive = false;
            if (_isClimbing)
            {
                StopClimbing();
            }
        }
        
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
        #endregion

        void Awake()
        {
            _playerMove = GetComponent<PlayerMove>();
            _playerDash = GetComponent<PlayerDash>();
            _stateManager = GetComponent<PlayerStateManager>();
            _collisionHandler = GetComponent<PlayerCollisionHandler>();
        }

        void Start()
        {
            _rb = _playerMove.GetRigidbody();
            _isClimbing = false;
            IsActive = true;
            
            // Subscribe to collision events
            if (_collisionHandler != null)
            {
                _collisionHandler.OnCollisionEnter += HandleCollision;
                _collisionHandler.OnCollisionExit += HandleCollisionExit;
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_collisionHandler != null)
            {
                _collisionHandler.OnCollisionEnter -= HandleCollision;
                _collisionHandler.OnCollisionExit -= HandleCollisionExit;
            }
        }

        #region New Collision Handling
        private void HandleCollision(Collision2D collision)
        {
            if (!enabled) return;
            
            if (collision.gameObject.CompareTag("Ladder"))
            {
                StartClimbing(collision.gameObject);
            }
        }

        private void HandleCollisionExit(Collision2D collision)
        {
            if (!enabled) return;
            
            if (collision.gameObject.CompareTag("Ladder"))
            {
                StopClimbing();
            }
        }

        private void StartClimbing(GameObject ladder)
        {
            if (!CanActivate()) return;
            
            _isClimbing = true;
            IsActive = true;
            
            // Change state to climbing
            if (_stateManager != null)
                _stateManager.ChangeState(PlayerState.Climbing);
            
            // Disable dash
            if (_playerDash != null)
                _playerDash.SetEnabled(false);
            
            // Fire event
            PlayerEvents.OnClimbStart?.Invoke();
            
            // Start climbing animation and coroutine
            StartCoroutine(NextStage(ladder));
        }

        private void StopClimbing()
        {
            if (!_isClimbing) return;
            
            _isClimbing = false;
            IsActive = false;
            
            // Re-enable dash
            if (_playerDash != null)
                _playerDash.SetEnabled(true);
            
            // Reset state
            if (_stateManager != null)
                _stateManager.ChangeState(PlayerState.Moving);
            
            // Handle score
            ScoreManager.Instance.RecycleFloor();
            ScoreManager.Instance.AddScore();
            
            // Fire event
            PlayerEvents.OnClimbEnd?.Invoke();
        }
        #endregion


        private IEnumerator NextStage(GameObject ladder)
        {
            // Cancel any active dash
            if (_playerDash != null)
                _playerDash.CancelDash();
            
            // Disable movement
            if (_playerMove != null)
            {
                _playerMove.SetEnabled(false);
                _playerMove.EnableMove(false);
            }

            ScoreManager.Instance.SetNewFloor();

            _rb.gravityScale = 0;

            Vector2 startPos = _rb.position;
            Vector2 endPos = CalculateLadderTopPosition(ladder);
            _playerMove.SetMove();

            yield return SmoothClimb(startPos, endPos, _climbDuration);

            yield return _waitForSeconds0_25;
            
            // Re-enable movement
            if (_playerMove != null)
            {
                _playerMove.EnableMove(true);
                _playerMove.SetEnabled(true);
            }
            
            _rb.gravityScale = 1;
        }

        private Vector2 CalculateLadderTopPosition(GameObject ladder)
        {
            if (ladder.TryGetComponent<Collider2D>(out var ladderCollider))
            {
                float ladderTop = ladderCollider.bounds.max.y;
                return new Vector2(_rb.position.x, ladderTop + 1.65f);
            }
            
            if (ladder.TryGetComponent<SpriteRenderer>(out var ladderSprite))
            {
                float ladderTop = ladderSprite.bounds.max.y;
                return new Vector2(_rb.position.x, ladderTop + 0.5f);
            }
            
            return _rb.position;
        }

        private IEnumerator SmoothClimb(Vector2 startPos, Vector2 endPos, float duration)
        {
            float elapsed = 0f;
            Vector2 totalDistance = endPos - startPos;
            Vector2 climbVelocity = totalDistance / duration;

            while (elapsed < duration)
            {
                _rb.linearVelocity = climbVelocity;
                elapsed += Time.deltaTime;
                yield return null;
            }

            _rb.linearVelocity = Vector2.zero;
            _rb.position = endPos;
        }
    }
}
