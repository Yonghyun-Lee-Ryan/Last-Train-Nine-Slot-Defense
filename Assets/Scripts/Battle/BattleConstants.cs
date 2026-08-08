using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>
    /// 전투 좌표계 상수.
    /// PassengerData.range / moveSpeed는 SafeArea 로컬(1080×1920 설계 단위)로 변환한다.
    /// </summary>
    public static class BattleConstants
    {
        /// <summary>
        /// range 1 = SafeArea 로컬 거리.
        /// 야근 직장인(range 6)이 두 번째 칸(상단 중앙)에서 상단 지그재그 레인을 때리도록 맞춤.
        /// 헬스 트레이너(range 2.5)는 앞줄에서 하단 레인·객차 접근 구간을 담당.
        /// </summary>
        public const float RangeToWorldScale = 150f;

        /// <summary>지그재그 경로 길이에서도 기존 V자 경로와 비슷한 도달 시간을 유지.</summary>
        public const float MoveSpeedToWorldScale = 116f;

        /// <summary>투사체 기본 이동 속도(로컬 단위/s).</summary>
        public const float ProjectileSpeed = 600f;

        /// <summary>스폰 후 이 로컬 거리만큼 이동하기 전에는 공격 대상으로 잡히지 않는다.</summary>
        public const float SpawnTargetProtectionDistance = 120f;

        public const float EnemyPathLeftX = -380f;
        public const float EnemyPathRightX = 380f;

        public static readonly float[] EnemyPathLaneYs =
        {
            620f,
            460f,
            300f,
            140f,
        };

        public static readonly Vector2 SpawnAnchoredPosition = new(EnemyPathRightX, EnemyPathLaneYs[0]);

        public static readonly Vector2[] EnemyWaypointAnchoredPositions =
        {
            new(EnemyPathLeftX, EnemyPathLaneYs[0]),
            new(EnemyPathLeftX, EnemyPathLaneYs[1]),
            new(EnemyPathRightX, EnemyPathLaneYs[1]),
            new(EnemyPathRightX, EnemyPathLaneYs[2]),
            new(EnemyPathLeftX, EnemyPathLaneYs[2]),
            new(EnemyPathLeftX, EnemyPathLaneYs[3]),
        };

        public static readonly Vector2 TrainTargetAnchoredPosition = new(360f, 100f);

        public static readonly LaneDecorSpec[] EnemyLaneDecors =
        {
            new("SpawnLaneDecor0", new Vector2(0f, 620f), new Vector2(820f, 72f)),
            new("SpawnLaneDecor1", new Vector2(0f, 460f), new Vector2(820f, 72f)),
            new("SpawnLaneDecor2", new Vector2(0f, 300f), new Vector2(820f, 72f)),
            new("SpawnLaneDecor3", new Vector2(0f, 140f), new Vector2(820f, 72f)),
            new("SpawnLaneDecorJoin0", new Vector2(EnemyPathLeftX, 540f), new Vector2(72f, 176f)),
            new("SpawnLaneDecorJoin1", new Vector2(EnemyPathRightX, 380f), new Vector2(72f, 176f)),
            new("SpawnLaneDecorJoin2", new Vector2(EnemyPathLeftX, 220f), new Vector2(72f, 176f)),
        };

        public static readonly string[] LegacyEnemyLaneDecorNames =
        {
            "SpawnLaneDecorTop",
            "SpawnLaneDecorRight",
        };

        public static readonly Vector2 GridAnchoredPosition = new(0f, 305f);
        public static readonly Vector2 GridSize = new(820f, 710f);
        public static readonly Vector2 GridCellSize = new(220f, 220f);
        public static readonly Vector2 GridSpacing = new(12f, 12f);

        public static float ToWorldRange(float dataRange)
        {
            return dataRange * RangeToWorldScale;
        }

        public static float ToWorldMoveSpeed(float dataMoveSpeed)
        {
            return dataMoveSpeed * MoveSpeedToWorldScale;
        }

        public static Vector2[] GetEnemyPathPoints()
        {
            var points = new Vector2[EnemyWaypointAnchoredPositions.Length + 2];
            points[0] = SpawnAnchoredPosition;
            for (int i = 0; i < EnemyWaypointAnchoredPositions.Length; i++)
            {
                points[i + 1] = EnemyWaypointAnchoredPositions[i];
            }

            points[^1] = TrainTargetAnchoredPosition;
            return points;
        }

        public static float GetEnemyPathLength()
        {
            Vector2[] points = GetEnemyPathPoints();
            float length = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                length += Vector2.Distance(points[i], points[i + 1]);
            }

            return length;
        }

        public readonly struct LaneDecorSpec
        {
            public readonly string Name;
            public readonly Vector2 AnchoredPosition;
            public readonly Vector2 Size;

            public LaneDecorSpec(string name, Vector2 anchoredPosition, Vector2 size)
            {
                Name = name;
                AnchoredPosition = anchoredPosition;
                Size = size;
            }
        }
    }
}
