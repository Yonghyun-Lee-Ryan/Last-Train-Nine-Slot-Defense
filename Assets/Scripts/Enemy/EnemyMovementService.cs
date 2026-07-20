using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>적 이동 순수 로직. EditMode 테스트 가능.</summary>
    public static class EnemyMovementService
    {
        /// <summary>
        /// 적을 목표 지점으로 이동시킨다.
        /// </summary>
        /// <returns>목표 도달 반경 이내면 true.</returns>
        public static bool TickMove(
            EnemyRuntime enemy,
            Vector2 targetPosition,
            float deltaTime,
            float speedWorldScale,
            float reachRadius)
        {
            if (enemy == null || !enemy.IsAlive || speedWorldScale <= 0f || deltaTime <= 0f)
            {
                return false;
            }

            float speed = enemy.MoveSpeed * speedWorldScale;
            Vector2 current = enemy.Position;
            Vector2 next = Vector2.MoveTowards(current, targetPosition, speed * deltaTime);
            enemy.Position = next;

            return Vector2.Distance(next, targetPosition) <= reachRadius;
        }
    }
}
