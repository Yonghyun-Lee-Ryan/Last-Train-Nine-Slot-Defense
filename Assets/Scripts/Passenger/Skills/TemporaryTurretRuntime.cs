using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>임시 터렛 런타임. View 없이도 공격 로직이 동작한다.</summary>
    public sealed class TemporaryTurretRuntime
    {
        public Vector2 Position { get; private set; }
        public float RemainingDuration { get; private set; }
        public float Damage { get; private set; }
        public float RangeInWorldUnits { get; private set; }
        public float AttackInterval { get; private set; }
        public float AttackCooldownRemaining { get; private set; }
        public bool IsExpired => RemainingDuration <= 0f;

        public void Activate(
            Vector2 position,
            float durationSeconds,
            float damage,
            float rangeInWorldUnits,
            float attackInterval)
        {
            Position = position;
            RemainingDuration = Mathf.Max(0.01f, durationSeconds);
            Damage = Mathf.Max(0f, damage);
            RangeInWorldUnits = Mathf.Max(0f, rangeInWorldUnits);
            AttackInterval = Mathf.Max(0.05f, attackInterval);
            AttackCooldownRemaining = 0f;
        }

        /// <summary>지속 시간을 줄이고, 준비되면 사거리 내 적 1명에게 피해를 준다.</summary>
        public bool Tick(float deltaTime, IReadOnlyList<EnemyRuntime> enemies)
        {
            if (IsExpired)
            {
                return false;
            }

            RemainingDuration = Mathf.Max(0f, RemainingDuration - deltaTime);
            AttackCooldownRemaining = Mathf.Max(0f, AttackCooldownRemaining - deltaTime);

            if (AttackCooldownRemaining > 0f || Damage <= 0f)
            {
                return false;
            }

            EnemyRuntime target = LastTrain.Passenger.TargetingService.SelectTarget(
                enemies,
                Position,
                RangeInWorldUnits,
                TargetPriority.Nearest);

            if (target == null)
            {
                return false;
            }

            DamageService.ApplyDamage(target, Damage);
            AttackCooldownRemaining = AttackInterval;
            return true;
        }
    }
}
