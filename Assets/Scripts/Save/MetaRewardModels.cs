using System;
using System.Collections.Generic;

namespace LastTrain.Save
{
    /// <summary>회차 종료 시 승차권 조각 계산 내역.</summary>
    public sealed class MetaRewardBreakdown
    {
        public int StationTickets { get; set; }
        public int KillTickets { get; set; }
        public int BossTickets { get; set; }
        public int RemainingHpTickets { get; set; }
        public int DiscoveryTickets { get; set; }
        public int AchievementTickets { get; set; }

        public int TotalTickets =>
            StationTickets
            + KillTickets
            + BossTickets
            + RemainingHpTickets
            + DiscoveryTickets
            + AchievementTickets;

        public List<string> NewPassengerDiscoveries { get; } = new();
        public List<string> NewEnemyDiscoveries { get; } = new();
        public List<string> NewBossDiscoveries { get; } = new();
        public List<string> NewlyUnlockedAchievements { get; } = new();
        public List<string> NewlyUnlockedPassengers { get; } = new();
    }

    /// <summary>메타 보상 적용 결과.</summary>
    public sealed class MetaApplyResult
    {
        public bool Applied { get; set; }
        public bool WasDuplicate { get; set; }
        public string RunId { get; set; } = string.Empty;
        public MetaRewardBreakdown Breakdown { get; set; }
        public int TicketFragmentsAfter { get; set; }
        public int AccountLevelAfter { get; set; }
        public int AccountXpAfter { get; set; }
    }

    /// <summary>메인 메뉴 표시용 메타 스냅샷.</summary>
    public sealed class MetaProgressSnapshot
    {
        public int TicketFragments { get; set; }
        public int AccountLevel { get; set; }
        public int AccountXp { get; set; }
        public int UnlockedPassengerCount { get; set; }
        public int DiscoveredPassengerCount { get; set; }
        public int DiscoveredEnemyCount { get; set; }
        public int DiscoveredBossCount { get; set; }
        public IReadOnlyList<string> PendingNewDiscoveryIds { get; set; } = Array.Empty<string>();

        /// <summary>해금 진행률(0~1). totalPassengerCount가 0이면 0.</summary>
        public float GetUnlockProgress01(int totalPassengerCount)
        {
            if (totalPassengerCount <= 0)
            {
                return 0f;
            }

            return Math.Min(1f, UnlockedPassengerCount / (float)totalPassengerCount);
        }
    }
}
