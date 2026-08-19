namespace LastTrain.Integrations
{
    /// <summary>런타임에 적용되는 원격/로컬 설정 스냅샷.</summary>
    public sealed class RemoteConfigSnapshot
    {
        public static RemoteConfigSnapshot Default { get; } = new RemoteConfigSnapshot(
            interstitialIntervalSeconds: 180,
            rewardedDailyLimit: 20,
            runsBeforeInterstitial: 3,
            baseSummonCost: 10,
            summonCostIncrease: 2,
            resultRewardMultiplier: 1f,
            freeRevivePerRun: 1,
            liveEventEnabled: false,
            loadedFromRemote: false,
            quickRunRewardMultiplier: 1f);

        public RemoteConfigSnapshot(
            int interstitialIntervalSeconds,
            int rewardedDailyLimit,
            int runsBeforeInterstitial,
            int baseSummonCost,
            int summonCostIncrease,
            float resultRewardMultiplier,
            int freeRevivePerRun,
            bool liveEventEnabled,
            bool loadedFromRemote,
            float quickRunRewardMultiplier = 1f,
            bool liveOpsUseRemoteCatalog = false,
            string liveOpsCatalogJson = "",
            string liveEventServerUtc = "")
        {
            InterstitialIntervalSeconds = interstitialIntervalSeconds;
            RewardedDailyLimit = rewardedDailyLimit;
            RunsBeforeInterstitial = runsBeforeInterstitial;
            BaseSummonCost = baseSummonCost;
            SummonCostIncrease = summonCostIncrease;
            ResultRewardMultiplier = resultRewardMultiplier;
            FreeRevivePerRun = freeRevivePerRun;
            LiveEventEnabled = liveEventEnabled;
            LoadedFromRemote = loadedFromRemote;
            QuickRunRewardMultiplier = quickRunRewardMultiplier > 0.01f ? quickRunRewardMultiplier : 1f;
            LiveOpsUseRemoteCatalog = liveOpsUseRemoteCatalog;
            LiveOpsCatalogJson = liveOpsCatalogJson ?? string.Empty;
            LiveEventServerUtc = liveEventServerUtc ?? string.Empty;
        }

        public int InterstitialIntervalSeconds { get; }
        public int RewardedDailyLimit { get; }
        public int RunsBeforeInterstitial { get; }
        public int BaseSummonCost { get; }
        public int SummonCostIncrease { get; }
        public float ResultRewardMultiplier { get; }
        public int FreeRevivePerRun { get; }
        public bool LiveEventEnabled { get; }
        public bool LoadedFromRemote { get; }
        public float QuickRunRewardMultiplier { get; }
        public bool LiveOpsUseRemoteCatalog { get; }
        public string LiveOpsCatalogJson { get; }
        public string LiveEventServerUtc { get; }
    }
}
