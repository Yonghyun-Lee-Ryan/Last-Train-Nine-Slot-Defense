using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>배달기사: 체력이 낮은 적에게 추가 피해를 준다.</summary>
    public sealed class LowHpBonusSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 2.2f;
        public const float LowHealthRatioThreshold = 0.5f;
        public const float BonusDamageMultiplier = 1.8f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.LowHpBonus;

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

            EnemyRuntime target = FindLowHealthTarget(
                context.Enemies,
                context.AttackerPosition,
                context.RangeInWorldUnits);

            if (target == null)
            {
                return;
            }

            float damage = context.Runtime.GetEffectiveAttack()
                           * BonusDamageMultiplier
                           * context.SkillValueMultiplier;
            DamageService.ApplyDamage(target, damage);
            _cooldownRemaining = BaseCooldownSeconds;
        }

        public static EnemyRuntime FindLowHealthTarget(
            System.Collections.Generic.IReadOnlyList<EnemyRuntime> enemies,
            Vector2 attackerPosition,
            float rangeInWorldUnits)
        {
            if (enemies == null || rangeInWorldUnits <= 0f)
            {
                return null;
            }

            float rangeSq = rangeInWorldUnits * rangeInWorldUnits;
            EnemyRuntime best = null;
            float bestRatio = float.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || !enemy.IsTargetable)
                {
                    continue;
                }

                if ((enemy.Position - attackerPosition).sqrMagnitude > rangeSq)
                {
                    continue;
                }

                float ratio = enemy.HealthRatio;
                if (ratio > LowHealthRatioThreshold || ratio >= bestRatio)
                {
                    continue;
                }

                best = enemy;
                bestRatio = ratio;
            }

            return best;
        }
    }
}
