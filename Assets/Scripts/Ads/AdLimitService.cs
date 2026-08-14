using System;
using System.Collections.Generic;

namespace LastTrain.Ads
{
    /// <summary>회차·일일 광고 횟수 제한 및 전역 쿨다운.</summary>
    public sealed class AdLimitService
    {
        public const int PassengerRerollPerRun = 2;
        public const int AbilityRerollPerRun = 2;
        public const int RevivePerRun = 1;
        public const int DoubleResultPerRun = 1;
        public const int StationRewardDoublePerStation = 1;
        public const int FreeSummonPerDay = 3;
        public const int ShopRefreshPerRun = 3;
        public const int AttendanceBonusPerDay = 1;
        public const int SeasonPassTrackPerDay = 5;
        public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(2);

        private readonly Dictionary<RewardedAdPlacement, int> _runUsage = new();
        private int _stationDoubleUsed;
        private int _currentStationIndex = -1;
        private string _dailyKey = string.Empty;
        private int _dailyFreeSummonUsed;
        private int _dailyAttendanceBonusUsed;
        private int _dailySeasonPassUsed;
        private int _dailyRewardedUsed;
        private int _rewardedDailyLimit = int.MaxValue;
        private int _revivePerRun = RevivePerRun;
        private DateTime _nextAvailableUtc = DateTime.MinValue;

        /// <summary>테스트용 현재 시각 주입. null이면 UtcNow.</summary>
        public Func<DateTime> UtcNowProvider { get; set; }

        public TimeSpan Cooldown { get; set; } = DefaultCooldown;

        public bool IsOnCooldown => UtcNow() < _nextAvailableUtc;

        /// <summary>Remote Config 스냅샷을 광고 한도에 반영한다.</summary>
        public void ApplyRemoteConfig(Integrations.RemoteConfigSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            _rewardedDailyLimit = snapshot.RewardedDailyLimit > 0
                ? snapshot.RewardedDailyLimit
                : int.MaxValue;
            _revivePerRun = Math.Max(0, snapshot.FreeRevivePerRun);
        }

        public void BeginRun()
        {
            _runUsage.Clear();
            _stationDoubleUsed = 0;
            _currentStationIndex = -1;
            _nextAvailableUtc = DateTime.MinValue;
            EnsureDailyBucket();
        }

        public void NotifyStationChanged(int stationIndex)
        {
            if (_currentStationIndex != stationIndex)
            {
                _currentStationIndex = stationIndex;
                _stationDoubleUsed = 0;
            }
        }

        public bool CanUse(RewardedAdPlacement placement)
        {
            EnsureDailyBucket();
            if (IsOnCooldown && !IsRerollPlacement(placement))
            {
                return false;
            }

            if (IsRewardedPlacement(placement) && _dailyRewardedUsed >= _rewardedDailyLimit)
            {
                return false;
            }

            int used = GetUsed(placement);
            return used < GetLimit(placement);
        }

        public int GetRemaining(RewardedAdPlacement placement)
        {
            EnsureDailyBucket();
            return Math.Max(0, GetLimit(placement) - GetUsed(placement));
        }

        public bool TryConsume(RewardedAdPlacement placement)
        {
            if (!CanUse(placement))
            {
                return false;
            }

            if (placement == RewardedAdPlacement.StationRewardDouble)
            {
                _stationDoubleUsed++;
            }
            else if (placement == RewardedAdPlacement.FreeSummon)
            {
                _dailyFreeSummonUsed++;
            }
            else if (placement == RewardedAdPlacement.AttendanceBonus)
            {
                _dailyAttendanceBonusUsed++;
            }
            else if (placement == RewardedAdPlacement.SeasonPassTrack)
            {
                _dailySeasonPassUsed++;
            }
            else
            {
                if (!_runUsage.TryGetValue(placement, out int used))
                {
                    used = 0;
                }

                _runUsage[placement] = used + 1;
            }

            if (IsRewardedPlacement(placement))
            {
                _dailyRewardedUsed++;
            }

            if (Cooldown > TimeSpan.Zero && !IsRerollPlacement(placement))
            {
                _nextAvailableUtc = UtcNow() + Cooldown;
            }

            return true;
        }

        private static bool IsRewardedPlacement(RewardedAdPlacement placement)
        {
            return true;
        }

        private static bool IsRerollPlacement(RewardedAdPlacement placement)
        {
            return placement == RewardedAdPlacement.PassengerReroll
                   || placement == RewardedAdPlacement.AbilityReroll;
        }

        private DateTime UtcNow()
        {
            return UtcNowProvider?.Invoke() ?? DateTime.UtcNow;
        }

        private int GetUsed(RewardedAdPlacement placement)
        {
            if (placement == RewardedAdPlacement.StationRewardDouble)
            {
                return _stationDoubleUsed;
            }

            if (placement == RewardedAdPlacement.FreeSummon)
            {
                return _dailyFreeSummonUsed;
            }

            if (placement == RewardedAdPlacement.AttendanceBonus)
            {
                return _dailyAttendanceBonusUsed;
            }

            if (placement == RewardedAdPlacement.SeasonPassTrack)
            {
                return _dailySeasonPassUsed;
            }

            return _runUsage.TryGetValue(placement, out int used) ? used : 0;
        }

        private int GetLimit(RewardedAdPlacement placement)
        {
            return placement switch
            {
                RewardedAdPlacement.PassengerReroll => PassengerRerollPerRun,
                RewardedAdPlacement.AbilityReroll => AbilityRerollPerRun,
                RewardedAdPlacement.Revive => _revivePerRun,
                RewardedAdPlacement.DoubleResultReward => DoubleResultPerRun,
                RewardedAdPlacement.StationRewardDouble => StationRewardDoublePerStation,
                RewardedAdPlacement.FreeSummon => FreeSummonPerDay,
                RewardedAdPlacement.ShopRefresh => ShopRefreshPerRun,
                RewardedAdPlacement.AttendanceBonus => AttendanceBonusPerDay,
                RewardedAdPlacement.SeasonPassTrack => SeasonPassTrackPerDay,
                _ => 0,
            };
        }

        private void EnsureDailyBucket()
        {
            string today = UtcNow().ToString("yyyy-MM-dd");
            if (string.Equals(_dailyKey, today, StringComparison.Ordinal))
            {
                return;
            }

            _dailyKey = today;
            _dailyFreeSummonUsed = 0;
            _dailyAttendanceBonusUsed = 0;
            _dailySeasonPassUsed = 0;
            _dailyRewardedUsed = 0;
        }
    }
}
