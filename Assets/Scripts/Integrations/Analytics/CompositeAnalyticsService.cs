using System.Collections.Generic;
using LastTrain.Analytics;

namespace LastTrain.Integrations
{
    /// <summary>여러 IAnalyticsService 구현체에 이벤트를 fan-out한다.</summary>
    public sealed class CompositeAnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsService[] _services;

        public CompositeAnalyticsService(IReadOnlyList<IAnalyticsService> services)
        {
            if (services == null || services.Count == 0)
            {
                _services = new[] { new NoOpAnalyticsService() };
                return;
            }

            _services = new IAnalyticsService[services.Count];
            for (int i = 0; i < services.Count; i++)
            {
                _services[i] = services[i];
            }
        }

        public void Track(string eventName, IDictionary<string, object> parameters = null)
        {
            for (int i = 0; i < _services.Length; i++)
            {
                _services[i].Track(eventName, parameters);
            }
        }

        public void Track(AnalyticsEvent analyticsEvent)
        {
            for (int i = 0; i < _services.Length; i++)
            {
                _services[i].Track(analyticsEvent);
            }
        }
    }
}
