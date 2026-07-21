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

        /// <summary>
        /// 적을 스폰 방향으로 밀어 이동 경로를 되돌린다.
        /// 스폰을 지나치지 않도록 클램프한다.
        /// </summary>
        public static void ApplyKnockback(
            EnemyRuntime enemy,
            Vector2 spawnPoint,
            Vector2 trainTarget,
            float distance)
        {
            if (enemy == null || !enemy.IsAlive || distance <= 0f)
            {
                return;
            }

            Vector2 segmentStart = enemy.HasRouteSegment ? enemy.RouteSegmentStart : spawnPoint;
            Vector2 segmentEnd = enemy.HasRouteSegment ? enemy.RouteSegmentEnd : trainTarget;
            Vector2 awayFromTrain = segmentStart - segmentEnd;
            if (awayFromTrain.sqrMagnitude < 0.0001f)
            {
                awayFromTrain = segmentStart - enemy.Position;
            }

            if (awayFromTrain.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector2 direction = awayFromTrain.normalized;
            Vector2 next = enemy.Position + direction * distance;

            // 현재 경로 구간의 시작점을 지나치지 않도록 선분 위에 투영·클램프
            Vector2 toSpawn = segmentStart - segmentEnd;
            float pathLengthSq = toSpawn.sqrMagnitude;
            if (pathLengthSq > 0.0001f)
            {
                float t = Vector2.Dot(next - segmentEnd, toSpawn) / pathLengthSq;
                t = Mathf.Clamp01(t);
                next = segmentEnd + toSpawn * t;
            }

            enemy.Position = next;
        }
    }
}
