using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Synergy
{
    /// <summary>
    /// 시너지 재계산·활성 이벤트.
    /// 배치/합성/판매 훅에서는 SynergyEffectApplier.Refresh를 직접 호출해도 된다.
    /// </summary>
    public sealed class SynergyManager
    {
        public event Action<IReadOnlyList<SynergyData>> ActiveSynergiesChanged;
        public event Action<SynergyData> SynergyActivated;
        public event Action<SynergyData> SynergyDeactivated;

        private readonly RunState _runState;
        private readonly HashSet<string> _previousActiveIds = new();

        public SynergyManager(RunState runState, IReadOnlyList<SynergyData> catalog)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            if (_runState.Synergies == null)
            {
                throw new InvalidOperationException("RunState.Synergies가 초기화되지 않았습니다.");
            }

            _runState.Synergies.SetCatalog(catalog);
            CapturePrevious();
        }

        public IReadOnlyList<SynergyData> ActiveSynergies => _runState.Synergies.Active;
        public SynergyModifiers Modifiers => _runState.Synergies.Modifiers;

        public void Recalculate()
        {
            SynergyEffectApplier.Refresh(_runState);
            EmitDiffEvents();
            ActiveSynergiesChanged?.Invoke(_runState.Synergies.Active);
            CapturePrevious();
        }

        private void EmitDiffEvents()
        {
            IReadOnlyList<SynergyData> active = _runState.Synergies.Active;
            var currentIds = new HashSet<string>();

            for (int i = 0; i < active.Count; i++)
            {
                SynergyData data = active[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                currentIds.Add(data.Id);
                if (!_previousActiveIds.Contains(data.Id))
                {
                    SynergyActivated?.Invoke(data);
                }
            }

            foreach (string previousId in _previousActiveIds)
            {
                if (currentIds.Contains(previousId))
                {
                    continue;
                }

                for (int i = 0; i < _runState.Synergies.Catalog.Count; i++)
                {
                    SynergyData data = _runState.Synergies.Catalog[i];
                    if (data != null && data.Id == previousId)
                    {
                        SynergyDeactivated?.Invoke(data);
                        break;
                    }
                }
            }
        }

        private void CapturePrevious()
        {
            _previousActiveIds.Clear();
            IReadOnlyList<SynergyData> active = _runState.Synergies.Active;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i] != null && !string.IsNullOrWhiteSpace(active[i].Id))
                {
                    _previousActiveIds.Add(active[i].Id);
                }
            }
        }
    }
}
