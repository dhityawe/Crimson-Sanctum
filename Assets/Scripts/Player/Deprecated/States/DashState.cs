using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Player.States
{
    public class DashState : PlayerBaseState
    {
        private float dashTimer;
        private Vector2 dashDir;
        private bool cooldownActive;
        public float LastLinearVelocityX { get; private set; }

        public bool IsDashing { get; private set; }

        public DashState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

        public override void Enter()
        {
            IsDashing = true;
            LastLinearVelocityX = player.Rb.linearVelocityX;
            dashTimer = player._dashDuration;
            dashDir = player.IsFlipX() ? Vector2.left : Vector2.right;
            cooldownActive = true;
            player.StartCoroutine(CooldownRoutine());
        }

        public override void LogicUpdate()
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                stateMachine.ChangeState(new WalkState(player, stateMachine));
        }

        public override void PhysicsUpdate()
        {
            player.Rb.linearVelocity = dashDir * player._dashSpeed;
        }

        public void EndDash()
        {
            IsDashing = false;
            // reset velocity ke arah sebelumnya
            player.Rb.linearVelocity = new Vector2(LastLinearVelocityX, 0f);
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(player._cooldownTime);
            cooldownActive = false;
        }

        public override void Exit()
        {
            EndDash();
        }
    }
} 
