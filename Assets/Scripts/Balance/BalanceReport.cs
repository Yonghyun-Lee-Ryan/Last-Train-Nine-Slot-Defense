using System;
using System.Collections.Generic;

namespace LastTrain.Balance
{
    [Serializable]
    public sealed class BalanceMetricValue
    {
        public string MetricId;
        public string DifficultyId;
        public string SubjectId;
        public float Value;
        public string Label;
    }

    [Serializable]
    public sealed class BalanceWarning
    {
        public BalanceSeverity Severity;
        public string MetricId;
        public string DifficultyId;
        public string SubjectId;
        public string Message;
        public float Actual;
        public float Min;
        public float Max;
    }

    /// <summary>시뮬레이션·Analytics CSV 공통 리포트 형식.</summary>
    public sealed class BalanceReport
    {
        public string Source = "simulation";
        public string VersionLabel = "A";
        public string DifficultyId = string.Empty;
        public DateTime GeneratedUtc = DateTime.UtcNow;
        public int SampleCount;
        public List<BalanceMetricValue> Metrics = new();
        public List<BalanceWarning> Warnings = new();
        public Dictionary<int, float> SurvivalCurveByStation = new();
        public Dictionary<string, float> PassengerDamage = new();
        public Dictionary<string, float> PassengerPickRate = new();

        public void AddMetric(string metricId, float value, string difficultyId = null, string subjectId = null, string label = null)
        {
            Metrics.Add(new BalanceMetricValue
            {
                MetricId = metricId,
                DifficultyId = difficultyId ?? DifficultyId ?? string.Empty,
                SubjectId = subjectId ?? string.Empty,
                Value = value,
                Label = label ?? metricId,
            });
        }

        public bool TryGetMetric(string metricId, string difficultyId, string subjectId, out float value)
        {
            value = 0f;
            for (int i = 0; i < Metrics.Count; i++)
            {
                BalanceMetricValue m = Metrics[i];
                if (!string.Equals(m.MetricId, metricId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(difficultyId)
                    && !string.Equals(m.DifficultyId, difficultyId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(subjectId)
                    && !string.Equals(m.SubjectId, subjectId, StringComparison.Ordinal))
                {
                    continue;
                }

                value = m.Value;
                return true;
            }

            return false;
        }
    }
}
