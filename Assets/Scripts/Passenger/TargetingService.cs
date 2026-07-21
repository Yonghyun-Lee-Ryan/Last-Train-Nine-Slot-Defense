using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger
{
    /// <summary>
    /// 타깃 선택 순수 로직. EditMode 테스트 가능.
    /// </summary>
    public static class TargetingService
    {
        private const float ScoreEpsilon = 0.0001f;

        public static EnemyRuntime SelectTarget(
            IReadOnlyList<EnemyRuntime> enemies,
            Vector2 attackerPosition,
            float range,
            TargetPriority priority)
        {
            if (enemies == null || enemies.Count == 0 || range <= 0f)
            {
                return null;
            }

            EnemyRuntime best = null;
            float bestPrimary = float.MaxValue;
            float bestSecondary = float.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || !enemy.IsTargetable)
                {
                    continue;
                }

                float distance = Vector2.Distance(attackerPosition, enemy.Position);
                if (distance > range)
                {
                    continue;
                }

                float primary = GetPrimaryScore(enemy, distance, priority);
                float secondary = distance;

                if (best == null
                    || primary < bestPrimary - ScoreEpsilon
                    || (Mathf.Abs(primary - bestPrimary) <= ScoreEpsilon && secondary < bestSecondary))
                {
                    best = enemy;
                    bestPrimary = primary;
                    bestSecondary = secondary;
                }
            }

            return best;
        }

        private static float GetPrimaryScore(EnemyRuntime enemy, float distance, TargetPriority priority)
        {
            switch (priority)
            {
                case TargetPriority.Nearest:
                    return distance;

                case TargetPriority.Fastest:
                    return -enemy.MoveSpeed;

                case TargetPriority.LowestHealth:
                    return enemy.CurrentHealth;

                case TargetPriority.BossFirst:
                    return IsBossOrElite(enemy) ? 0f : 1f;

                default:
                    return distance;
            }
        }

        private static bool IsBossOrElite(EnemyRuntime enemy)
        {
            return enemy.EnemyType == EnemyType.Boss || enemy.EnemyType == EnemyType.Elite;
        }
    }
}
