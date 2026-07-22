using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Analytics
{
    /// <summary>분석 SDK/구현 예외가 게임으로 전파되지 않게 감싼다.</summary>
    public sealed class SafeAnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsService _inner;
        private readonly Action<Exception> _onFailure;

        public SafeAnalyticsService(IAnalyticsService inner, Action<Exception> onFailure = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _onFailure = onFailure ?? DefaultOnFailure;
        }

        public void Track(string eventName, IDictionary<string, object> parameters = null)
        {
            try
            {
                _inner.Track(eventName, parameters);
            }
            catch (Exception e)
            {
                _onFailure(e);
            }
        }

        public void Track(AnalyticsEvent analyticsEvent)
        {
            try
            {
                _inner.Track(analyticsEvent);
            }
            catch (Exception e)
            {
                _onFailure(e);
            }
        }

        private static void DefaultOnFailure(Exception e)
        {
            Debug.LogWarning($"[Analytics] Track failed: {e.Message}");
        }
    }
}
