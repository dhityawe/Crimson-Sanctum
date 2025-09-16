using System.Collections;
using Assets.Scripts.Core.Managers;
using UnityEngine;

namespace Assets.Scripts.Player.States
{
    public class ClimbState : PlayerBaseState
    {
        private static readonly WaitForSeconds _waitForSeconds0_25 = new(0.25f);
        private GameObject ladder;

        public ClimbState(PlayerController player, PlayerStateMachine sm, GameObject ladder)
            : base(player, sm)
        {
            this.ladder = ladder;
        }

        public override void Enter()
        {
            player.StartCoroutine(ClimbRoutine());
        }

        private IEnumerator ClimbRoutine()
        {
            player.EnableMove(false);
            player.Rb.gravityScale = 0;

            ScoreManager.Instance.SetNewFloor();

            Vector2 startPos = player.Rb.position;
            Vector2 endPos = CalculateLadderTopPosition(ladder);
            float elapsed = 0f;

            while (elapsed < player._climbDuration)
            {
                Vector2 totalDist = endPos - startPos;
                Vector2 climbVel = totalDist / player._climbDuration;
                player.Rb.linearVelocity = climbVel;
                elapsed += Time.deltaTime;
                yield return null;
            }

            player.Rb.linearVelocity = Vector2.zero;
            player.Rb.position = endPos;
            player.Rb.gravityScale = 1;
            player.SetMove();
            ScoreManager.Instance.AddScore();

            yield return _waitForSeconds0_25;
            player.EnableMove(true);

            // selesai → balik ke idle
            stateMachine.ChangeState(new WalkState(player, stateMachine));
        }

        private Vector2 CalculateLadderTopPosition(GameObject ladder)
        {
            if (ladder.TryGetComponent<Collider2D>(out var col))
                return new Vector2(player.Rb.position.x, col.bounds.max.y + 1.65f);
            if (ladder.TryGetComponent<SpriteRenderer>(out var sr))
                return new Vector2(player.Rb.position.x, sr.bounds.max.y + 0.5f);

            return player.Rb.position;
        }
    }
}
