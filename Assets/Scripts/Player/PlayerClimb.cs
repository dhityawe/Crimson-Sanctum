using System.Collections;
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
        private Rigidbody2D _rb;

        void Awake()
        {
            _playerMove = GetComponent<PlayerMove>();
            
        }

        void Start()
        {
            _rb = _playerMove.GetRigidbody();
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Ladder"))
            {
                // jalanin animasi manjat
                StartCoroutine(NextStage(collision.gameObject));
            }
        }

        private IEnumerator NextStage(GameObject ladder)
        {
            _playerMove.EnableMove(false);
            _rb.gravityScale = 0;

            Vector2 startPos = _rb.position;
            Vector2 endPos = CalculateLadderTopPosition(ladder);

            yield return SmoothClimb(startPos, endPos, _climbDuration);

            yield return _waitForSeconds0_25;
            _playerMove.SetMove();
            _playerMove.EnableMove(true);
            _rb.gravityScale = 10;
        }

        private Vector2 CalculateLadderTopPosition(GameObject ladder)
        {
            // Get the ladder's collider to determine its bounds
            if (ladder.TryGetComponent<Collider2D>(out var ladderCollider))
            {
                // Get the top of the ladder's collider
                float ladderTop = ladderCollider.bounds.max.y;
                
                // Position the player slightly above the ladder top
                return new Vector2(_rb.position.x, ladderTop + 0.6f);
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
            float elapsed = 0;
            while (elapsed < duration)
            {
                _rb.MovePosition(Vector2.Lerp(startPos, endPos, elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }
            _rb.MovePosition(endPos);
        }
    }
}
