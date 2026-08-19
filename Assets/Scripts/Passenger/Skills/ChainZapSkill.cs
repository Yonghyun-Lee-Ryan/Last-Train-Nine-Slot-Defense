using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>승무원: 주 목표 타격 후 근처 적에게 연쇄 피해.</summary>
    public sealed class ChainZapSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 3.2f;
        public const float PrimaryDamageMultiplier = 1.2f;
        public const float ChainDamageMultiplier = 0.7f;
        public const float ChainRadiusRatio = 0.85f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.ChainZap;

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

            EnemyRuntime primary = TargetingService.SelectTarget(
                context.Enemies,
                context.AttackerPosition,
                context.RangeInWorldUnits,
                context.Runtime.Data.TargetPriority);
            if (primary == null || !primary.IsAlive)
            {
                return;
            }

            float primaryDamage = context.Runtime.GetEffectiveAttack()
                                  * PrimaryDamageMultiplier
                                  * context.SkillValueMultiplier;
            DamageService.ApplyDamage(primary, primaryDamage);

            float chainRadius = context.RangeInWorldUnits * ChainRadiusRatio;
            float chainDamage = context.Runtime.GetEffectiveAttack()
                                * ChainDamageMultiplier
                                * context.SkillValueMultiplier;
            EnemyRuntime secondary = FindChainTarget(
                context.Enemies,
                primary,
                chainRadius);
            if (secondary != null)
            {
                DamageService.ApplyDamage(secondary, chainDamage);
            }

            CombatVisualEvents.RaisePassengerSkillActivated(context.Runtime.InstanceId);
            _cooldownRemaining = BaseCooldownSeconds;
        }

        public static EnemyRuntime FindChainTarget(
            System.Collections.Generic.IReadOnlyList<EnemyRuntime> enemies,
            EnemyRuntime primary,
            float radius)
        {
            if (enemies == null || primary == null || radius <= 0f)
            {
                return null;
            }

            float radiusSq = radius * radius;
            EnemyRuntime best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || !enemy.IsTargetable)
                {
                    continue;
                }

                if (ReferenceEquals(enemy, primary)
                    || enemy.InstanceId == primary.InstanceId)
                {
                    continue;
                }

                float distSq = (enemy.Position - primary.Position).sqrMagnitude;
                if (distSq > radiusSq || distSq >= bestDist)
                {
                    continue;
                }

                best = enemy;
                bestDist = distSq;
            }

            return best;
        }
    }
}
