using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 능력 카드 정적 데이터.
    /// 역 완료 후 후보로 등장하며 RunState에 적용된다(개발 단위 11).
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_", menuName = "Last Train/Ability Data")]
    public class AbilityData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Rarity rarity = Rarity.Common;

        [Header("Effect")]
        [SerializeField] private AbilityEffectType effectType;
        [SerializeField] private float effectValue;

        [Tooltip("특정 승객 대상 효과일 때 PassengerData ID")]
        [SerializeField] private string targetPassengerId;

        [Header("Stacking")]
        [SerializeField] private bool allowDuplicate = true;
        [SerializeField] private int maxStack = 99;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Rarity Rarity => rarity;
        public AbilityEffectType EffectType => effectType;
        public float EffectValue => effectValue;
        public string TargetPassengerId => targetPassengerId;
        public bool AllowDuplicate => allowDuplicate;
        public int MaxStack => maxStack;

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[AbilityData] '{name}' ID가 비어 있습니다.", this);
            }

            if (effectType == AbilityEffectType.None)
            {
                Debug.LogWarning($"[AbilityData] '{id}' effectType이 None입니다.", this);
            }

            if (maxStack < 1)
            {
                Debug.LogWarning($"[AbilityData] '{id}' maxStack은 1 이상이어야 합니다.", this);
            }
        }
    }
}
