using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>야근 직장인: 일정 주기로 추가 서류 피해를 투척한다.</summary>
    public sealed class PaperThrowSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 2.8f;
        public const float BonusDamageMultiplier = 1.5f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.PaperThrow;

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

            EnemyRuntime target = TargetingService.SelectTarget(
                context.Enemies,
                context.AttackerPosition,
                context.RangeInWorldUnits,
                context.Runtime.Data.TargetPriority);

            if (target == null || !target.IsAlive)
            {
                return;
            }

            float damage = context.Runtime.GetEffectiveAttack()
                           * BonusDamageMultiplier
                           * context.SkillValueMultiplier;
            DamageService.ApplyDamage(target, damage);
            CombatVisualEvents.RaisePassengerSkillActivated(context.Runtime.InstanceId);
            _cooldownRemaining = BaseCooldownSeconds;
        }
    }
}
