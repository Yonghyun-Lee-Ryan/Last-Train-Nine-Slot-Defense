using System;
using UnityEngine;

namespace LastTrain.Mission
{
    /// <summary>미션 조건. MissionData와 진행도(MissionProgress)를 분리한다.</summary>
    [Serializable]
    public sealed class MissionCondition
    {
        [SerializeField] private MissionConditionType type = MissionConditionType.None;
        [SerializeField] private int targetValue = 1;
        [Tooltip("승객 ID, 난이도 ID 등 조건 부가 파라미터")]
        [SerializeField] private string targetId = string.Empty;
        [Tooltip("최소 내구도 등 숫자 부가 파라미터")]
        [SerializeField] private int targetParam;

        public MissionConditionType Type => type;
        public int TargetValue => Mathf.Max(1, targetValue);
        public string TargetId => targetId ?? string.Empty;
        public int TargetParam => targetParam;

        public MissionCondition()
        {
        }

        public MissionCondition(MissionConditionType type, int targetValue, string targetId = null, int targetParam = 0)
        {
            this.type = type;
            this.targetValue = Mathf.Max(1, targetValue);
            this.targetId = targetId ?? string.Empty;
            this.targetParam = targetParam;
        }
    }
}
