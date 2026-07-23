using System;
using UnityEngine;

namespace LastTrain.LiveOps
{
    public interface ILiveEventClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class LocalLiveEventClock : ILiveEventClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// <summary>서버 시간 연동용. 초기에는 로컬과 동일하게 동작한다.</summary>
    public sealed class ServerSyncedLiveEventClock : ILiveEventClock
    {
        private readonly ILiveEventClock _fallback;
        private TimeSpan _offset;

        public ServerSyncedLiveEventClock(ILiveEventClock fallback = null)
        {
            _fallback = fallback ?? new LocalLiveEventClock();
        }

        public DateTime UtcNow => _fallback.UtcNow + _offset;

        public void SetServerUtc(DateTime serverUtc)
        {
            _offset = serverUtc.ToUniversalTime() - _fallback.UtcNow;
        }
    }

    public interface ILiveEventProvider
    {
        SeasonData[] LoadSeasons();
        LiveEventData[] LoadEvents();
    }

    /// <summary>로컬 ScriptableObject / Resources 기반 Provider.</summary>
    public sealed class LocalLiveEventProvider : ILiveEventProvider
    {
        private readonly SeasonData[] _seasons;
        private readonly LiveEventData[] _events;

        public LocalLiveEventProvider(SeasonData[] seasons = null, LiveEventData[] events = null)
        {
            _seasons = seasons ?? Array.Empty<SeasonData>();
            _events = events ?? Array.Empty<LiveEventData>();
        }

        public SeasonData[] LoadSeasons() => _seasons;
        public LiveEventData[] LoadEvents() => _events;

        public static LocalLiveEventProvider FromResources()
        {
            LiveOpsCatalog catalog = Resources.Load<LiveOpsCatalog>("LiveOps/LiveOpsCatalog");
            if (catalog != null)
            {
                return new LocalLiveEventProvider(catalog.Seasons, catalog.Events);
            }

            SeasonData[] seasons = Resources.LoadAll<SeasonData>("LiveOps/Seasons")
                                   ?? Array.Empty<SeasonData>();
            LiveEventData[] events = Resources.LoadAll<LiveEventData>("LiveOps/Events")
                                     ?? Array.Empty<LiveEventData>();
            return new LocalLiveEventProvider(seasons, events);
        }

        /// <summary>테스트·에디터용. 명시 배열로 Provider를 만든다.</summary>
        public static LocalLiveEventProvider FromCatalog(SeasonData[] seasons, LiveEventData[] events)
        {
            return new LocalLiveEventProvider(seasons, events);
        }
    }

    /// <summary>Remote Config / JSON 교체용 Provider 골격.</summary>
    public sealed class JsonLiveEventProvider : ILiveEventProvider
    {
        private readonly ILiveEventProvider _fallback;

        public JsonLiveEventProvider(ILiveEventProvider fallback = null)
        {
            _fallback = fallback ?? new LocalLiveEventProvider();
        }

        public SeasonData[] LoadSeasons() => _fallback.LoadSeasons();
        public LiveEventData[] LoadEvents() => _fallback.LoadEvents();
    }
}
