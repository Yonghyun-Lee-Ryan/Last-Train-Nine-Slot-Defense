using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>Remote Config 실패 시 사용하는 ScriptableObject 기본값.</summary>
    [CreateAssetMenu(fileName = "RemoteConfigDefaults", menuName = "Last Train/Integration/Remote Config Defaults")]
    public sealed class RemoteConfigDefaults : ScriptableObject
    {
        [Header("Ads")]
        [SerializeField] private int interstitialIntervalSeconds = 180;
        [SerializeField] private int rewardedDailyLimit = 20;
        [SerializeField] private int runsBeforeInterstitial = 1;

        [Header("Economy")]
        [SerializeField] private int baseSummonCost = 10;
        [SerializeField] private int summonCostIncrease = 2;
        [SerializeField] private float resultRewardMultiplier = 1f;
        [SerializeField] private int freeRevivePerRun = 1;

        [Header("Events")]
        [SerializeField] private bool liveEventEnabled;

        [Header("Quick Run")]
        [SerializeField] private float quickRunRewardMultiplier = 1f;

        [Header("LiveOps Remote")]
        [SerializeField] private bool liveOpsUseRemoteCatalog;
        [SerializeField] private string liveOpsCatalogJson = string.Empty;
        [SerializeField] private string liveEventServerUtc = string.Empty;

        public int InterstitialIntervalSeconds => Mathf.Max(30, interstitialIntervalSeconds);
        public int RewardedDailyLimit => Mathf.Max(0, rewardedDailyLimit);
        public int RunsBeforeInterstitial => Mathf.Max(0, runsBeforeInterstitial);
        public int BaseSummonCost => Mathf.Max(0, baseSummonCost);
        public int SummonCostIncrease => Mathf.Max(0, summonCostIncrease);
        public float ResultRewardMultiplier => Mathf.Max(0f, resultRewardMultiplier);
        public int FreeRevivePerRun => Mathf.Max(0, freeRevivePerRun);
        public bool LiveEventEnabled => liveEventEnabled;
        public float QuickRunRewardMultiplier => Mathf.Max(0.01f, quickRunRewardMultiplier);
        public bool LiveOpsUseRemoteCatalog => liveOpsUseRemoteCatalog;
        public string LiveOpsCatalogJson => liveOpsCatalogJson ?? string.Empty;
        public string LiveEventServerUtc => liveEventServerUtc ?? string.Empty;

        public RemoteConfigSnapshot ToSnapshot()
        {
            return new RemoteConfigSnapshot(
                InterstitialIntervalSeconds,
                RewardedDailyLimit,
                RunsBeforeInterstitial,
                BaseSummonCost,
                SummonCostIncrease,
                ResultRewardMultiplier,
                FreeRevivePerRun,
                LiveEventEnabled,
                loadedFromRemote: false,
                QuickRunRewardMultiplier,
                LiveOpsUseRemoteCatalog,
                LiveOpsCatalogJson,
                LiveEventServerUtc);
        }
    }
}
