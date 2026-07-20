using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 승객 조합 시너지 정적 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "Synergy_", menuName = "Last Train/Synergy Data")]
    public class SynergyData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Condition")]
        [Tooltip("필요 태그. Flags 조합으로 여러 태그를 요구할 수 있다.")]
        [SerializeField] private PassengerTag requiredTags;

        [Tooltip("requiredTags에 해당하는 승객 수")]
        [SerializeField] private int requiredCount = 2;

        [Tooltip("서로 다른 승객 종류 수 조건 (다양성 시너지 등). 0이면 무시")]
        [SerializeField] private int requiredUniquePassengerCount;

        [Header("Effect")]
        [SerializeField] private SynergyEffectType effectType;
        [SerializeField] private float effectValue;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public PassengerTag RequiredTags => requiredTags;
        public int RequiredCount => requiredCount;
        public int RequiredUniquePassengerCount => requiredUniquePassengerCount;
        public SynergyEffectType EffectType => effectType;
        public float EffectValue => effectValue;

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[SynergyData] '{name}' ID가 비어 있습니다.", this);
            }

            if (requiredTags == PassengerTag.None && requiredUniquePassengerCount <= 0)
            {
                Debug.LogWarning($"[SynergyData] '{id}' 활성 조건(태그 또는 고유 승객 수)이 없습니다.", this);
            }

            if (requiredCount < 1 && requiredUniquePassengerCount < 1)
            {
                Debug.LogWarning($"[SynergyData] '{id}' requiredCount/requiredUniquePassengerCount가 유효하지 않습니다.", this);
            }

            if (effectType == SynergyEffectType.None)
            {
                Debug.LogWarning($"[SynergyData] '{id}' effectType이 None입니다.", this);
            }
        }
    }
}
