using System;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>전투 연출 이벤트 버스. 로직과 View/VFX를 분리한다.</summary>
    public static class CombatVisualEvents
    {
        public static event Action<EnemyRuntime, float, bool> EnemyDamaged;
        public static event Action<EnemyRuntime> EnemyKilled;
        public static event Action<string> PassengerAttacked;
        public static event Action<string> PassengerSkillActivated;
        public static event Action<Vector2> TrainHealed;
        public static event Action<Vector2> AreaAttack;
        public static event Action<Vector2> KnockbackApplied;
        public static event Action<float> TrainDamaged;

        public static void RaiseEnemyDamaged(EnemyRuntime enemy, float damage, bool isCrit = false)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyDamaged?.Invoke(enemy, damage, isCrit);
        }

        public static void RaiseEnemyKilled(EnemyRuntime enemy)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyKilled?.Invoke(enemy);
        }

        public static void RaisePassengerAttacked(string passengerInstanceId)
        {
            if (string.IsNullOrWhiteSpace(passengerInstanceId))
            {
                return;
            }

            PassengerAttacked?.Invoke(passengerInstanceId);
        }

        public static void RaisePassengerSkillActivated(string passengerInstanceId)
        {
            if (string.IsNullOrWhiteSpace(passengerInstanceId))
            {
                return;
            }

            PassengerSkillActivated?.Invoke(passengerInstanceId);
        }

        public static void RaiseTrainHealed(Vector2 worldPosition)
        {
            TrainHealed?.Invoke(worldPosition);
        }

        public static void RaiseAreaAttack(Vector2 worldPosition)
        {
            AreaAttack?.Invoke(worldPosition);
        }

        public static void RaiseKnockbackApplied(Vector2 worldPosition)
        {
            KnockbackApplied?.Invoke(worldPosition);
        }

        public static void RaiseTrainDamaged(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            TrainDamaged?.Invoke(damage);
        }
    }
}
