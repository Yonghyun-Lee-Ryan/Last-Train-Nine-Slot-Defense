using LastTrain.Battle;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>바리스타: 주변에 뜨거운 스플래시 피해.</summary>
    public sealed class ScaldSplashSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 3.8f;
        public const float DamageMultiplier = 0.9f;
        public const float SplashRadiusRatio = 1.1f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.ScaldSplash;

        public void Tick(float deltaTime, in PassengerSkillContext context)
        {
            if (context.Runtime == null || context.Runtime.GridSlotIndex < 0)
            {
                return;
            }

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            float radius = context.RangeInWorldUnits * SplashRadiusRatio;
            float damage = context.Runtime.GetEffectiveAttack()
                           * DamageMultiplier
                           * context.SkillValueMultiplier;
            int hits = CriticalAreaDamageSkill.ApplyAreaDamageOnce(
                context.Enemies,
                context.AttackerPosition,
                radius,
                damage);
            if (hits <= 0)
            {
                return;
            }

            CombatVisualEvents.RaiseAreaAttack(context.AttackerPosition);
            CombatVisualEvents.RaisePassengerSkillActivated(context.Runtime.InstanceId);
            _cooldownRemaining = BaseCooldownSeconds;
        }
    }
}
