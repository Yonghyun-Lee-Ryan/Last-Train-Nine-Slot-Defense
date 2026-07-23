using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LastTrain.Balance
{
    public static class BalanceAnalyticsCsvImporter
    {
        /// <summary>
        /// Analytics CSV (metric_id,difficulty_id,subject_id,value) → BalanceReport.
        /// </summary>
        public static BalanceReport FromCsv(string csvText, string versionLabel = "analytics")
        {
            var report = new BalanceReport
            {
                Source = "analytics_csv",
                VersionLabel = versionLabel ?? "analytics",
            };

            if (string.IsNullOrWhiteSpace(csvText))
            {
                return report;
            }

            string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int start = 0;
            if (lines.Length > 0 && lines[0].IndexOf("metric_id", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                start = 1;
            }

            for (int i = start; i < lines.Length; i++)
            {
                string[] cols = SplitCsvLine(lines[i]);
                if (cols.Length < 4)
                {
                    continue;
                }

                if (!float.TryParse(cols[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    continue;
                }

                string metricId = cols[0].Trim();
                string difficultyId = cols[1].Trim();
                string subjectId = cols[2].Trim();
                report.AddMetric(metricId, value, difficultyId, subjectId);
                if (string.IsNullOrEmpty(report.DifficultyId) && !string.IsNullOrEmpty(difficultyId))
                {
                    report.DifficultyId = difficultyId;
                }

                if (metricId == BalanceMetricIds.PassengerAvgDamage && !string.IsNullOrEmpty(subjectId))
                {
                    report.PassengerDamage[subjectId] = value;
                }

                if (metricId == BalanceMetricIds.PassengerPickRate && !string.IsNullOrEmpty(subjectId))
                {
                    report.PassengerPickRate[subjectId] = value;
                }

                report.SampleCount++;
            }

            return report;
        }

        public static BalanceReport FromCsvFile(string path, string versionLabel = "analytics")
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new BalanceReport { Source = "analytics_csv", VersionLabel = versionLabel };
            }

            return FromCsv(File.ReadAllText(path), versionLabel);
        }

        private static string[] SplitCsvLine(string line)
        {
            var list = new List<string>(8);
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    list.Add(sb.ToString());
                    sb.Length = 0;
                    continue;
                }

                sb.Append(c);
            }

            list.Add(sb.ToString());
            return list.ToArray();
        }
    }
}
