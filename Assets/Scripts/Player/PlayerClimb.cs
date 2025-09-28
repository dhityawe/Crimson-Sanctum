using System.Collections;
using Assets.Scripts.Core.Managers;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(PlayerMove))]
    public class PlayerClimb : MonoBehaviour
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

        void Awake()
        {
            _playerMove = GetComponent<PlayerMove>();
            _playerDash = GetComponent<PlayerDash>();
        }

        void Start()
        {
            _rb = _playerMove.GetRigidbody();
            _isClimbing = false;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ladder"))
            {
                _isClimbing = true;
                if (_isClimbing)
                {
                    // jalanin animasi manjat
                    _playerDash.enabled = false;
                    StartCoroutine(NextStage(collision.gameObject));
                    
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collider)
        {
            if (collider.gameObject.CompareTag("Ladder"))
            {
                _playerDash.enabled = true;
                ScoreManager.Instance.RecycleFloor();
                ScoreManager.Instance.AddScore();
                _isClimbing = false;
            }
        }

        private IEnumerator NextStage(GameObject ladder)
        {
            _playerDash.CancelDash();
            _playerMove.enabled = false;

            ScoreManager.Instance.SetNewFloor();

            _playerMove.EnableMove(false);
            _rb.gravityScale = 0;

            Vector2 startPos = _rb.position;
            Vector2 endPos = CalculateLadderTopPosition(ladder);
            _playerMove.SetMove();

            yield return SmoothClimb(startPos, endPos, _climbDuration);

            yield return _waitForSeconds0_25;
            _playerMove.EnableMove(true);
            _playerMove.enabled = true;
            // ScoreManager.Instance.AddScore(1);
            _rb.gravityScale = 1;
            // _playerDash.enabled = true;
        }

        private Vector2 CalculateLadderTopPosition(GameObject ladder)
        {
            // Get the ladder's collider to determine its bounds
            if (ladder.TryGetComponent<Collider2D>(out var ladderCollider))
            {
                // Get the top of the ladder's collider
                float ladderTop = ladderCollider.bounds.max.y;
                
                // Position the player slightly above the ladder top
                return new Vector2(_rb.position.x, ladderTop + 1.65f);
            }
            
            // Fallback: if no collider found, use sprite bounds
            if (ladder.TryGetComponent<SpriteRenderer>(out var ladderSprite))
            {
                float ladderTop = ladderSprite.bounds.max.y;
                return new Vector2(_rb.position.x, ladderTop + 0.5f);
            }
            
            // Final fallback: return current position (shouldn't happen in normal circumstances)
            return _rb.position;
        }

        private IEnumerator SmoothClimb(Vector2 startPos, Vector2 endPos, float duration)
        {
            float elapsed = 0f;
            Vector2 totalDistance = endPos - startPos;
            Vector2 climbVelocity = totalDistance / duration; // kecepatan konstan yang dibutuhkan

            while (elapsed < duration)
            {
                _rb.linearVelocity = climbVelocity; // gerak dengan velocity, bukan teleport
                elapsed += Time.deltaTime;
                yield return null;
            }

            // pastikan player berhenti di atas ladder
            _rb.linearVelocity = Vector2.zero;
            _rb.position = endPos;
        }
    }
}
