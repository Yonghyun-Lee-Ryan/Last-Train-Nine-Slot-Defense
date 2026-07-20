using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 유물 정적 데이터. 한 회차 동안 지속되는 특수 효과.
    /// </summary>
    [CreateAssetMenu(fileName = "Relic_", menuName = "Last Train/Relic Data")]
    public class RelicData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Rarity rarity = Rarity.Common;

        [Header("Effect")]
        [SerializeField] private RelicEffectType effectType;
        [SerializeField] private float effectValue;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Rarity Rarity => rarity;
        public RelicEffectType EffectType => effectType;
        public float EffectValue => effectValue;

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[RelicData] '{name}' ID가 비어 있습니다.", this);
            }

            if (effectType == RelicEffectType.None)
            {
                Debug.LogWarning($"[RelicData] '{id}' effectType이 None입니다.", this);
            }
        }
    }
}
