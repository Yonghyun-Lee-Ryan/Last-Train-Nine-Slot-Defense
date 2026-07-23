using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>대학원생: 확률로 범위 내 적에게 한 번씩 치명타 피해를 준다.</summary>
    public sealed class CriticalAreaDamageSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 3.5f;
        public const float BaseProcChance = 0.35f;
        public const float CritDamageMultiplier = 2f;
        public const float AreaRadiusRatio = 1.25f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.CriticalAreaDamage;

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

            float radius = context.RangeInWorldUnits * AreaRadiusRatio * context.SkillValueMultiplier;
            float rawDamage = context.Runtime.GetEffectiveAttack()
                              * CritDamageMultiplier
                              * context.SkillValueMultiplier;

            int hits = ApplyAreaDamageOnce(context.Enemies, context.AttackerPosition, radius, rawDamage);
            if (hits > 0)
            {
                CombatVisualEvents.RaiseAreaAttack(context.AttackerPosition);
            }

            _cooldownRemaining = BaseCooldownSeconds;
        }

        /// <summary>범위 내 살아 있는 적에게 인스턴스당 1회만 피해를 적용한다.</summary>
        public static int ApplyAreaDamageOnce(
            IReadOnlyList<EnemyRuntime> enemies,
            Vector2 center,
            float radius,
            float rawDamage)
        {
            if (enemies == null || radius <= 0f || rawDamage <= 0f)
            {
                return 0;
            }

            var hitIds = new HashSet<string>();
            int hitCount = 0;
            float radiusSq = radius * radius;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || !enemy.IsTargetable)
                {
                    continue;
                }

                if (hitIds.Contains(enemy.InstanceId))
                {
                    continue;
                }

                if ((enemy.Position - center).sqrMagnitude > radiusSq)
                {
                    continue;
                }

                hitIds.Add(enemy.InstanceId);
                DamageService.ApplyDamage(enemy, rawDamage, isCrit: true);
                hitCount++;
            }

            return hitCount;
        }
    }
}
