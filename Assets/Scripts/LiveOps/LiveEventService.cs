using System;
using System.Collections.Generic;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.LiveOps
{
    public enum LiveEventPhase
    {
        None = 0,
        Scheduled = 1,
        Active = 2,
        Ended = 3,
        ClaimWindow = 4,
    }

    /// <summary>
    /// 시즌/라이브 이벤트 오케스트레이터.
    /// 데이터가 없거나 로드 실패 시 기본 게임(이벤트 없음)으로 안전하게 동작한다.
    /// </summary>
    public sealed class LiveEventService
    {
        private readonly ILiveEventProvider _provider;
        private readonly ILiveEventClock _clock;
        private LiveEventData[] _catalog = Array.Empty<LiveEventData>();
        private SeasonData[] _seasons = Array.Empty<SeasonData>();
        private LiveEventData _active;

        public LiveEventService(ILiveEventProvider provider = null, ILiveEventClock clock = null)
        {
            _provider = provider ?? new LocalLiveEventProvider();
            _clock = clock ?? new LocalLiveEventClock();
        }

        public LiveEventData ActiveEvent => _active;
        public bool HasActiveEvent => _active != null;
        public ILiveEventClock Clock => _clock;
        public IReadOnlyList<LiveEventData> Catalog => _catalog;
        public IReadOnlyList<SeasonData> Seasons => _seasons;

        public void RefreshCatalog()
        {
            try
            {
                _seasons = _provider.LoadSeasons() ?? Array.Empty<SeasonData>();
                _catalog = _provider.LoadEvents() ?? Array.Empty<LiveEventData>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LiveEventService] 카탈로그 로드 실패 → 기본 게임: {ex.Message}");
                _seasons = Array.Empty<SeasonData>();
                _catalog = Array.Empty<LiveEventData>();
            }

            _active = ResolveActive(_clock.UtcNow);
        }

        public LiveEventData ResolveActive(DateTime utcNow)
        {
            LiveEventData best = null;
            for (int i = 0; i < _catalog.Length; i++)
            {
                LiveEventData data = _catalog[i];
                if (data == null || !data.TryGetSchedule(out DateTime start, out DateTime end))
                {
                    continue;
                }

                if (utcNow >= start && utcNow < end)
                {
                    best = data;
                    break;
                }
            }

            return best;
        }

        public LiveEventPhase GetPhase(LiveEventData data, DateTime? utcNow = null)
        {
            if (data == null || !data.TryGetSchedule(out DateTime start, out DateTime end))
            {
                return LiveEventPhase.None;
            }

            DateTime now = utcNow ?? _clock.UtcNow;
            if (now < start)
            {
                return LiveEventPhase.Scheduled;
            }

            if (now < end)
            {
                return LiveEventPhase.Active;
            }

            if (data.EndedRewardPolicy == EndedRewardPolicy.ClaimUntilExpiry
                && DateTime.TryParse(
                    data.ClaimExpiryUtc,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime claimExpiry)
                && now < claimExpiry)
            {
                return LiveEventPhase.ClaimWindow;
            }

            return LiveEventPhase.Ended;
        }

        public LiveEventProgress GetOrCreateProgress(MetaSaveData meta, LiveEventData data)
        {
            if (meta == null || data == null)
            {
                return null;
            }

            meta.EnsureDefaults();
            LiveEventProgress[] list = meta.liveEventProgresses ?? Array.Empty<LiveEventProgress>();
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null && string.Equals(list[i].eventId, data.Id, StringComparison.Ordinal))
                {
                    list[i].EnsureDefaults();
                    return list[i];
                }
            }

            var created = new LiveEventProgress { eventId = data.Id };
            created.EnsureDefaults();
            var next = new LiveEventProgress[list.Length + 1];
            for (int i = 0; i < list.Length; i++)
            {
                next[i] = list[i];
            }

            next[list.Length] = created;
            meta.liveEventProgresses = next;
            return created;
        }

        public int TryEarnCurrency(MetaSaveData meta, LiveEventData data, int amount)
        {
            if (meta == null || data == null || amount <= 0)
            {
                return 0;
            }

            LiveEventPhase phase = GetPhase(data);
            if (phase != LiveEventPhase.Active)
            {
                return 0;
            }

            LiveEventProgress progress = GetOrCreateProgress(meta, data);
            string dayKey = _clock.UtcNow.ToString("yyyy-MM-dd");
            if (!string.Equals(progress.lastEarnDayKey, dayKey, StringComparison.Ordinal))
            {
                progress.lastEarnDayKey = dayKey;
                progress.currencyEarnedToday = 0;
            }

            int remainingCap = Math.Max(0, data.DailyCurrencyCap - progress.currencyEarnedToday);
            int granted = Math.Min(amount, remainingCap);
            if (granted <= 0)
            {
                return 0;
            }

            int maxBalance = data.EventCurrency != null ? data.EventCurrency.MaxBalance : int.MaxValue;
            int room = Math.Max(0, maxBalance - progress.currencyBalance);
            granted = Math.Min(granted, room);
            progress.currencyBalance += granted;
            progress.currencyEarnedToday += granted;
            return granted;
        }

        /// <summary>보상 수령. 중복 수령 불가. 종료 정책에 따라 거부될 수 있다.</summary>
        public bool TryClaimReward(MetaSaveData meta, LiveEventData data, string rewardId)
        {
            if (meta == null || data == null || string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            LiveEventPhase phase = GetPhase(data);
            if (phase != LiveEventPhase.Active && phase != LiveEventPhase.ClaimWindow)
            {
                return false;
            }

            LiveEventProgress progress = GetOrCreateProgress(meta, data);
            if (progress.HasClaimed(rewardId))
            {
                return false;
            }

            EventRewardStep step = FindStep(data, rewardId);
            if (step == null)
            {
                return false;
            }

            if (progress.currencyBalance < step.requiredCurrency)
            {
                return false;
            }

            // 메타 보상을 먼저 반영한 뒤 클레임 표시 — 실패 시 중복 수령 방지 상태가 오염되지 않게 한다.
            if (!MetaProgressionService.TryGrantLiveEventReward(
                    meta,
                    step.ticketFragments,
                    step.accountXp,
                    step.unlockPassengerId))
            {
                return false;
            }

            progress.MarkClaimed(rewardId);
            return true;
        }

        public void FinalizeEndedEvents(MetaSaveData meta)
        {
            if (meta == null)
            {
                return;
            }

            meta.EnsureDefaults();
            LiveEventProgress[] list = meta.liveEventProgresses ?? Array.Empty<LiveEventProgress>();
            DateTime now = _clock.UtcNow;
            for (int i = 0; i < list.Length; i++)
            {
                LiveEventProgress progress = list[i];
                if (progress == null || progress.finalized)
                {
                    continue;
                }

                LiveEventData data = FindById(progress.eventId);
                if (data == null)
                {
                    // 카탈로그에 없는 종료 이벤트 → 기본 진행을 건드리지 않고 finalize만
                    progress.finalized = true;
                    continue;
                }

                LiveEventPhase phase = GetPhase(data, now);
                if (phase == LiveEventPhase.Ended
                    && data.EndedRewardPolicy == EndedRewardPolicy.ForfeitUnclaimed)
                {
                    progress.finalized = true;
                }
            }
        }

        public LiveEventData FindById(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            for (int i = 0; i < _catalog.Length; i++)
            {
                if (_catalog[i] != null && string.Equals(_catalog[i].Id, eventId, StringComparison.Ordinal))
                {
                    return _catalog[i];
                }
            }

            return null;
        }

        private static EventRewardStep FindStep(LiveEventData data, string rewardId)
        {
            EventRewardTrack track = data.RewardTrack;
            if (track == null)
            {
                return null;
            }

            EventRewardStep[] steps = track.Steps;
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null && string.Equals(steps[i].rewardId, rewardId, StringComparison.Ordinal))
                {
                    return steps[i];
                }
            }

            return null;
        }
    }
}
