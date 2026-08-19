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

    /// <summary>서버 시간 연동용. 파싱 실패 시 로컬 시각과 동일하다.</summary>
    public sealed class ServerSyncedLiveEventClock : ILiveEventClock
    {
        private readonly ILiveEventClock _fallback;
        private TimeSpan _offset;

        public ServerSyncedLiveEventClock(ILiveEventClock fallback = null)
        {
            _fallback = fallback ?? new LocalLiveEventClock();
        }

        public DateTime UtcNow => _fallback.UtcNow + _offset;

        public bool HasServerOffset => _offset != TimeSpan.Zero;

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

    /// <summary>Remote Config JSON 오버레이. 파싱 실패·빈 JSON이면 로컬 카탈로그.</summary>
    public sealed class JsonLiveEventProvider : ILiveEventProvider
    {
        private readonly ILiveEventProvider _fallback;
        private readonly Func<string> _jsonSource;
        private readonly string _inlineJson;

        public JsonLiveEventProvider(ILiveEventProvider fallback = null, Func<string> jsonSource = null)
        {
            _fallback = fallback ?? new LocalLiveEventProvider();
            _jsonSource = jsonSource;
        }

        public JsonLiveEventProvider(ILiveEventProvider fallback, string json)
            : this(fallback, jsonSource: null)
        {
            _inlineJson = json ?? string.Empty;
        }

        public SeasonData[] LoadSeasons() => FilterSeasons(_fallback.LoadSeasons());
        public LiveEventData[] LoadEvents() => FilterEvents(_fallback.LoadEvents());

        public bool LastRemoteParseSucceeded { get; private set; } = true;

        private LiveEventData[] FilterEvents(LiveEventData[] local)
        {
            local ??= Array.Empty<LiveEventData>();
            if (!TryReadDto(out RemoteLiveOpsCatalogDto dto))
            {
                return local;
            }

            if (dto.disableAll)
            {
                return Array.Empty<LiveEventData>();
            }

            if (dto.enabledEventIds == null || dto.enabledEventIds.Length == 0)
            {
                return local;
            }

            var filtered = new System.Collections.Generic.List<LiveEventData>(local.Length);
            for (int i = 0; i < local.Length; i++)
            {
                LiveEventData data = local[i];
                if (data == null)
                {
                    continue;
                }

                for (int j = 0; j < dto.enabledEventIds.Length; j++)
                {
                    if (string.Equals(data.Id, dto.enabledEventIds[j], StringComparison.Ordinal))
                    {
                        filtered.Add(data);
                        break;
                    }
                }
            }

            return filtered.ToArray();
        }

        private SeasonData[] FilterSeasons(SeasonData[] local)
        {
            local ??= Array.Empty<SeasonData>();
            if (!TryReadDto(out RemoteLiveOpsCatalogDto dto))
            {
                return local;
            }

            return dto.disableAll ? Array.Empty<SeasonData>() : local;
        }

        private bool TryReadDto(out RemoteLiveOpsCatalogDto dto)
        {
            dto = null;
            LastRemoteParseSucceeded = true;
            string json = _jsonSource != null ? _jsonSource.Invoke() : _inlineJson;
            if (string.IsNullOrWhiteSpace(json))
            {
                LastRemoteParseSucceeded = false;
                return false;
            }

            json = json.Trim();
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}')
            {
                LastRemoteParseSucceeded = false;
                return false;
            }

            try
            {
                dto = JsonUtility.FromJson<RemoteLiveOpsCatalogDto>(json);
                if (dto == null)
                {
                    LastRemoteParseSucceeded = false;
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                LastRemoteParseSucceeded = false;
                dto = null;
                return false;
            }
        }

        [Serializable]
        private sealed class RemoteLiveOpsCatalogDto
        {
            public bool disableAll;
            public string[] enabledEventIds;
        }
    }
}
