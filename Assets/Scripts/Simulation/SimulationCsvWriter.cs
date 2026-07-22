using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LastTrain.Simulation
{
    /// <summary>시뮬 집계 결과를 CSV로 저장한다.</summary>
    public static class SimulationCsvWriter
    {
        public static string Write(BattleSimulationAggregate aggregate, string directory, string fileName = null)
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException(nameof(aggregate));
            }

            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.GetTempPath();
            }

            Directory.CreateDirectory(directory);
            fileName ??= $"battle_sim_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = Path.Combine(directory, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("metric,value");
            Append(sb, "iterations", aggregate.Iterations);
            Append(sb, "wins", aggregate.Wins);
            Append(sb, "win_rate", aggregate.WinRate);
            Append(sb, "avg_remaining_hp", aggregate.AvgRemainingHp);
            Append(sb, "min_remaining_hp", aggregate.MinRemainingHp);
            Append(sb, "max_remaining_hp", aggregate.MaxRemainingHp);
            Append(sb, "stddev_remaining_hp", aggregate.StdDevRemainingHp);
            Append(sb, "avg_simulated_seconds", aggregate.AvgSimulatedSeconds);
            Append(sb, "min_simulated_seconds", aggregate.MinSimulatedSeconds);
            Append(sb, "max_simulated_seconds", aggregate.MaxSimulatedSeconds);

            foreach (KeyValuePair<string, float> pair in aggregate.AvgDamageByPassengerId)
            {
                Append(sb, "avg_damage_" + pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, float> pair in aggregate.AvgSkillTicksByPassengerId)
            {
                Append(sb, "avg_attacks_" + pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, float> pair in aggregate.AvgTrainReachesByEnemyId)
            {
                Append(sb, "avg_train_reach_" + pair.Key, pair.Value);
            }

            sb.AppendLine();
            sb.AppendLine("run_index,seed,victory,remaining_hp,train_max_hp,simulated_seconds,enemies_killed,bosses_killed");
            for (int i = 0; i < aggregate.Runs.Count; i++)
            {
                BattleSimulationRunResult run = aggregate.Runs[i];
                sb.Append(i).Append(',')
                    .Append(run.Seed).Append(',')
                    .Append(run.IsVictory ? 1 : 0).Append(',')
                    .Append(run.RemainingTrainHp).Append(',')
                    .Append(run.TrainMaxHp).Append(',')
                    .Append(F(run.SimulatedSeconds)).Append(',')
                    .Append(run.EnemiesKilled).Append(',')
                    .Append(run.BossesKilled)
                    .AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static void Append(StringBuilder sb, string key, float value)
        {
            sb.Append(key).Append(',').Append(F(value)).AppendLine();
        }

        private static void Append(StringBuilder sb, string key, int value)
        {
            sb.Append(key).Append(',').Append(value).AppendLine();
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
