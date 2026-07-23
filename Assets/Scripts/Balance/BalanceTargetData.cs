using System;
using UnityEngine;

namespace LastTrain.Balance
{
    public enum BalanceSeverity
    {
        None = 0,
        Warning = 1,
        Critical = 2,
    }

    public static class BalanceMetricIds
    {
        public const string WinRate = "win_rate";
        public const string ReachStation5Rate = "reach_station_5_rate";
        public const string AvgSimulatedSeconds = "avg_simulated_seconds";
        public const string AvgRemainingHp = "avg_remaining_hp";
        public const string AvgRemainingCoins = "avg_remaining_coins";
        public const string PassengerPickRate = "passenger_pick_rate";
        public const string PassengerAvgDamage = "passenger_avg_damage";
        public const string AbilityPickRate = "ability_pick_rate";
        public const string SynergyActivationRate = "synergy_activation_rate";
        public const string EnemyTrainReachRate = "enemy_train_reach_rate";
        public const string StationFailRate = "station_fail_rate";
        public const string NoAdsClearRate = "no_ads_clear_rate";
        public const string BossPhaseFailRate = "boss_phase_fail_rate";
    }

    [Serializable]
    public sealed class BalanceMetricRange
    {
        public string metricId = BalanceMetricIds.WinRate;
        public string difficultyId = string.Empty;
        public string subjectId = string.Empty;
        public float minValue;
        public float maxValue = 1f;
        public float warningBand = 0.05f;
    }

    [CreateAssetMenu(fileName = "BalanceTargets", menuName = "LastTrain/Balance/Balance Target Data")]
    public sealed class BalanceTargetData : ScriptableObject
    {
        [SerializeField] private string id = "default_targets";
        [SerializeField] private BalanceMetricRange[] ranges = Array.Empty<BalanceMetricRange>();

        public string Id => id;
        public BalanceMetricRange[] Ranges => ranges ?? Array.Empty<BalanceMetricRange>();

        public static BalanceTargetData CreateDefaultRuntime()
        {
            var data = CreateInstance<BalanceTargetData>();
            data.id = "default_targets";
            data.ranges = new[]
            {
                Range(BalanceMetricIds.WinRate, Difficulty.DifficultyIds.Normal, 0.35f, 0.50f),
                Range(BalanceMetricIds.WinRate, Difficulty.DifficultyIds.Express, 0.20f, 0.35f),
                Range(BalanceMetricIds.WinRate, Difficulty.DifficultyIds.MidnightExpress, 0.10f, 0.25f),
                Range(BalanceMetricIds.WinRate, Difficulty.DifficultyIds.NonstopHell, 0.05f, 0.15f),
                Range(BalanceMetricIds.ReachStation5Rate, Difficulty.DifficultyIds.Normal, 0.70f, 1f),
                Range(BalanceMetricIds.ReachStation5Rate, Difficulty.DifficultyIds.Express, 0.55f, 1f),
                Range(BalanceMetricIds.ReachStation5Rate, Difficulty.DifficultyIds.MidnightExpress, 0.40f, 1f),
                Range(BalanceMetricIds.ReachStation5Rate, Difficulty.DifficultyIds.NonstopHell, 0.25f, 1f),
                Range(BalanceMetricIds.AvgRemainingHp, Difficulty.DifficultyIds.Normal, 25f, 50f),
                Range(BalanceMetricIds.AvgRemainingHp, Difficulty.DifficultyIds.Express, 15f, 40f),
                Range(BalanceMetricIds.AvgRemainingHp, Difficulty.DifficultyIds.MidnightExpress, 10f, 30f),
                Range(BalanceMetricIds.AvgRemainingHp, Difficulty.DifficultyIds.NonstopHell, 5f, 25f),
                Range(BalanceMetricIds.PassengerPickRate, string.Empty, 0.10f, 0.70f),
                Range(BalanceMetricIds.AbilityPickRate, string.Empty, 0f, 0.80f),
                Range(BalanceMetricIds.EnemyTrainReachRate, string.Empty, 0f, 0.35f),
            };
            return data;
        }

        private static BalanceMetricRange Range(string metricId, string difficultyId, float min, float max)
        {
            return new BalanceMetricRange
            {
                metricId = metricId,
                difficultyId = difficultyId ?? string.Empty,
                minValue = min,
                maxValue = max,
                warningBand = Math.Abs(max - min) * 0.1f,
            };
        }
    }
}
