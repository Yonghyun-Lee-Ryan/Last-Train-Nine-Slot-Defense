using System;
using System.Collections.Generic;

namespace LastTrain.Analytics
{
    public sealed class AnalyticsEvent
    {
        public AnalyticsEvent(string name, IDictionary<string, object> parameters = null)
        {
            Name = name ?? string.Empty;
            Parameters = parameters != null
                ? new Dictionary<string, object>(parameters, StringComparer.Ordinal)
                : new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public string Name { get; }
        public Dictionary<string, object> Parameters { get; }
    }
}
