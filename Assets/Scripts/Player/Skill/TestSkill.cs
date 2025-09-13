using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Player.Skills
{
    public class TestSkill : PlayerDash
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected override void Start()
        {
            base.Start();
        }

        // Update is called once per frame
        protected override void Update()
        {
            base.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        protected override void StartDash()
        {
            base.StartDash();
        }

        protected override void EndDash()
        {
            base.EndDash();
        }

        protected override IEnumerator StartCooldownDash(float cooldownTime)
        {
            return base.StartCooldownDash(cooldownTime);
        }
    }
}
