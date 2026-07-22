using System.Collections.Generic;

namespace LastTrain.Analytics
{
    /// <summary>Release 빌드용. 분석 SDK 미연결 시 게임에 영향을 주지 않는다.</summary>
    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        public void Track(string eventName, IDictionary<string, object> parameters = null)
        {
        }

        public void Track(AnalyticsEvent analyticsEvent)
        {
        }
    }
}
