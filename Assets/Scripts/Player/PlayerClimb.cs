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
        [SerializeField]
        private float _ladderClimbDistance;
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
                StartCoroutine(NextStage());
            }
        }

        private IEnumerator NextStage()
        {
            _playerMove.EnableMove(false);
            _rb.gravityScale = 0;

            Vector2 startPos = _rb.position;
            Vector2 endPos = startPos + Vector2.up * _ladderClimbDistance;

            yield return SmoothClimb(startPos, endPos, _climbDuration);

            yield return _waitForSeconds0_25;
            _playerMove.SetMove();
            _playerMove.EnableMove(true);
            _rb.gravityScale = 1;
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
