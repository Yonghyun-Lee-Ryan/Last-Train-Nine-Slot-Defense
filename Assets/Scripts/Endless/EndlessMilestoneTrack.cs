using System;
using UnityEngine;

namespace LastTrain.Endless
{
    [Serializable]
    public sealed class EndlessMilestoneStep
    {
        public string id = string.Empty;
        public int requiredStation;
        public int requiredScore;
        public int ticketFragments;
        public int accountXp;
    }

    [CreateAssetMenu(fileName = "EndlessMilestoneTrack", menuName = "Last Train/Endless Milestone Track")]
    public sealed class EndlessMilestoneTrack : ScriptableObject
    {
        [SerializeField] private EndlessMilestoneStep[] steps = Array.Empty<EndlessMilestoneStep>();

        public EndlessMilestoneStep[] Steps => steps ?? Array.Empty<EndlessMilestoneStep>();
    }

    public static class EndlessMilestoneCatalog
    {
        public const string ResourcesName = "Endless/EndlessMilestoneTrack";
        public const string AssetPath = "Assets/Data/Endless/EndlessMilestoneTrack.asset";

        public static EndlessMilestoneTrack Load()
        {
            return Resources.Load<EndlessMilestoneTrack>(ResourcesName);
        }
    }
}
