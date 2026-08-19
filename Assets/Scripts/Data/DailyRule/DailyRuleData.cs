using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>오늘의 막차 일일 규칙 정적 데이터.</summary>
    [CreateAssetMenu(fileName = "DailyRule_", menuName = "Last Train/Daily Rule Data")]
    public sealed class DailyRuleData : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private DailyRuleKind kind;
        [SerializeField] private float magnitude = 1f;
        [SerializeField] private string targetId = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public DailyRuleKind Kind => kind;
        public float Magnitude => magnitude;
        public string TargetId => targetId ?? string.Empty;
    }
}
