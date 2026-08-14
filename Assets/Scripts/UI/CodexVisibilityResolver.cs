using LastTrain.Data;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.UI
{
    public enum CodexCategory
    {
        Passenger,
        Enemy,
        Boss,
        Relic,
    }

    public readonly struct CodexEntryView
    {
        public CodexEntryView(string id, bool isDiscovered, string title, string detail, Sprite portrait)
        {
            Id = id ?? string.Empty;
            IsDiscovered = isDiscovered;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            Portrait = portrait;
        }

        public string Id { get; }
        public bool IsDiscovered { get; }
        public string Title { get; }
        public string Detail { get; }
        public Sprite Portrait { get; }
    }

    /// <summary>도감 표시용 잠금/해금 라벨·설명을 Meta 저장과 GameDatabase에서 조합한다.</summary>
    public static class CodexVisibilityResolver
    {
        public const string LockedTitle = "???";
        public const string LockedDetail = "아직 발견하지 못했습니다.";

        public static bool IsDiscovered(MetaSaveData meta, CodexCategory category, string id)
        {
            return category switch
            {
                CodexCategory.Passenger => MetaProgressionService.IsPassengerDiscovered(meta, id),
                CodexCategory.Enemy => MetaProgressionService.IsEnemyDiscovered(meta, id),
                CodexCategory.Boss => MetaProgressionService.IsBossDiscovered(meta, id),
                CodexCategory.Relic => MetaProgressionService.IsRelicDiscovered(meta, id),
                _ => false,
            };
        }

        public static CodexEntryView BuildPassengerEntry(
            MetaSaveData meta,
            PassengerData data,
            VisualDatabase visuals)
        {
            if (data == null)
            {
                return new CodexEntryView(string.Empty, false, LockedTitle, LockedDetail, null);
            }

            bool discovered = MetaProgressionService.IsPassengerDiscovered(meta, data.Id);
            string title = discovered ? data.DisplayName : LockedTitle;
            string detail = discovered ? BuildPassengerDetail(meta, data) : LockedDetail;
            Sprite portrait = ResolvePassengerPortrait(discovered, data.Id, visuals);
            return new CodexEntryView(data.Id, discovered, title, detail, portrait);
        }

        public static CodexEntryView BuildEnemyEntry(
            MetaSaveData meta,
            EnemyData data,
            VisualDatabase visuals,
            CodexCategory category)
        {
            if (data == null)
            {
                return new CodexEntryView(string.Empty, false, LockedTitle, LockedDetail, null);
            }

            bool discovered = IsDiscovered(meta, category, data.Id);
            string title = discovered ? data.DisplayName : LockedTitle;
            string detail = discovered ? BuildEnemyDetail(data, category) : LockedDetail;
            Sprite portrait = ResolveEnemyPortrait(discovered, data.Id, visuals);
            return new CodexEntryView(data.Id, discovered, title, detail, portrait);
        }

        public static CodexEntryView BuildRelicEntry(MetaSaveData meta, RelicData data)
        {
            if (data == null)
            {
                return new CodexEntryView(string.Empty, false, LockedTitle, LockedDetail, null);
            }

            bool discovered = MetaProgressionService.IsRelicDiscovered(meta, data.Id);
            string title = discovered ? data.DisplayName : LockedTitle;
            string detail = discovered ? data.Description : LockedDetail;
            return new CodexEntryView(data.Id, discovered, title, detail, null);
        }

        private static string BuildPassengerDetail(MetaSaveData meta, PassengerData data)
        {
            string role = FormatRole(data.Role);
            if (!MetaProgressionService.TryGetPassengerMastery(meta, data.Id, out MetaPassengerMasteryEntry mastery))
            {
                return $"역할: {role}";
            }

            return
                $"역할: {role}\n"
                + $"Lv.{Mathf.Max(1, mastery.highestStar)}"
                + $" · 사용 {Mathf.Max(0, mastery.useCount)}"
                + $" · 보스 {Mathf.Max(0, mastery.bossKillParticipations)}";
        }

        public static string FormatRole(PassengerRole role)
        {
            return role switch
            {
                PassengerRole.Attack => "공격",
                PassengerRole.Defense => "방어",
                PassengerRole.Support => "지원",
                PassengerRole.Summon => "소환",
                PassengerRole.Special => "특수",
                _ => "역할",
            };
        }

        private static string BuildEnemyDetail(EnemyData data, CodexCategory category)
        {
            string prefix = category == CodexCategory.Boss ? "보스 · " : string.Empty;
            return $"{prefix}체력 {data.BaseHealth:0} · 이동 {data.MoveSpeed:0.0} · 기관차 피해 {data.TrainDamage:0}";
        }

        private static Sprite ResolvePassengerPortrait(bool discovered, string id, VisualDatabase visuals)
        {
            if (!discovered || visuals == null || !visuals.TryGetPassengerVisual(id, out PassengerVisualSet set))
            {
                return null;
            }

            return set.GetPortraitOrFallback();
        }

        private static Sprite ResolveEnemyPortrait(bool discovered, string id, VisualDatabase visuals)
        {
            if (!discovered || visuals == null || !visuals.TryGetEnemyVisual(id, out EnemyVisualSet set))
            {
                return null;
            }

            return set.GetMoveOrFallback();
        }
    }
}
