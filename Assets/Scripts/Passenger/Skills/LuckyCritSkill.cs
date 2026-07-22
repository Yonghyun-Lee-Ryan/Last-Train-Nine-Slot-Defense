using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>고양이: 낮은 확률로 큰 치명타 피해를 준다.</summary>
    public sealed class LuckyCritSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 3f;
        public const float BaseProcChance = 0.2f;
        public const float CritDamageMultiplier = 3f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.LuckyCrit;

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

            float chance = Mathf.Clamp01(
                BaseProcChance
                + context.Modifiers.CatCritChancePercent / 100f
                + context.SynergyModifiers.CritChancePercent / 100f
                + context.RelicCritChancePercent / 100f);
            float roll = context.Random != null ? context.Random.NextFloat() : 0f;
            if (roll > chance)
            {
                _cooldownRemaining = BaseCooldownSeconds;
                return;
            }

            EnemyRuntime target = TargetingService.SelectTarget(
                context.Enemies,
                context.AttackerPosition,
                context.RangeInWorldUnits,
                context.Runtime.Data.TargetPriority);

            if (target == null || !target.IsAlive)
            {
                _cooldownRemaining = BaseCooldownSeconds;
                return;
            }

            float damage = context.Runtime.GetEffectiveAttack()
                           * CritDamageMultiplier
                           * context.SkillValueMultiplier;
            DamageService.ApplyDamage(target, damage, isCrit: true);
            _cooldownRemaining = BaseCooldownSeconds;
        }
    }
}
