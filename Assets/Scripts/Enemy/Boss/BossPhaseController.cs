using System;
using LastTrain.Data;

namespace LastTrain.Enemy
{
    /// <summary>체력 비율과 데이터 임계값에 따른 보스 단계 전환.</summary>
    public sealed class BossPhaseController
    {
        public const float LegacyEnrageHealthRatio = 0.3f;

        private BossPhaseThresholds _thresholds;

        public BossPhase Current { get; private set; } = BossPhase.Normal;

        public event Action<BossPhase, BossPhase> PhaseChanged;

        public void Configure(BossPhaseThresholds thresholds)
        {
            _thresholds = thresholds.EnrageHealthRatio > 0f
                ? thresholds
                : BossPhaseThresholds.Create(thresholds.DoorOpenHealthRatio, LegacyEnrageHealthRatio);
        }

        public void NotifyHealth(float current, float max)
        {
            BossPhase next = ResolvePhase(max > 0f ? current / max : 0f);
            if (next == Current)
            {
                return;
            }

            BossPhase previous = Current;
            Current = next;
            PhaseChanged?.Invoke(previous, next);
        }

        public void Reset()
        {
            Current = BossPhase.Normal;
        }

        private BossPhase ResolvePhase(float healthRatio)
        {
            float enrage = _thresholds.EnrageHealthRatio > 0f
                ? _thresholds.EnrageHealthRatio
                : LegacyEnrageHealthRatio;
            if (healthRatio <= enrage)
            {
                return BossPhase.Enraged;
            }

            float doorOpen = _thresholds.DoorOpenHealthRatio;
            if (doorOpen > 0f && healthRatio <= doorOpen)
            {
                return BossPhase.DoorOpen;
            }

            return BossPhase.Normal;
        }
    }
}
