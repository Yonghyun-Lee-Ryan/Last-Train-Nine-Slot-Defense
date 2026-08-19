using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>전투 HUD 하단 버튼 간격. 소환 패널과 겹치지 않게 둔다.</summary>
    public static class BattleHudLayout
    {
        public const float ActionButtonHeight = 78f;
        public const float UndoOffsetXFromReady = -180f;
        public const float SummonButtonY = 45f;
        public const float SummonButtonHeight = 80f;
        public const float SummonButtonWidth = 240f;
        public const float SpeedButtonY = 220f;
        public const float SpeedButtonHeight = 78f;
        public const float FreeSummonAdX = 270f;
        public const float FreeSummonAdY = 45f;
        public const float FreeSummonAdHeight = 72f;
        public const float FreeSummonAdWidth = 200f;

        public static Vector2 UndoMergeAnchoredPosition(Vector2 readyAnchoredPosition)
        {
            return new Vector2(readyAnchoredPosition.x + UndoOffsetXFromReady, readyAnchoredPosition.y);
        }

        public static bool OverlapsVertically(float bottomA, float heightA, float bottomB, float heightB)
        {
            float topA = bottomA + heightA;
            float topB = bottomB + heightB;
            return bottomA < topB && bottomB < topA;
        }
    }
}
