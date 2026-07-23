using System;
using System.Globalization;

namespace LastTrain.Balance
{
    public static class BalanceValidator
    {
        public static void ApplyTargets(BalanceReport report, BalanceTargetData targets)
        {
            ApplyTargets(report, targets, ignoreFixedLoadoutPickRates: false);
        }

        public static void ApplyTargets(
            BalanceReport report,
            BalanceTargetData targets,
            bool ignoreFixedLoadoutPickRates)
        {
            if (report == null || targets == null)
            {
                return;
            }

            report.Warnings.Clear();
            BalanceMetricRange[] ranges = targets.Ranges;
            for (int i = 0; i < ranges.Length; i++)
            {
                BalanceMetricRange range = ranges[i];
                if (range == null || string.IsNullOrWhiteSpace(range.metricId))
                {
                    continue;
                }

                if (ignoreFixedLoadoutPickRates
                    && (range.metricId == BalanceMetricIds.PassengerPickRate
                        || range.metricId == BalanceMetricIds.AbilityPickRate))
                {
                    continue;
                }

                EvaluateRange(report, range);
            }

            // 규칙 기반 추가 경고
            for (int i = 0; i < report.Metrics.Count; i++)
            {
                BalanceMetricValue m = report.Metrics[i];
                if (m.MetricId == BalanceMetricIds.PassengerPickRate)
                {
                    if (ignoreFixedLoadoutPickRates)
                    {
                        continue;
                    }

                    if (m.Value >= 0.70f)
                    {
                        Add(report, BalanceSeverity.Warning, m, 0.10f, 0.70f,
                            $"승객 '{m.SubjectId}' 픽률 {Pct(m.Value)} — 과도한 범용성");
                    }
                    else if (m.Value > 0f && m.Value < 0.10f)
                    {
                        Add(report, BalanceSeverity.Warning, m, 0.10f, 0.70f,
                            $"승객 '{m.SubjectId}' 픽률 {Pct(m.Value)} — 효용 부족");
                    }
                }
                else if (m.MetricId == BalanceMetricIds.AbilityPickRate && m.Value >= 0.80f)
                {
                    if (ignoreFixedLoadoutPickRates)
                    {
                        continue;
                    }

                    Add(report, BalanceSeverity.Critical, m, 0f, 0.80f,
                        $"능력 '{m.SubjectId}' 선택률 {Pct(m.Value)} — 필수 카드 가능성");
                }
            }
        }

        private static void EvaluateRange(BalanceReport report, BalanceMetricRange range)
        {
            for (int i = 0; i < report.Metrics.Count; i++)
            {
                BalanceMetricValue m = report.Metrics[i];
                if (!string.Equals(m.MetricId, range.metricId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(range.difficultyId)
                    && !string.Equals(m.DifficultyId, range.difficultyId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(range.subjectId)
                    && !string.Equals(m.SubjectId, range.subjectId, StringComparison.Ordinal))
                {
                    continue;
                }

                BalanceSeverity severity = Classify(m.Value, range.minValue, range.maxValue, range.warningBand);
                if (severity == BalanceSeverity.None)
                {
                    continue;
                }

                Add(report, severity, m, range.minValue, range.maxValue,
                    $"{m.MetricId}={m.Value.ToString("0.###", CultureInfo.InvariantCulture)} 목표 [{range.minValue},{range.maxValue}] 이탈");
            }
        }

        public static BalanceSeverity Classify(float value, float min, float max, float warningBand)
        {
            if (value >= min && value <= max)
            {
                return BalanceSeverity.None;
            }

            float band = Math.Max(0f, warningBand);
            if (value < min)
            {
                return value >= min - band ? BalanceSeverity.Warning : BalanceSeverity.Critical;
            }

            return value <= max + band ? BalanceSeverity.Warning : BalanceSeverity.Critical;
        }

        private static void Add(
            BalanceReport report,
            BalanceSeverity severity,
            BalanceMetricValue m,
            float min,
            float max,
            string message)
        {
            report.Warnings.Add(new BalanceWarning
            {
                Severity = severity,
                MetricId = m.MetricId,
                DifficultyId = m.DifficultyId,
                SubjectId = m.SubjectId,
                Message = message,
                Actual = m.Value,
                Min = min,
                Max = max,
            });
        }

        private static string Pct(float v) => (v * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }
}
