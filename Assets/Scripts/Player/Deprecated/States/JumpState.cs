using UnityEngine;

namespace Assets.Scripts.Player.States
{
    public class JumpState : PlayerBaseState
    {
        public JumpState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

        public override void Enter()
        {
            if (stateMachine.CurrentState is DashState dash && dash.IsDashing)
            {
                float lastX = dash.LastLinearVelocityX;
                dash.EndDash();

                // restore X velocity sebelum dash
                player.Rb.linearVelocity = new Vector2(lastX, player.Rb.linearVelocityY);
            }
            player.Rb.AddForceY(player.JumpForce, ForceMode2D.Impulse);
        }

        public override void PhysicsUpdate()
        {
            if (player.IsGrounded())
            {
                stateMachine.ChangeState(new WalkState(player, stateMachine));
            }
        }
    }
}
