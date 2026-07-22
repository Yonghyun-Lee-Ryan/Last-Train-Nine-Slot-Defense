using System;
using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Run;
using LastTrain.Save;

namespace LastTrain.Ads
{
    /// <summary>광고 Completed 시에만 보상을 지급한다. RequestId로 중복 지급을 막는다.</summary>
    public sealed class AdRewardService
    {
        public const int ReviveRestoreHp = 30;

        private readonly HashSet<string> _rewardedRequestIds = new(StringComparer.Ordinal);
        private readonly AdLimitService _limits;

        public AdRewardService(AdLimitService limits)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        }

        public bool WasRequestRewarded(string requestId)
        {
            return !string.IsNullOrWhiteSpace(requestId) && _rewardedRequestIds.Contains(requestId);
        }

        /// <summary>
        /// Completed 결과를 보상으로 연결한다.
        /// 한도와 RequestId 중복을 검사하며, 성공 시에만 onGrant를 실행한다.
        /// </summary>
        public bool TryGrant(
            AdRequest request,
            AdResult result,
            Action onGrant)
        {
            if (request == null || onGrant == null)
            {
                return false;
            }

            if (result != AdResult.Completed)
            {
                return false;
            }

            if (WasRequestRewarded(request.RequestId))
            {
                return false;
            }

            if (!_limits.CanUse(request.Placement))
            {
                return false;
            }

            if (!_limits.TryConsume(request.Placement))
            {
                return false;
            }

            _rewardedRequestIds.Add(request.RequestId);
            onGrant();
            return true;
        }

        public bool TryGrantRevive(GameSession session, AdRequest request, AdResult result)
        {
            return TryGrant(request, result, () =>
            {
                if (session?.RunState?.Train == null)
                {
                    return;
                }

                TrainState train = session.RunState.Train;
                int target = Math.Min(train.MaxHp, Math.Max(1, ReviveRestoreHp));
                train.SetCurrentHp(target);
                session.MarkReviveUsed();
                session.ClearPendingDefeat();
            });
        }

        public bool TryGrantDoubleResultTickets(AdRequest request, AdResult result)
        {
            return TryGrant(request, result, () =>
            {
                MetaApplyResult last = MetaSaveSystem.LastApplyResult;
                int bonus = last?.Breakdown?.TotalTickets ?? 0;
                if (bonus <= 0)
                {
                    return;
                }

                MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
                meta.ticketFragments = SaturatingAdd(meta.ticketFragments, bonus);
                meta.accountXp = SaturatingAdd(
                    meta.accountXp,
                    bonus * MetaProgressionDefaults.AccountXpPerTicketFragment);
                meta.accountLevel = MetaProgressionService.CalculateAccountLevel(meta.accountXp);
                MetaSaveSystem.Save(meta);
            });
        }

        public bool TryGrantStationRewardDouble(
            RunState runState,
            int baseCoins,
            AdRequest request,
            AdResult result)
        {
            if (runState == null || baseCoins <= 0)
            {
                return false;
            }

            return TryGrant(request, result, () => runState.Currency.AddCoins(baseCoins));
        }

        private static int SaturatingAdd(int a, int b)
        {
            long sum = (long)a + b;
            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }

            return sum < 0 ? 0 : (int)sum;
        }
    }
}
