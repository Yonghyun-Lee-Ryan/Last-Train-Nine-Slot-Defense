using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Passenger
{
    /// <summary>
    /// 승객 공격 쿨타임·타깃 선택·발사 순수 로직.
    /// MonoBehaviour에 의존하지 않는다.
    /// </summary>
    public sealed class PassengerAttackController
    {
        /// <summary>
        /// 한 프레임 공격 처리. Grid에 배치된 승객만 공격한다.
        /// </summary>
        /// <returns>공격을 발사했으면 true.</returns>
        public bool Tick(
            float deltaTime,
            PassengerRuntime runtime,
            Vector2 attackerPosition,
            float rangeInWorldUnits,
            IReadOnlyList<EnemyRuntime> enemies,
            IProjectileLauncher launcher,
            float fastEnemyDamagePercent = 0f,
            float bossDamagePercent = 0f)
        {
            if (runtime == null || launcher == null || runtime.GridSlotIndex < 0)
            {
                return false;
            }

            runtime.TickAttackCooldown(deltaTime);
            if (!runtime.IsAttackReady)
            {
                return false;
            }

            EnemyRuntime target = TargetingService.SelectTarget(
                enemies,
                attackerPosition,
                rangeInWorldUnits,
                runtime.Data.TargetPriority);

            if (target == null)
            {
                return false;
            }

            float damage = runtime.GetEffectiveAttack();
            if (fastEnemyDamagePercent != 0f && target.EnemyType == EnemyType.Fast)
            {
                damage *= 1f + fastEnemyDamagePercent / 100f;
            }

            if (bossDamagePercent != 0f && target.EnemyType == EnemyType.Boss)
            {
                damage *= 1f + bossDamagePercent / 100f;
            }

            launcher.Launch(attackerPosition, target, damage, runtime.Data.Id);
            runtime.SetAttackCooldownRemaining(runtime.GetEffectiveAttackInterval());
            CombatVisualEvents.RaisePassengerAttacked(runtime.InstanceId);
            return true;
        }
    }
}
