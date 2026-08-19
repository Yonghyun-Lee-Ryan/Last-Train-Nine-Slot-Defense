using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>수험생: 체력이 가장 높은 적에게 집중 사격.</summary>
    public sealed class FocusShotSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 4f;
        public const float DamageMultiplier = 2.4f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.FocusShot;

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

            EnemyRuntime target = FindHighestHpTarget(
                context.Enemies,
                context.AttackerPosition,
                context.RangeInWorldUnits);
            if (target == null)
            {
                return;
            }

            float damage = context.Runtime.GetEffectiveAttack()
                           * DamageMultiplier
                           * context.SkillValueMultiplier;
            DamageService.ApplyDamage(target, damage, isCrit: true);
            CombatVisualEvents.RaisePassengerSkillActivated(context.Runtime.InstanceId);
            _cooldownRemaining = BaseCooldownSeconds;
        }

        public static EnemyRuntime FindHighestHpTarget(
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
            float bestHp = -1f;
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

                if (enemy.CurrentHealth <= bestHp)
                {
                    continue;
                }

                best = enemy;
                bestHp = enemy.CurrentHealth;
            }

            return best;
        }
    }
}
