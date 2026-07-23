using LastTrain.Data;
using LastTrain.Difficulty;
using UnityEngine;

namespace LastTrain.Difficulty.Modifiers
{
    /// <summary>깊이 구간마다 적 체력 보너스를 누적한다 (무한 모드용).</summary>
    public sealed class EscalatingEnemiesModifier : IDifficultyModifier
    {
        private readonly DifficultyModifierData _data;

        public EscalatingEnemiesModifier(DifficultyModifierData data)
        {
            _data = data;
        }

        public string ModifierId => _data != null ? _data.Id : "escalating_enemies";

        public void OnRunStarted(DifficultyModifierContext context)
        {
            Apply(context);
        }

        public void OnStationStarted(DifficultyModifierContext context, StationData station)
        {
            Apply(context);
        }

        public void Tick(float deltaTime, DifficultyModifierContext context)
        {
        }

        private void Apply(DifficultyModifierContext context)
        {
            if (context?.RunState == null || _data == null)
            {
                return;
            }

            // magnitude: 예) 1.1 = +10% 적 체력. 활성 구간마다 덮어쓴다(누적 배율은 데이터 StationIndexMin로 단계화).
            float mult = _data.Magnitude > 0f ? _data.Magnitude : 1.1f;
            context.RunState.DifficultyModifiers.SetEnemyHealthBonusMultiplier(mult);
        }
    }
}
