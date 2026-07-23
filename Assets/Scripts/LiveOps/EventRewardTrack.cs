using System;
using UnityEngine;

namespace LastTrain.LiveOps
{
    [Serializable]
    public sealed class EventRewardStep
    {
        public string rewardId = "reward_1";
        public int requiredCurrency;
        public int ticketFragments;
        public int accountXp;
        public string unlockPassengerId = string.Empty;
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
