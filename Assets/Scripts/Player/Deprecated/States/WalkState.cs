using Assets.Scripts.Core.Managers;
using UnityEngine;

namespace Assets.Scripts.Player.States
{
    public class WalkState : PlayerBaseState
    {
        public WalkState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

        public override void HandleInput()
        {
            if (GameInput.Instance.IsJumpPressed() && player.IsGrounded())
                stateMachine.ChangeState(new JumpState(player, stateMachine));
            else if (GameInput.Instance.IsDashPressed())
                stateMachine.ChangeState(new DashState(player, stateMachine));
        }

        public override void PhysicsUpdate()
        {
            // version 3
            float direction = player.IsFlipX() ? -1f : 1f;
            float targetX = direction * player.MoveSpeed;

            // smooth transition ke kecepatan target
            float newX = Mathf.Lerp(player.Rb.linearVelocityX, targetX, 0.1f);
            player.Rb.linearVelocity = new Vector2(newX, player.Rb.linearVelocityY);
        }
    }    
}
