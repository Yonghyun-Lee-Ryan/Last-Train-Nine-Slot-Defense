using System;
using LastTrain.Run;

namespace LastTrain.Core
{
    /// <summary>
    /// 현재 게임 세션을 관리한다. 한 번에 하나의 RunState만 활성화한다.
    /// MonoBehaviour에 의존하지 않으며 AppRoot 또는 Game Scene에서 소유한다.
    /// </summary>
    public sealed class GameSession
    {
        public event Action<RunState> RunStarted;
        public event Action<RunResult> RunEnded;

        public RunState RunState { get; private set; }
        public RunResult LastResult { get; private set; }

        public bool HasActiveRun => RunState != null && RunState.Battle != null && RunState.Battle.IsRunActive;

        /// <summary>새 회차를 시작한다. 기존 활성 회차가 있으면 먼저 종료한다.</summary>
        public RunState StartNewRun(RunStartConfig config = null)
        {
            if (HasActiveRun)
            {
                EndRun(RunEndReason.Abandoned, isVictory: false);
            }
            else
            {
                RunState?.Dispose();
            }

            config ??= RunStartConfig.CreateDefault();
            RunState = new RunState();
            RunState.Initialize(config);
            RunState.Train.Destroyed += HandleTrainDestroyed;
            RunState.Battle.StartRun();

            LastResult = null;
            RunStarted?.Invoke(RunState);
            return RunState;
        }

        /// <summary>회차를 종료하고 RunResult를 생성한다.</summary>
        public RunResult EndRun(RunEndReason reason, bool isVictory)
        {
            if (RunState == null)
            {
                throw new InvalidOperationException("활성 RunState가 없어 종료할 수 없습니다.");
            }

            UnsubscribeTrainDestroyed();

            RunResult result = RunState.BuildResult(reason, isVictory);
            RunState.Battle.EndRun(reason);
            LastResult = result;
            RunEnded?.Invoke(result);
            RunState.Dispose();
            return result;
        }

        /// <summary>RunState 참조를 해제한다. 종료 처리 없이 초기화할 때 사용한다.</summary>
        public void ClearRun()
        {
            UnsubscribeTrainDestroyed();
            RunState?.Dispose();
            RunState = null;
        }

        private void HandleTrainDestroyed()
        {
            if (!HasActiveRun)
            {
                return;
            }

            EndRun(RunEndReason.Defeat, isVictory: false);
        }

        private void UnsubscribeTrainDestroyed()
        {
            if (RunState?.Train != null)
            {
                RunState.Train.Destroyed -= HandleTrainDestroyed;
            }
        }
    }
}
