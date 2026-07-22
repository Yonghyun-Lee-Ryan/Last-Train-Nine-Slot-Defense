using System;
using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Economy;
using LastTrain.Run;

namespace LastTrain.Passenger
{
    public enum SummonRequestResult
    {
        Success = 0,
        NoEmptySlot = 1,
        NotEnoughCoins = 2,
        NoUnlockedPassengers = 3,
        OfferAlreadyOpen = 4,
        InvalidState = 5
    }

    public enum SelectOfferResult
    {
        Success = 0,
        InvalidOffer = 1,
        NoEmptySlot = 2,
        InvalidState = 3
    }

    public enum RerollResult
    {
        Success = 0,
        NoActiveOffers = 1,
        NoRerollsRemaining = 2,
        AdFailed = 3,
        InvalidState = 4
    }

    /// <summary>
    /// 소환 요청·후보 선택·리롤을 조율한다.
    /// 코인 차감과 승객 생성은 선택 시점에 원자적으로 처리한다.
    /// (요청 시에는 후보만 생성하고, 선택 확정 시 비용 차감 + 배치)
    /// </summary>
    public sealed class SummonManager
    {
        public event Action<SummonRequestResult> SummonRequested;
        public event Action<SelectOfferResult, PassengerRuntime> OfferSelected;
        public event Action<string> StatusMessage;

        private readonly RunState _runState;
        private readonly SummonEconomyConfig _config;
        private readonly PassengerOfferService _offerService;
        private readonly CurrencyService _currencyService;
        private readonly Func<bool> _tryShowRewardedAd;

        private int _pendingCost;
        private bool _costReserved;

        public SummonManager(
            RunState runState,
            SummonEconomyConfig config,
            PassengerOfferService offerService,
            Func<bool> tryShowRewardedAd = null)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
            _currencyService = new CurrencyService(runState);
            _tryShowRewardedAd = tryShowRewardedAd ?? MockRewardedAdSuccess;
        }

        public int CurrentSummonCost =>
            SummonCostCalculator.CalculateCost(_config, _runState);

        public int RemainingFreeRerolls =>
            Math.Max(0, _config.FreeRerollsPerRun - _runState.Summon.FreeRerollsUsed);

        public int RemainingAdRerolls =>
            Math.Max(0, _config.AdRerollsPerRun - _runState.Summon.AdRerollsUsed);

        public bool HasActiveOffers => _runState.Summon.HasActiveOffers;

        public IReadOnlyList<PassengerData> CurrentOffers => _runState.Summon.CurrentOffers;

        /// <summary>
        /// 소환을 시작한다. 빈 슬롯·코인 확인 후 후보 3명을 생성한다.
        /// 코인은 선택 확정 시 차감한다(원자성).
        /// </summary>
        public SummonRequestResult TryBeginSummon()
        {
            if (_runState.Battle == null || !_runState.Battle.IsRunActive)
            {
                return FailRequest(SummonRequestResult.InvalidState, "활성 회차가 없습니다.");
            }

            if (_runState.Summon.HasActiveOffers)
            {
                return FailRequest(SummonRequestResult.OfferAlreadyOpen, "이미 소환 후보가 열려 있습니다.");
            }

            if (!_runState.HasEmptySlot())
            {
                return FailRequest(SummonRequestResult.NoEmptySlot, "빈 좌석이 없어 소환할 수 없습니다.");
            }

            if (_offerService.UnlockedPassengers.Count == 0)
            {
                return FailRequest(SummonRequestResult.NoUnlockedPassengers, "해금된 승객이 없습니다.");
            }

            int cost = CurrentSummonCost;
            if (!_currencyService.CanAfford(cost))
            {
                return FailRequest(SummonRequestResult.NotEnoughCoins, $"코인이 부족합니다. (필요 {cost})");
            }

            List<PassengerData> offers = _offerService.GenerateOffers();
            if (offers.Count == 0)
            {
                return FailRequest(SummonRequestResult.NoUnlockedPassengers, "후보를 생성할 수 없습니다.");
            }

            _pendingCost = cost;
            _costReserved = true;
            _runState.Summon.SetOffers(offers);
            SummonRequested?.Invoke(SummonRequestResult.Success);
            return SummonRequestResult.Success;
        }

        public void CancelOffers()
        {
            _pendingCost = 0;
            _costReserved = false;
            _runState.Summon.ClearOffers();
        }

        public SelectOfferResult TrySelectOffer(int offerIndex, out PassengerRuntime placed)
        {
            placed = null;

            if (!_runState.Summon.HasActiveOffers || !_costReserved)
            {
                return SelectOfferResult.InvalidState;
            }

            PassengerData data = _runState.Summon.GetOffer(offerIndex);
            if (data == null)
            {
                return SelectOfferResult.InvalidOffer;
            }

            int emptySlot = _runState.FindFirstEmptySlot();
            if (emptySlot < 0)
            {
                StatusMessage?.Invoke("빈 좌석이 없어 배치할 수 없습니다.");
                return SelectOfferResult.NoEmptySlot;
            }

            // 원자적 처리: 코인 차감 성공 시에만 배치
            if (!_currencyService.TrySpend(_pendingCost))
            {
                StatusMessage?.Invoke("코인이 부족합니다.");
                CancelOffers();
                return SelectOfferResult.InvalidState;
            }

            placed = PassengerRuntime.Create(data, starLevel: 1);
            if (!_runState.TryPlacePassenger(emptySlot, placed))
            {
                // 배치 실패 시 코인 환불
                _currencyService.AddCoins(_pendingCost);
                placed = null;
                CancelOffers();
                return SelectOfferResult.NoEmptySlot;
            }

            _runState.Summon.RecordPaidSummon();
            _pendingCost = 0;
            _costReserved = false;
            _runState.Summon.ClearOffers();
            Ability.AbilityEffectApplier.RefreshPassengerBuffs(_runState);
            Synergy.SynergyEffectApplier.Refresh(_runState);

            OfferSelected?.Invoke(SelectOfferResult.Success, placed);
            return SelectOfferResult.Success;
        }

        public RerollResult TryRerollFree()
        {
            if (!_runState.Summon.HasActiveOffers)
            {
                return RerollResult.NoActiveOffers;
            }

            if (RemainingFreeRerolls <= 0)
            {
                StatusMessage?.Invoke("무료 리롤을 모두 사용했습니다.");
                return RerollResult.NoRerollsRemaining;
            }

            List<PassengerData> offers = _offerService.GenerateOffers();
            _runState.Summon.RecordFreeReroll();
            _runState.Summon.SetOffers(offers);
            return RerollResult.Success;
        }

        /// <summary>광고 리롤. 광고 SDK는 Mock 콜백으로 대체한다.</summary>
        public RerollResult TryRerollWithAd()
        {
            if (!_runState.Summon.HasActiveOffers)
            {
                return RerollResult.NoActiveOffers;
            }

            if (RemainingAdRerolls <= 0)
            {
                StatusMessage?.Invoke("광고 리롤을 모두 사용했습니다.");
                return RerollResult.NoRerollsRemaining;
            }

            if (!_tryShowRewardedAd())
            {
                StatusMessage?.Invoke("광고 시청에 실패했습니다.");
                return RerollResult.AdFailed;
            }

            return ApplyAdRerollInternal(recordUsage: true);
        }

        /// <summary>
        /// 광고 Completed 이후 호출. 한도 소비는 AdLimitService가 담당한 경우를 위해
        /// recordUsage로 SummonProgress 기록 여부를 제어한다.
        /// </summary>
        public RerollResult ApplyAdReroll(bool recordUsage = true)
        {
            if (!_runState.Summon.HasActiveOffers)
            {
                return RerollResult.NoActiveOffers;
            }

            return ApplyAdRerollInternal(recordUsage);
        }

        private RerollResult ApplyAdRerollInternal(bool recordUsage)
        {
            List<PassengerData> offers = _offerService.GenerateOffers();
            if (recordUsage)
            {
                _runState.Summon.RecordAdReroll();
            }

            _runState.Summon.SetOffers(offers);
            StatusMessage?.Invoke("광고 리롤 성공");
            return RerollResult.Success;
        }

        private SummonRequestResult FailRequest(SummonRequestResult result, string message)
        {
            StatusMessage?.Invoke(message);
            SummonRequested?.Invoke(result);
            return result;
        }

        private static bool MockRewardedAdSuccess()
        {
            return true;
        }
    }
}
