using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>경찰관: 보스·정예의 행동을 짧게 중단한다.</summary>
    public sealed class BossInterruptSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 6f;
        public const float InterruptDurationSeconds = 2f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.BossInterrupt;

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

            EnemyRuntime target = FindInterruptTarget(
                context.Enemies,
                context.AttackerPosition,
                context.RangeInWorldUnits);

            if (target == null)
            {
                return;
            }

            float duration = InterruptDurationSeconds * context.SkillValueMultiplier;
            target.PauseAbilities(duration);
            CombatVisualEvents.RaisePassengerSkillActivated(context.Runtime.InstanceId);
            _cooldownRemaining = BaseCooldownSeconds;
        }

        public static EnemyRuntime FindInterruptTarget(
            System.Collections.Generic.IReadOnlyList<EnemyRuntime> enemies,
            Vector2 attackerPosition,
            float rangeInWorldUnits)
        {
            if (enemies == null || rangeInWorldUnits <= 0f)
            {
                return null;
            }

            float rangeSq = rangeInWorldUnits * rangeInWorldUnits;
            EnemyRuntime boss = null;
            EnemyRuntime elite = null;

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

                if (enemy.EnemyType == EnemyType.Boss)
                {
                    boss = enemy;
                    break;
                }

                if (elite == null
                    && (enemy.EnemyType == EnemyType.Elite || enemy.IsElitePromoted))
                {
                    elite = enemy;
                }
            }

            return boss ?? elite;
        }
    }
}
