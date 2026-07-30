using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>전투 좌표계 상수. PassengerData.range(게임 단위)를 Canvas 월드 거리로 변환한다.</summary>
    public static class BattleConstants
    {
        /// <summary>range 1 = Canvas 기준(스케일 1) 거리 70 (1080×1920 레퍼런스).</summary>
        public const float RangeToWorldScale = 70f;

        /// <summary>moveSpeed 1 = Canvas 월드 거리 34px/s (V자 경로에서 충분한 대응 시간).</summary>
        public const float MoveSpeedToWorldScale = 34f;

        /// <summary>투사체 기본 이동 속도(px/s).</summary>
        public const float ProjectileSpeed = 600f;

        /// <summary>스폰 후 이 거리만큼 이동하기 전에는 공격 대상으로 잡히지 않는다.</summary>
        public const float SpawnTargetProtectionDistance = 120f;

        public static readonly UnityEngine.Vector2 SpawnAnchoredPosition = new(40f, 680f);
        public static readonly UnityEngine.Vector2[] EnemyWaypointAnchoredPositions =
        {
            new(440f, 680f),
            new(440f, 150f),
        };
        public static readonly UnityEngine.Vector2 TrainTargetAnchoredPosition = new(360f, 100f);
        public static readonly UnityEngine.Vector2 EnemyLaneTopAnchoredPosition = new(240f, 680f);
        public static readonly UnityEngine.Vector2 EnemyLaneTopSize = new(820f, 100f);
        public static readonly UnityEngine.Vector2 EnemyLaneRightAnchoredPosition = new(440f, 415f);
        public static readonly UnityEngine.Vector2 EnemyLaneRightSize = new(100f, 530f);
        public static readonly UnityEngine.Vector2 GridAnchoredPosition = new(0f, 305f);
        public static readonly UnityEngine.Vector2 GridSize = new(820f, 710f);
        public static readonly UnityEngine.Vector2 GridCellSize = new(220f, 220f);
        public static readonly UnityEngine.Vector2 GridSpacing = new(12f, 12f);

        public static float ToWorldRange(float dataRange)
        {
            return dataRange * RangeToWorldScale;
        }

        public static float ToWorldMoveSpeed(float dataMoveSpeed)
        {
            return dataMoveSpeed * MoveSpeedToWorldScale;
        }

        /// <summary>
        /// Screen Space Overlay CanvasScaler에 맞춘 월드 스케일.
        /// 슬롯/적 위치(.position)는 이 값에 비례하므로 사거리·이동속도에도 곱해야
        /// 에디터 Game 뷰와 실기기 체감 사거리가 맞는다.
        /// </summary>
        public static float GetUiWorldScale(Canvas canvas)
        {
            if (canvas == null)
            {
                return 1f;
            }

            Transform root = canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;
            float scale = root.lossyScale.x;
            return scale > 0.0001f ? scale : 1f;
        }

        public static float GetEnemyPathLength()
        {
            var points = new UnityEngine.Vector2[EnemyWaypointAnchoredPositions.Length + 2];
            points[0] = SpawnAnchoredPosition;
            for (int i = 0; i < EnemyWaypointAnchoredPositions.Length; i++)
            {
                points[i + 1] = EnemyWaypointAnchoredPositions[i];
            }

            points[^1] = TrainTargetAnchoredPosition;

            float length = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                length += UnityEngine.Vector2.Distance(points[i], points[i + 1]);
            }

            return length;
        }
    }
}
