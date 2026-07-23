using System;
using System.Collections.Generic;
using LastTrain.Analytics;
using LastTrain.Core;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.Tutorial
{
    /// <summary>튜토리얼 상태 머신. 실제 게임 이벤트를 기다려 단계를 진행한다.</summary>
    public sealed class TutorialStateMachine
    {
        public event Action<TutorialStepData> StepStarted;
        public event Action<TutorialStepData> StepCompleted;
        public event Action Completed;
        public event Action Skipped;

        private readonly IReadOnlyList<TutorialStepData> _steps;
        private int _index = -1;
        private bool _active;
        private bool _finished;

        public TutorialStateMachine(IReadOnlyList<TutorialStepData> steps)
        {
            _steps = steps ?? Array.Empty<TutorialStepData>();
        }

        public bool IsActive => _active && !_finished;
        public bool IsFinished => _finished;
        public int CurrentIndex => _index;
        public TutorialStepData CurrentStep =>
            _index >= 0 && _index < _steps.Count ? _steps[_index] : null;

        public TutorialInputMask AllowedInputs =>
            CurrentStep != null ? CurrentStep.AllowedInputs : TutorialInputMask.All;

        public bool Allows(TutorialInputMask mask)
        {
            if (!IsActive)
            {
                return true;
            }

            return (AllowedInputs & mask) != 0;
        }

        public void StartOrResume(MetaSaveData meta)
        {
            if (meta == null || _steps.Count == 0)
            {
                return;
            }

            meta.EnsureDefaults();
            if (meta.tutorialCompleted || meta.tutorialSkipped)
            {
                _finished = true;
                _active = false;
                return;
            }

            _finished = false;
            _active = true;
            _index = Mathf.Clamp(meta.tutorialStepIndex, 0, _steps.Count - 1);
            Track(AnalyticsEventNames.TutorialStarted, new Dictionary<string, object>
            {
                ["step_index"] = _index,
                ["step_id"] = CurrentStep?.Id ?? string.Empty,
            });
            StepStarted?.Invoke(CurrentStep);
        }

        public void Notify(TutorialWaitEvent evt)
        {
            if (!IsActive || CurrentStep == null)
            {
                return;
            }

            TutorialWaitEvent expected = CurrentStep.WaitEvent;
            if (expected == TutorialWaitEvent.None)
            {
                return;
            }

            if (expected != evt && expected != TutorialWaitEvent.Acknowledge)
            {
                return;
            }

            // Acknowledge는 명시적 Advance/Acknowledge만 허용
            if (expected == TutorialWaitEvent.Acknowledge && evt != TutorialWaitEvent.Acknowledge)
            {
                return;
            }

            CompleteCurrentStep();
        }

        public void Acknowledge()
        {
            if (!IsActive || CurrentStep == null)
            {
                return;
            }

            if (CurrentStep.WaitEvent == TutorialWaitEvent.Acknowledge
                || (CurrentStep.AllowedInputs & TutorialInputMask.Acknowledge) != 0)
            {
                if (CurrentStep.WaitEvent == TutorialWaitEvent.Acknowledge)
                {
                    CompleteCurrentStep();
                }
            }
        }

        public void SkipAll(MetaSaveData meta)
        {
            if (meta == null || _finished)
            {
                return;
            }

            meta.tutorialSkipped = true;
            meta.tutorialCompleted = true;
            meta.tutorialStepIndex = _steps.Count;
            _finished = true;
            _active = false;
            Skipped?.Invoke();
            Track(AnalyticsEventNames.TutorialCompleted, new Dictionary<string, object>
            {
                ["skipped"] = true,
            });
        }

        public void Restart(MetaSaveData meta)
        {
            if (meta == null || _steps.Count == 0)
            {
                return;
            }

            meta.tutorialCompleted = false;
            meta.tutorialSkipped = false;
            meta.tutorialStepIndex = 0;
            _finished = false;
            _active = true;
            _index = 0;
            Track(AnalyticsEventNames.TutorialStarted, new Dictionary<string, object>
            {
                ["restart"] = true,
                ["step_index"] = 0,
            });
            StepStarted?.Invoke(CurrentStep);
        }

        private void CompleteCurrentStep()
        {
            TutorialStepData step = CurrentStep;
            StepCompleted?.Invoke(step);
            Track(AnalyticsEventNames.TutorialStepCompleted, new Dictionary<string, object>
            {
                ["step_index"] = _index,
                ["step_id"] = step?.Id ?? string.Empty,
                ["step_kind"] = step != null ? step.StepKind.ToString() : string.Empty,
            });

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            _index++;
            if (_index >= _steps.Count)
            {
                meta.tutorialCompleted = true;
                meta.tutorialStepIndex = _steps.Count;
                MetaSaveSystem.Save(meta);
                _finished = true;
                _active = false;
                Completed?.Invoke();
                Track(AnalyticsEventNames.TutorialCompleted, new Dictionary<string, object>
                {
                    ["skipped"] = false,
                });
                return;
            }

            meta.tutorialStepIndex = _index;
            MetaSaveSystem.Save(meta);
            StepStarted?.Invoke(CurrentStep);
        }

        private static void Track(string eventName, IDictionary<string, object> extra)
        {
            AppRoot.Instance?.Analytics?.Track(eventName, extra);
        }
    }
}
