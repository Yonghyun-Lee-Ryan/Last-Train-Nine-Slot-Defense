using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Battle
{
    /// <summary>전투 시작 전 플레이어에게 보여줄 역 정보.</summary>
    public sealed class StationBriefing
    {
        public string StationName { get; set; } = string.Empty;
        public string StationTypeLabel { get; set; } = string.Empty;
        public string EnemyTags { get; set; } = string.Empty;
        public bool HasFastEnemy { get; set; }
        public bool HasTankEnemy { get; set; }
        public bool HasEliteEnemy { get; set; }
        public string BossPatternHint { get; set; } = string.Empty;
        public int RewardCoins { get; set; }
        public string ModifierHint { get; set; } = string.Empty;

        public string BuildDisplayText()
        {
            var lines = new System.Text.StringBuilder();
            lines.Append(StationName);
            if (!string.IsNullOrWhiteSpace(StationTypeLabel))
            {
                lines.Append(" (").Append(StationTypeLabel).Append(')');
            }

            lines.Append('\n');
            if (!string.IsNullOrWhiteSpace(EnemyTags))
            {
                lines.Append("적 태그: ").Append(EnemyTags).Append('\n');
            }

            if (HasFastEnemy)
            {
                lines.Append("• 빠른 적 등장\n");
            }

            if (HasTankEnemy)
            {
                lines.Append("• 방어형 적 등장\n");
            }

            if (HasEliteEnemy)
            {
                lines.Append("• 정예 적 등장\n");
            }

            if (!string.IsNullOrWhiteSpace(BossPatternHint))
            {
                lines.Append("보스 힌트: ").Append(BossPatternHint).Append('\n');
            }

            lines.Append("완료 보상: ").Append(RewardCoins).Append(" 코인");
            if (!string.IsNullOrWhiteSpace(ModifierHint))
            {
                lines.Append('\n').Append(ModifierHint);
            }

            return lines.ToString().TrimEnd();
        }

        /// <summary>텍스트 태그 대신 HUD 아이콘으로 쓸 위협 유형. 등장 순서는 빠른→방어→정예→보스.</summary>
        public IReadOnlyList<EnemyType> CollectThreatTypes()
        {
            var types = new List<EnemyType>(4);
            if (HasFastEnemy)
            {
                types.Add(EnemyType.Fast);
            }

            if (HasTankEnemy)
            {
                types.Add(EnemyType.Tank);
            }

            if (HasEliteEnemy)
            {
                types.Add(EnemyType.Elite);
            }

            if (!string.IsNullOrWhiteSpace(BossPatternHint))
            {
                types.Add(EnemyType.Boss);
            }

            return types;
        }
    }
}
