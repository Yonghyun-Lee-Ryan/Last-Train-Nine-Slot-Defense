using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LastTrain.Balance
{
    public sealed class BalanceDiffEntry
    {
        public string MetricId;
        public string DifficultyId;
        public string SubjectId;
        public float Before;
        public float After;
        public float Delta;
    }

    public sealed class BalanceDiffReport
    {
        public string BeforeLabel;
        public string AfterLabel;
        public List<BalanceDiffEntry> Entries = new();
    }

    public static class BalanceDiffBuilder
    {
        public static BalanceDiffReport Compare(BalanceReport before, BalanceReport after)
        {
            var diff = new BalanceDiffReport
            {
                BeforeLabel = before?.VersionLabel ?? "before",
                AfterLabel = after?.VersionLabel ?? "after",
            };

            var keys = new HashSet<string>(StringComparer.Ordinal);
            CollectKeys(before, keys);
            CollectKeys(after, keys);

            foreach (string key in keys)
            {
                ParseKey(key, out string metricId, out string difficultyId, out string subjectId);
                float b = 0f;
                float a = 0f;
                before?.TryGetMetric(metricId, difficultyId, subjectId, out b);
                after?.TryGetMetric(metricId, difficultyId, subjectId, out a);
                if (Math.Abs(a - b) < 0.0001f)
                {
                    continue;
                }

                diff.Entries.Add(new BalanceDiffEntry
                {
                    MetricId = metricId,
                    DifficultyId = difficultyId,
                    SubjectId = subjectId,
                    Before = b,
                    After = a,
                    Delta = a - b,
                });
            }

            return diff;
        }

        private static void CollectKeys(BalanceReport report, HashSet<string> keys)
        {
            if (report?.Metrics == null)
            {
                return;
            }

            for (int i = 0; i < report.Metrics.Count; i++)
            {
                BalanceMetricValue m = report.Metrics[i];
                keys.Add(MakeKey(m.MetricId, m.DifficultyId, m.SubjectId));
            }
        }

        private static string MakeKey(string metricId, string difficultyId, string subjectId)
            => $"{metricId}|{difficultyId}|{subjectId}";

        private static void ParseKey(string key, out string metricId, out string difficultyId, out string subjectId)
        {
            string[] parts = key.Split('|');
            metricId = parts.Length > 0 ? parts[0] : string.Empty;
            difficultyId = parts.Length > 1 ? parts[1] : string.Empty;
            subjectId = parts.Length > 2 ? parts[2] : string.Empty;
        }

        public static string ToMarkdown(BalanceDiffReport diff)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Balance Diff: {diff.BeforeLabel} → {diff.AfterLabel}");
            sb.AppendLine();
            sb.AppendLine("| Metric | Difficulty | Subject | Before | After | Delta |");
            sb.AppendLine("|---|---|---|---:|---:|---:|");
            for (int i = 0; i < diff.Entries.Count; i++)
            {
                BalanceDiffEntry e = diff.Entries[i];
                sb.Append("| ").Append(e.MetricId)
                    .Append(" | ").Append(e.DifficultyId)
                    .Append(" | ").Append(e.SubjectId)
                    .Append(" | ").Append(F(e.Before))
                    .Append(" | ").Append(F(e.After))
                    .Append(" | ").Append(F(e.Delta))
                    .AppendLine(" |");
            }

            return sb.ToString();
        }

        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
