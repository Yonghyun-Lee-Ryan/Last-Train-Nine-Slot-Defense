using System.Collections.Generic;
using LastTrain.Simulation;

namespace LastTrain.Balance
{
    /// <summary>Simulator Aggregate → 표준 BalanceReport.</summary>
    public static class BalanceReportBuilder
    {
        public static BalanceReport FromAggregate(BattleSimulationAggregate aggregate, string versionLabel = "sim")
        {
            var report = new BalanceReport
            {
                Source = "simulation",
                VersionLabel = versionLabel ?? "sim",
                DifficultyId = aggregate?.DifficultyId ?? string.Empty,
                SampleCount = aggregate?.Iterations ?? 0,
            };

            if (aggregate == null)
            {
                return report;
            }

            string diff = aggregate.DifficultyId ?? string.Empty;
            report.AddMetric(BalanceMetricIds.WinRate, aggregate.WinRate, diff);
            report.AddMetric(BalanceMetricIds.ReachStation5Rate, aggregate.ReachStation5Rate, diff);
            report.AddMetric(BalanceMetricIds.AvgSimulatedSeconds, aggregate.AvgSimulatedSeconds, diff);
            report.AddMetric(BalanceMetricIds.AvgRemainingHp, aggregate.AvgRemainingHp, diff);
            report.AddMetric(BalanceMetricIds.AvgRemainingCoins, aggregate.AvgRemainingCoins, diff);

            foreach (KeyValuePair<string, float> pair in aggregate.PassengerPickRate)
            {
                report.PassengerPickRate[pair.Key] = pair.Value;
                report.AddMetric(BalanceMetricIds.PassengerPickRate, pair.Value, diff, pair.Key, pair.Key);
            }

            foreach (KeyValuePair<string, float> pair in aggregate.AvgDamageByPassengerId)
            {
                report.PassengerDamage[pair.Key] = pair.Value;
                report.AddMetric(BalanceMetricIds.PassengerAvgDamage, pair.Value, diff, pair.Key, pair.Key);
            }

            foreach (KeyValuePair<string, float> pair in aggregate.AbilityPickRate)
            {
                report.AddMetric(BalanceMetricIds.AbilityPickRate, pair.Value, diff, pair.Key, pair.Key);
            }

            foreach (KeyValuePair<string, float> pair in aggregate.SynergyActivationRate)
            {
                report.AddMetric(BalanceMetricIds.SynergyActivationRate, pair.Value, diff, pair.Key, pair.Key);
            }

            foreach (KeyValuePair<string, float> pair in aggregate.AvgTrainReachesByEnemyId)
            {
                report.AddMetric(BalanceMetricIds.EnemyTrainReachRate, pair.Value, diff, pair.Key, pair.Key);
            }

            foreach (KeyValuePair<int, float> pair in aggregate.FailRateByStationIndex)
            {
                report.AddMetric(
                    BalanceMetricIds.StationFailRate,
                    pair.Value,
                    diff,
                    pair.Key.ToString(),
                    $"station_{pair.Key}");
            }

            foreach (KeyValuePair<int, float> pair in aggregate.SurvivalCurveByStation)
            {
                report.SurvivalCurveByStation[pair.Key] = pair.Value;
            }

            return report;
        }
    }
}
