using System;
using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Run
{
    /// <summary>회차 내 활성 시너지·카탈로그·수정치.</summary>
    public sealed class SynergyProgress
    {
        public event Action Changed;

        private readonly List<SynergyData> _catalog = new();
        private readonly List<SynergyData> _active = new();

        public IReadOnlyList<SynergyData> Catalog => _catalog;
        public IReadOnlyList<SynergyData> Active => _active;
        public SynergyModifiers Modifiers { get; private set; } = SynergyModifiers.Empty;

        public void Reset()
        {
            _catalog.Clear();
            _active.Clear();
            Modifiers = SynergyModifiers.Empty;
            Changed?.Invoke();
        }

        public void SetCatalog(IReadOnlyList<SynergyData> catalog)
        {
            _catalog.Clear();
            if (catalog == null)
            {
                return;
            }

            var seen = new HashSet<string>();
            for (int i = 0; i < catalog.Count; i++)
            {
                SynergyData data = catalog[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id) || !seen.Add(data.Id))
                {
                    continue;
                }

                _catalog.Add(data);
            }
        }

        public void SetActive(IReadOnlyList<SynergyData> active, SynergyModifiers modifiers)
        {
            _active.Clear();
            if (active != null)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    if (active[i] != null)
                    {
                        _active.Add(active[i]);
                    }
                }
            }

            Modifiers = modifiers ?? SynergyModifiers.Empty;
            Changed?.Invoke();
        }

        public bool IsActive(string synergyId)
        {
            if (string.IsNullOrWhiteSpace(synergyId))
            {
                return false;
            }

            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Id == synergyId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
