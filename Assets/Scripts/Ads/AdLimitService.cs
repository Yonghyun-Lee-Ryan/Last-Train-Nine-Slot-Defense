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
        public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(2);

        private readonly Dictionary<RewardedAdPlacement, int> _runUsage = new();
        private int _stationDoubleUsed;
        private int _currentStationIndex = -1;
        private string _dailyKey = string.Empty;
        private int _dailyFreeSummonUsed;
        private DateTime _nextAvailableUtc = DateTime.MinValue;

        /// <summary>테스트용 현재 시각 주입. null이면 UtcNow.</summary>
        public Func<DateTime> UtcNowProvider { get; set; }

        public TimeSpan Cooldown { get; set; } = DefaultCooldown;

        public bool IsOnCooldown => UtcNow() < _nextAvailableUtc;

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
            if (IsOnCooldown)
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
            else
            {
                if (!_runUsage.TryGetValue(placement, out int used))
                {
                    used = 0;
                }

                _runUsage[placement] = used + 1;
            }

            if (Cooldown > TimeSpan.Zero)
            {
                _nextAvailableUtc = UtcNow() + Cooldown;
            }

            return true;
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

            return _runUsage.TryGetValue(placement, out int used) ? used : 0;
        }

        private static int GetLimit(RewardedAdPlacement placement)
        {
            return placement switch
            {
                RewardedAdPlacement.PassengerReroll => PassengerRerollPerRun,
                RewardedAdPlacement.AbilityReroll => AbilityRerollPerRun,
                RewardedAdPlacement.Revive => RevivePerRun,
                RewardedAdPlacement.DoubleResultReward => DoubleResultPerRun,
                RewardedAdPlacement.StationRewardDouble => StationRewardDoublePerStation,
                RewardedAdPlacement.FreeSummon => FreeSummonPerDay,
                RewardedAdPlacement.ShopRefresh => ShopRefreshPerRun,
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
        }
    }
}
