using System;

namespace LastTrain.Run
{
    /// <summary>전투 진행 상태. 개발 단위 7에서 상태 전환을 사용한다.</summary>
    public sealed class BattleState
    {
        public event Action<RunPhase> PhaseChanged;

        public RunPhase CurrentPhase { get; private set; } = RunPhase.None;
        public bool IsRunActive { get; private set; }
        public RunEndReason EndReason { get; private set; } = RunEndReason.None;

        public void StartRun()
        {
            IsRunActive = true;
            EndReason = RunEndReason.None;
            SetPhase(RunPhase.Preparing);
        }

        public void SetPhase(RunPhase phase)
        {
            if (CurrentPhase == phase)
            {
                return;
            }

            CurrentPhase = phase;
            PhaseChanged?.Invoke(phase);
        }

        public void EndRun(RunEndReason reason)
        {
            IsRunActive = false;
            EndReason = reason;
            SetPhase(RunPhase.RunEnded);
        }
    }
}
