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

        /// <summary>객차 파괴 직후 부활 광고를 띄울 수 있을 때 발생한다.</summary>
        public event Action ReviveOffered;

        public RunState RunState { get; private set; }
        public RunResult LastResult { get; private set; }
        public bool IsPendingDefeat { get; private set; }
        public bool ReviveAvailableThisRun { get; private set; } = true;

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
            IsPendingDefeat = false;
            ReviveAvailableThisRun = true;
            RunStarted?.Invoke(RunState);
            return RunState;
        }

        /// <summary>회차를 종료하고 RunResult를 생성한다. 이미 종료된 경우 LastResult를 반환한다.</summary>
        public RunResult EndRun(RunEndReason reason, bool isVictory)
        {
            if (!HasActiveRun)
            {
                return LastResult;
            }

            IsPendingDefeat = false;
            UnsubscribeTrainDestroyed();

            RunResult result = RunState.BuildResult(reason, isVictory);
            RunState.Battle.EndRun(reason);
            LastResult = result;
            RunEnded?.Invoke(result);
            RunState.Dispose();
            RunState = null;
            return result;
        }

        /// <summary>RunState 참조를 해제한다. 종료 처리 없이 초기화할 때 사용한다.</summary>
        public void ClearRun()
        {
            IsPendingDefeat = false;
            UnsubscribeTrainDestroyed();
            RunState?.Dispose();
            RunState = null;
        }

        public void ClearPendingDefeat()
        {
            IsPendingDefeat = false;
        }

        public void MarkReviveUsed()
        {
            ReviveAvailableThisRun = false;
        }

        /// <summary>부활을 거절하고 패배로 종료한다.</summary>
        public void DeclineReviveAndEnd()
        {
            if (!IsPendingDefeat)
            {
                return;
            }

            EndRun(RunEndReason.Defeat, isVictory: false);
        }

        /// <summary>부활 가능 여부를 외부(광고 한도)와 함께 판단할 때 사용한다.</summary>
        public void ConfigureReviveOffer(bool canOffer)
        {
            if (!IsPendingDefeat)
            {
                return;
            }

            if (canOffer && ReviveAvailableThisRun)
            {
                ReviveOffered?.Invoke();
                return;
            }

            EndRun(RunEndReason.Defeat, isVictory: false);
        }

        private void HandleTrainDestroyed()
        {
            if (!HasActiveRun || IsPendingDefeat)
            {
                return;
            }

            IsPendingDefeat = true;
            // AppRoot/UI가 광고 한도를 보고 ConfigureReviveOffer를 호출한다.
            // 구독자가 없으면 즉시 패배 처리한다.
            if (ReviveOffered == null)
            {
                EndRun(RunEndReason.Defeat, isVictory: false);
                return;
            }

            ReviveOffered.Invoke();
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
