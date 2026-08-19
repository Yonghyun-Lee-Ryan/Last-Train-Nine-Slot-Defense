using System;
using System.Globalization;
using LastTrain.Integrations;

namespace LastTrain.LiveOps
{
    /// <summary>
    /// Remote Config 스냅샷으로 LiveOps 시계·카탈로그를 조립한다.
    /// 실패 시 로컬 카탈로그·로컬 시각으로 기본 게임이 진행된다.
    /// </summary>
    public static class LiveOpsRuntimeFactory
    {
        public static LiveEventService Create(
            RemoteConfigSnapshot snapshot = null,
            ILiveEventProvider localProvider = null)
        {
            snapshot ??= RemoteConfigRuntime.Current ?? RemoteConfigSnapshot.Default;
            var clock = new ServerSyncedLiveEventClock();
            TrySyncClock(clock, snapshot.LiveEventServerUtc);

            if (IsRemoteKillSwitch(snapshot))
            {
                return new LiveEventService(new LocalLiveEventProvider(), clock);
            }

            ILiveEventProvider provider = localProvider ?? LocalLiveEventProvider.FromResources();
            if (snapshot.LiveOpsUseRemoteCatalog)
            {
                provider = new JsonLiveEventProvider(provider, snapshot.LiveOpsCatalogJson);
            }

            return new LiveEventService(provider, clock);
        }

        /// <summary>원격에서 내려온 kill switch만 존중한다. 로컬 기본값 false는 Heatwave를 끄지 않는다.</summary>
        public static bool IsRemoteKillSwitch(RemoteConfigSnapshot snapshot)
        {
            return snapshot != null && snapshot.LoadedFromRemote && !snapshot.LiveEventEnabled;
        }

        public static bool TrySyncClock(ServerSyncedLiveEventClock clock, string iso)
        {
            if (clock == null || string.IsNullOrWhiteSpace(iso))
            {
                return false;
            }

            if (!DateTime.TryParse(
                    iso.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsed))
            {
                return false;
            }

            if (parsed.Kind == DateTimeKind.Unspecified)
            {
                parsed = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            clock.SetServerUtc(parsed.ToUniversalTime());
            return true;
        }
    }
}
