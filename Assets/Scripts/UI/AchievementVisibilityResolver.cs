using LastTrain.Save;

namespace LastTrain.UI
{
    public readonly struct AchievementEntryView
    {
        public AchievementEntryView(string id, bool isUnlocked, string title, string detail)
        {
            Id = id ?? string.Empty;
            IsUnlocked = isUnlocked;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Id { get; }
        public bool IsUnlocked { get; }
        public string Title { get; }
        public string Detail { get; }
    }

    /// <summary>업적 목록 표시용 잠금/해금 라벨을 Meta 저장과 카탈로그에서 조합한다.</summary>
    public static class AchievementVisibilityResolver
    {
        public const string LockedTitle = "???";
        public const string LockedDetail = "아직 해금하지 못했습니다.";

        public static AchievementEntryView BuildEntry(MetaSaveData meta, string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                return new AchievementEntryView(string.Empty, false, LockedTitle, LockedDetail);
            }

            bool unlocked = MetaProgressionService.IsAchievementUnlocked(meta, achievementId);
            AchievementCatalog.TryGetDisplay(achievementId, out string displayName, out string description);
            string title = unlocked
                ? (string.IsNullOrWhiteSpace(displayName) ? achievementId : displayName)
                : LockedTitle;
            string detail = unlocked
                ? (string.IsNullOrWhiteSpace(description) ? string.Empty : description)
                : LockedDetail;
            return new AchievementEntryView(achievementId, unlocked, title, detail);
        }
    }
}
