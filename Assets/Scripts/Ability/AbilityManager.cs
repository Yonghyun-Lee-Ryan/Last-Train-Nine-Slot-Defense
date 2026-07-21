using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Ability
{
    public enum AbilityOfferResult
    {
        Success = 0,
        NotSelecting = 1,
        AlreadyOpen = 2,
        NoEligibleAbilities = 3,
        InvalidState = 4
    }

    public enum AbilitySelectResult
    {
        Success = 0,
        InvalidOffer = 1,
        CannotSelect = 2,
        InvalidState = 3
    }

    public enum AbilityRerollResult
    {
        Success = 0,
        NoActiveOffers = 1,
        NoRerollsRemaining = 2,
        AdFailed = 3,
        InvalidState = 4
    }

    /// <summary>
    /// 능력 카드 후보 생성·선택·리롤을 조율한다.
    /// 광고 리롤은 주입된 콜백으로 연결한다.
    /// </summary>
    public sealed class AbilityManager
    {
        public const int DefaultFreeRerollsPerRun = 1;
        public const int DefaultAdRerollsPerRun = 2;

        public event Action<AbilityOfferResult> OffersGenerated;
        public event Action<AbilitySelectResult, AbilityData> AbilitySelected;
        public event Action<string> StatusMessage;

        private readonly RunState _runState;
        private readonly AbilityOfferService _offerService;
        private readonly Func<bool> _tryShowRewardedAd;
        private readonly int _baseTrainMaxHp;
        private readonly int _freeRerollsPerRun;
        private readonly int _adRerollsPerRun;
        private readonly Action _onRewardFinished;

        public AbilityManager(
            RunState runState,
            AbilityOfferService offerService,
            int baseTrainMaxHp,
            Action onRewardFinished = null,
            Func<bool> tryShowRewardedAd = null,
            int freeRerollsPerRun = DefaultFreeRerollsPerRun,
            int adRerollsPerRun = DefaultAdRerollsPerRun)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
            _baseTrainMaxHp = Math.Max(1, baseTrainMaxHp);
            _onRewardFinished = onRewardFinished;
            _tryShowRewardedAd = tryShowRewardedAd ?? MockRewardedAdSuccess;
            _freeRerollsPerRun = Math.Max(0, freeRerollsPerRun);
            _adRerollsPerRun = Math.Max(0, adRerollsPerRun);
        }

        public int RemainingFreeRerolls =>
            Math.Max(0, _freeRerollsPerRun - _runState.Abilities.FreeRerollsUsed);

        public int RemainingAdRerolls =>
            Math.Max(0, _adRerollsPerRun - _runState.Abilities.AdRerollsUsed);

        public bool HasActiveOffers => _runState.Abilities.HasActiveOffers;

        public IReadOnlyList<AbilityData> CurrentOffers => _runState.Abilities.CurrentOffers;

        public IReadOnlyList<AbilityData> SelectedAbilities => _runState.Abilities.Selected;

        /// <summary>역 완료 후 능력 선택 UI를 연다.</summary>
        public AbilityOfferResult TryBeginRewardSelection()
        {
            if (_runState.Battle == null || !_runState.Battle.IsRunActive)
            {
                return FailOffer(AbilityOfferResult.InvalidState, "활성 회차가 없습니다.");
            }

            if (_runState.Abilities.HasActiveOffers)
            {
                return FailOffer(AbilityOfferResult.AlreadyOpen, "이미 능력 후보가 열려 있습니다.");
            }

            _runState.Abilities.BeginRewardSelection();
            List<AbilityData> offers = _offerService.GenerateOffers(_runState.Abilities);
            if (offers.Count == 0)
            {
                _runState.Abilities.EndRewardSelection();
                CompleteRewardFlow();
                return FailOffer(AbilityOfferResult.NoEligibleAbilities, "선택 가능한 능력이 없어 건너뜁니다.");
            }

            _runState.Abilities.SetOffers(offers);
            OffersGenerated?.Invoke(AbilityOfferResult.Success);
            return AbilityOfferResult.Success;
        }

        public AbilitySelectResult TrySelectOffer(int offerIndex)
        {
            if (!_runState.Abilities.IsSelectingReward || !_runState.Abilities.HasActiveOffers)
            {
                return AbilitySelectResult.InvalidState;
            }

            AbilityData ability = _runState.Abilities.GetOffer(offerIndex);
            if (ability == null)
            {
                return AbilitySelectResult.InvalidOffer;
            }

            if (!_runState.Abilities.CanSelect(ability))
            {
                StatusMessage?.Invoke("이 능력은 더 이상 선택할 수 없습니다.");
                return AbilitySelectResult.CannotSelect;
            }

            _runState.Abilities.AddSelected(ability);
            _runState.History.RecordAbilitySelected();
            AbilityEffectApplier.Refresh(_runState, _baseTrainMaxHp);

            _runState.Abilities.EndRewardSelection();
            CompleteRewardFlow();

            AbilitySelected?.Invoke(AbilitySelectResult.Success, ability);
            StatusMessage?.Invoke($"능력 선택: {ability.DisplayName}");
            return AbilitySelectResult.Success;
        }

        public AbilityRerollResult TryRerollFree()
        {
            if (!_runState.Abilities.HasActiveOffers)
            {
                return AbilityRerollResult.NoActiveOffers;
            }

            if (RemainingFreeRerolls <= 0)
            {
                StatusMessage?.Invoke("무료 리롤을 모두 사용했습니다.");
                return AbilityRerollResult.NoRerollsRemaining;
            }

            List<AbilityData> offers = _offerService.GenerateOffers(_runState.Abilities);
            _runState.Abilities.RecordFreeReroll();
            _runState.Abilities.SetOffers(offers);
            return AbilityRerollResult.Success;
        }

        /// <summary>광고 리롤. SDK는 주입 콜백으로 연결한다.</summary>
        public AbilityRerollResult TryRerollWithAd()
        {
            if (!_runState.Abilities.HasActiveOffers)
            {
                return AbilityRerollResult.NoActiveOffers;
            }

            if (RemainingAdRerolls <= 0)
            {
                StatusMessage?.Invoke("광고 리롤을 모두 사용했습니다.");
                return AbilityRerollResult.NoRerollsRemaining;
            }

            if (!_tryShowRewardedAd())
            {
                StatusMessage?.Invoke("광고 시청에 실패했습니다. (Mock)");
                return AbilityRerollResult.AdFailed;
            }

            List<AbilityData> offers = _offerService.GenerateOffers(_runState.Abilities);
            _runState.Abilities.RecordAdReroll();
            _runState.Abilities.SetOffers(offers);
            StatusMessage?.Invoke("능력 광고 리롤 성공 (Mock)");
            return AbilityRerollResult.Success;
        }

        /// <summary>배치 변경 후 승객 버프를 다시 계산한다.</summary>
        public void RefreshPassengerBuffs()
        {
            AbilityEffectApplier.RefreshPassengerBuffs(_runState);
        }

        private void CompleteRewardFlow()
        {
            _onRewardFinished?.Invoke();
        }

        private AbilityOfferResult FailOffer(AbilityOfferResult result, string message)
        {
            StatusMessage?.Invoke(message);
            OffersGenerated?.Invoke(result);
            return result;
        }

        private static bool MockRewardedAdSuccess()
        {
            return true;
        }
    }
}
