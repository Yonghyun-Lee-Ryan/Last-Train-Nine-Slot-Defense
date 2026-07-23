using System;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Mission;
using UnityEngine;

namespace LastTrain.LiveOps
{
    public enum EndedRewardPolicy
    {
        /// <summary>종료 후 미수령 보상은 소멸한다.</summary>
        ForfeitUnclaimed = 0,
        /// <summary>종료 후에도 수령 창이 열려 있으면 수령 가능.</summary>
        ClaimUntilExpiry = 1,
    }

    [CreateAssetMenu(fileName = "LiveEvent_", menuName = "LastTrain/LiveOps/Live Event Data")]
    public sealed class LiveEventData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "event_heatwave";
        [SerializeField] private string displayName = "폭염 막차";
        [SerializeField] private string themeId = "heatwave";

        [Header("Schedule (UTC ISO-8601)")]
        [SerializeField] private string startUtc = "2026-01-01T00:00:00Z";
        [SerializeField] private string endUtc = "2026-01-08T00:00:00Z";
        [SerializeField] private string claimExpiryUtc = "";

        [Header("Content Hooks")]
        [SerializeField] private RouteData eventRoute;
        [SerializeField] private DifficultyData eventDifficulty;
        [SerializeField] private DifficultyModifierData[] eventModifiers = Array.Empty<DifficultyModifierData>();
        [SerializeField] private string[] boostedPassengerIds = Array.Empty<string>();
        [SerializeField] private string[] restrictedPassengerIds = Array.Empty<string>();
        [SerializeField] private float boostedPassengerAttackMultiplier = 1.2f;
        [SerializeField] private MissionData[] eventMissions = Array.Empty<MissionData>();
        [SerializeField] private EventCurrencyData eventCurrency;
        [SerializeField] private EventRewardTrack rewardTrack;
        [SerializeField] private int dailyCurrencyCap = 500;
        [SerializeField] private EndedRewardPolicy endedRewardPolicy = EndedRewardPolicy.ForfeitUnclaimed;

        public string Id => id;
        public string DisplayName => displayName;
        public string ThemeId => themeId;
        public string StartUtc => startUtc;
        public string EndUtc => endUtc;
        public string ClaimExpiryUtc => claimExpiryUtc;
        public RouteData EventRoute => eventRoute;
        public DifficultyData EventDifficulty => eventDifficulty;
        public DifficultyModifierData[] EventModifiers => eventModifiers ?? Array.Empty<DifficultyModifierData>();
        public string[] BoostedPassengerIds => boostedPassengerIds ?? Array.Empty<string>();
        public string[] RestrictedPassengerIds => restrictedPassengerIds ?? Array.Empty<string>();
        public float BoostedPassengerAttackMultiplier => Mathf.Max(0.01f, boostedPassengerAttackMultiplier);
        public MissionData[] EventMissions => eventMissions ?? Array.Empty<MissionData>();
        public EventCurrencyData EventCurrency => eventCurrency;
        public EventRewardTrack RewardTrack => rewardTrack;
        public int DailyCurrencyCap => Mathf.Max(0, dailyCurrencyCap);
        public EndedRewardPolicy EndedRewardPolicy => endedRewardPolicy;

        public bool TryGetSchedule(out DateTime start, out DateTime end)
        {
            start = default;
            end = default;
            return DateTime.TryParse(
                       startUtc,
                       null,
                       System.Globalization.DateTimeStyles.RoundtripKind,
                       out start)
                   && DateTime.TryParse(
                       endUtc,
                       null,
                       System.Globalization.DateTimeStyles.RoundtripKind,
                       out end);
        }

        public bool IsPassengerAllowed(string passengerId)
        {
            if (string.IsNullOrWhiteSpace(passengerId))
            {
                return false;
            }

            string[] restricted = RestrictedPassengerIds;
            if (restricted.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < restricted.Length; i++)
            {
                if (string.Equals(restricted[i], passengerId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPassengerBoosted(string passengerId)
        {
            string[] boosted = BoostedPassengerIds;
            for (int i = 0; i < boosted.Length; i++)
            {
                if (string.Equals(boosted[i], passengerId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
