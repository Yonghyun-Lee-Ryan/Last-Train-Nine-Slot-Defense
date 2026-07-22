using System.Collections.Generic;

namespace LastTrain.Analytics
{
    public interface IAnalyticsService
    {
        void Track(string eventName, IDictionary<string, object> parameters = null);

        void Track(AnalyticsEvent analyticsEvent);
    }
}
