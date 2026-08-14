namespace LastTrain.Save
{
    /// <summary>기존 Meta achievement id의 표시 이름·설명. 해금 로직은 MetaProgressionService에 둔다.</summary>
    public static class AchievementCatalog
    {
        public static readonly string[] AllIds =
        {
            MetaProgressionDefaults.AchFirstVictory,
            MetaProgressionDefaults.AchFirstBossKill,
            MetaProgressionDefaults.AchKill10,
            MetaProgressionDefaults.AchReachStation3,
            MetaProgressionDefaults.AchVictoryFullHp,
        };

        public static bool TryGetDisplay(string achievementId, out string displayName, out string description)
        {
            displayName = string.Empty;
            description = string.Empty;
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                return false;
            }

            switch (achievementId)
            {
                case MetaProgressionDefaults.AchFirstVictory:
                    displayName = "첫 도착";
                    description = "종착역에 한 번 도착한다.";
                    return true;
                case MetaProgressionDefaults.AchFirstBossKill:
                    displayName = "첫 보스 처치";
                    description = "보스를 한 번 처치한다.";
                    return true;
                case MetaProgressionDefaults.AchKill10:
                    displayName = "처치 10";
                    description = "한 회차에서 적을 10기 이상 처치한다.";
                    return true;
                case MetaProgressionDefaults.AchReachStation3:
                    displayName = "3역 도달";
                    description = "한 회차에서 역 인덱스 3 이상에 도달한다.";
                    return true;
                case MetaProgressionDefaults.AchVictoryFullHp:
                    displayName = "만신 도착";
                    description = "객차 내구도를 모두 남기고 도착한다.";
                    return true;
                default:
                    return false;
            }
        }

        public static string GetDisplayNameOrId(string achievementId)
        {
            return TryGetDisplay(achievementId, out string name, out _) ? name : achievementId ?? string.Empty;
        }
    }
}
