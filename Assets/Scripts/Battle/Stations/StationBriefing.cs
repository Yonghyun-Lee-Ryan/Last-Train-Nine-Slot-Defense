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
    }
}
