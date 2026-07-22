using UnityEngine;

namespace LastTrain.Difficulty
{
    public enum DifficultyModifierKind
    {
        Custom = 0,
        ReducedSellPrice = 1,
        ReducedPreparationTime = 2,
        ExtraEnemyWave = 3,
        DualSpawnLanes = 4,
        ReducedHeal = 5,
        EscalatingEnemies = 6,
        PowerOutage = 7,
    }

    /// <summary>난이도 전용 특수 규칙 정의. 런타임에는 IDifficultyModifier로 해석된다.</summary>
    [CreateAssetMenu(fileName = "DifficultyModifier_", menuName = "Last Train/Difficulty Modifier Data")]
    public sealed class DifficultyModifierData : ScriptableObject, Data.IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private DifficultyModifierKind modifierKind = DifficultyModifierKind.Custom;
        [SerializeField] private float magnitude = 1f;
        [SerializeField] private int stationIndexMin = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public DifficultyModifierKind ModifierKind => modifierKind;
        public float Magnitude => magnitude;
        public int StationIndexMin => Mathf.Max(1, stationIndexMin);
    }
}
