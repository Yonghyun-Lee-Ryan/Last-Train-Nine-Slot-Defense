using System;

namespace LastTrain.Enemy
{
    /// <summary>체력 비율에 따른 보스 단계 전환.</summary>
    public sealed class BossPhaseController
    {
        public const float EnrageHealthRatio = 0.3f;

        public BossPhase Current { get; private set; } = BossPhase.Normal;

        public event Action<BossPhase, BossPhase> PhaseChanged;

        public void NotifyHealth(float current, float max)
        {
            BossPhase next = max > 0f && current / max <= EnrageHealthRatio
                ? BossPhase.Enraged
                : BossPhase.Normal;

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
    }
}
