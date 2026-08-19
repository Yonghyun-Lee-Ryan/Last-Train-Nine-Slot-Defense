using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>경비원: 객차 근처 적에게 경계 펄스를 가한다.</summary>
    public sealed class PerimeterPulseSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 3.5f;
        public const float DamageMultiplier = 1.1f;
        public const float TrainProximityRatio = 0.55f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.PerimeterPulse;

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

            float radius = context.RangeInWorldUnits * TrainProximityRatio
                           * Mathf.Max(0.5f, context.SkillValueMultiplier);
            float damage = context.Runtime.GetEffectiveAttack()
                           * DamageMultiplier
                           * context.SkillValueMultiplier;
            int hits = ApplyNearTrain(
                context.Enemies,
                context.TrainTarget,
                radius,
                damage);
            if (hits <= 0)
            {
                return;
            }

            CombatVisualEvents.RaiseAreaAttack(context.TrainTarget);
            CombatVisualEvents.RaisePassengerSkillActivated(context.Runtime.InstanceId);
            _cooldownRemaining = BaseCooldownSeconds;
        }

        public static int ApplyNearTrain(
            System.Collections.Generic.IReadOnlyList<EnemyRuntime> enemies,
            Vector2 trainTarget,
            float radius,
            float damage)
        {
            if (enemies == null || radius <= 0f || damage <= 0f)
            {
                return 0;
            }

            float radiusSq = radius * radius;
            int hits = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || !enemy.IsTargetable)
                {
                    continue;
                }

                if ((enemy.Position - trainTarget).sqrMagnitude > radiusSq)
                {
                    continue;
                }

                DamageService.ApplyDamage(enemy, damage);
                hits++;
            }

            return hits;
        }
    }
}
