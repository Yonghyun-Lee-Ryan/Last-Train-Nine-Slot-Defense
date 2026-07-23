using LastTrain.Data;
using UnityEngine;

namespace LastTrain.Mission
{
    /// <summary>일일/주간 미션 정적 데이터.</summary>
    [CreateAssetMenu(fileName = "Mission_", menuName = "Last Train/Mission Data")]
    public sealed class MissionData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private MissionPeriod period = MissionPeriod.Daily;

        [Header("Condition")]
        [SerializeField] private MissionCondition condition = new();

        [Header("Reward")]
        [SerializeField] private int rewardTicketFragments = 10;
        [SerializeField] private int rewardAccountXp = 20;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public MissionPeriod Period => period;
        public MissionCondition Condition => condition ?? new MissionCondition();
        public int RewardTicketFragments => Mathf.Max(0, rewardTicketFragments);
        public int RewardAccountXp => Mathf.Max(0, rewardAccountXp);

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[MissionData] '{name}' ID가 비어 있습니다.", this);
            }

            if (condition == null || condition.Type == MissionConditionType.None)
            {
                Debug.LogWarning($"[MissionData] '{id}' 조건이 None입니다.", this);
            }
        }

#if UNITY_EDITOR
        public void EditorSet(
            string newId,
            string newDisplayName,
            string newDescription,
            MissionPeriod newPeriod,
            MissionCondition newCondition,
            int tickets,
            int xp)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            period = newPeriod;
            condition = newCondition;
            rewardTicketFragments = tickets;
            rewardAccountXp = xp;
        }
#endif
    }
}
