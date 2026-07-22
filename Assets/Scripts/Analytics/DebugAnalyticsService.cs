using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace LastTrain.Analytics
{
    /// <summary>Editor/개발용. Console에 JSON 형태로 이벤트를 출력한다.</summary>
    public sealed class DebugAnalyticsService : IAnalyticsService
    {
        private readonly string _prefix;

        public DebugAnalyticsService(string prefix = "[Analytics]")
        {
            _prefix = string.IsNullOrWhiteSpace(prefix) ? "[Analytics]" : prefix;
        }

        public void Track(string eventName, IDictionary<string, object> parameters = null)
        {
            Track(new AnalyticsEvent(eventName, parameters));
        }

        public void Track(AnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null || string.IsNullOrWhiteSpace(analyticsEvent.Name))
            {
                return;
            }

            string json = ToJson(analyticsEvent.Name, analyticsEvent.Parameters);
            Debug.Log($"{_prefix} {json}");
        }

        private static string ToJson(string eventName, IDictionary<string, object> parameters)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"event\":\"").Append(Escape(eventName)).Append("\",\"params\":{");

            if (parameters != null)
            {
                bool first = true;
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || IsPiiKey(pair.Key))
                    {
                        continue;
                    }

                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;
                    sb.Append('"').Append(Escape(pair.Key)).Append("\":");
                    AppendValue(sb, pair.Value);
                }
            }

            sb.Append("}}");
            return sb.ToString();
        }

        private static bool IsPiiKey(string key)
        {
            string k = key.ToLowerInvariant();
            return k.Contains("email")
                   || k.Contains("phone")
                   || k.Contains("advertising")
                   || k == "name"
                   || k == "user_name"
                   || k == "ad_id"
                   || k == "idfa"
                   || k == "gaid";
        }

        private static void AppendValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case int or long or short or byte:
                    sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                case float or double or decimal:
                    sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                case string s:
                    sb.Append('"').Append(Escape(s)).Append('"');
                    break;
                case IEnumerable<object> list:
                    sb.Append('[');
                    bool first = true;
                    foreach (object item in list)
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        first = false;
                        AppendValue(sb, item);
                    }

                    sb.Append(']');
                    break;
                default:
                    sb.Append('"').Append(Escape(Convert.ToString(value, CultureInfo.InvariantCulture)))
                        .Append('"');
                    break;
            }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal);
        }
    }
}
