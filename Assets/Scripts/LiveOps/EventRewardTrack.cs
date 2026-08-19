using System;
using UnityEngine;

namespace LastTrain.LiveOps
{
    public enum RewardTrackLane
    {
        Free = 0,
        Ad = 1,
    }

    [Serializable]
    public sealed class EventRewardStep
    {
        public string rewardId = "reward_1";
        public int requiredCurrency;
        public int ticketFragments;
        public int accountXp;
        public string unlockPassengerId = string.Empty;
        public RewardTrackLane lane = RewardTrackLane.Free;
    }

    [CreateAssetMenu(fileName = "EventRewardTrack_", menuName = "LastTrain/LiveOps/Event Reward Track")]
    public sealed class EventRewardTrack : ScriptableObject
    {
        [SerializeField] private string id = "track_default";
        [SerializeField] private EventRewardStep[] steps = Array.Empty<EventRewardStep>();

        public string Id => id;
        public EventRewardStep[] Steps => steps ?? Array.Empty<EventRewardStep>();
    }
}
