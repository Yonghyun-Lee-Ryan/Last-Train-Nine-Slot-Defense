using System.Globalization;
using System.IO;
using System.Text;

namespace LastTrain.Balance
{
    public static class BalanceReportExporter
    {
        public static string ToCsv(BalanceReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("metric_id,difficulty_id,subject_id,value,label");
            if (report?.Metrics == null)
            {
                return sb.ToString();
            }

            for (int i = 0; i < report.Metrics.Count; i++)
            {
                BalanceMetricValue m = report.Metrics[i];
                sb.Append(Escape(m.MetricId)).Append(',')
                    .Append(Escape(m.DifficultyId)).Append(',')
                    .Append(Escape(m.SubjectId)).Append(',')
                    .Append(m.Value.ToString("0.####", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Escape(m.Label))
                    .AppendLine();
            }

            return sb.ToString();
        }

        public static string ToMarkdown(BalanceReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Balance Report ({report?.VersionLabel})");
            sb.AppendLine();
            sb.AppendLine($"- Source: `{report?.Source}`");
            sb.AppendLine($"- Difficulty: `{report?.DifficultyId}`");
            sb.AppendLine($"- Samples: {report?.SampleCount ?? 0}");
            sb.AppendLine($"- GeneratedUtc: {report?.GeneratedUtc:o}");
            sb.AppendLine();
            sb.AppendLine("## Metrics");
            sb.AppendLine("| Metric | Difficulty | Subject | Value |");
            sb.AppendLine("|---|---|---|---:|");
            if (report?.Metrics != null)
            {
                for (int i = 0; i < report.Metrics.Count; i++)
                {
                    BalanceMetricValue m = report.Metrics[i];
                    sb.Append("| ").Append(m.MetricId)
                        .Append(" | ").Append(m.DifficultyId)
                        .Append(" | ").Append(m.SubjectId)
                        .Append(" | ").Append(m.Value.ToString("0.###", CultureInfo.InvariantCulture))
                        .AppendLine(" |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Survival Curve");
            if (report?.SurvivalCurveByStation != null)
            {
                foreach (var pair in report.SurvivalCurveByStation)
                {
                    sb.AppendLine($"- Station {pair.Key}: {(pair.Value * 100f).ToString("0.0", CultureInfo.InvariantCulture)}%");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Passenger Pick vs Damage");
            if (report?.PassengerPickRate != null)
            {
                foreach (var pair in report.PassengerPickRate)
                {
                    report.PassengerDamage.TryGetValue(pair.Key, out float dmg);
                    sb.AppendLine($"- {pair.Key}: pick={pair.Value:0.###}, dmg={dmg:0.###}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Warnings");
            if (report?.Warnings != null && report.Warnings.Count > 0)
            {
                for (int i = 0; i < report.Warnings.Count; i++)
                {
                    BalanceWarning w = report.Warnings[i];
                    sb.AppendLine($"- **{w.Severity}**: {w.Message}");
                }
            }
            else
            {
                sb.AppendLine("- (none)");
            }

            return sb.ToString();
        }

        public static string WriteFiles(BalanceReport report, string directory, string baseName)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            Directory.CreateDirectory(directory);
            string safe = string.IsNullOrWhiteSpace(baseName) ? "balance_report" : baseName;
            string csvPath = Path.Combine(directory, safe + ".csv");
            string mdPath = Path.Combine(directory, safe + ".md");
            File.WriteAllText(csvPath, ToCsv(report), Encoding.UTF8);
            File.WriteAllText(mdPath, ToMarkdown(report), Encoding.UTF8);
            return csvPath;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
