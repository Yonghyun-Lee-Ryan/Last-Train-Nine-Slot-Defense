using System;
using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Relic
{
    public sealed class RelicProgress
    {
        public event Action Changed;

        private readonly List<RelicRuntime> _owned = new();

        public IReadOnlyList<RelicRuntime> Owned => _owned;
        public RelicModifiers Modifiers { get; private set; } = RelicModifiers.Empty;
        public bool EmergencyAutoHealUsed { get; private set; }

        public void Reset()
        {
            _owned.Clear();
            EmergencyAutoHealUsed = false;
            Recompute();
        }

        public bool HasRelic(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                return false;
            }

            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i]?.Id == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAdd(RelicData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id) || HasRelic(data.Id))
            {
                return false;
            }

            _owned.Add(new RelicRuntime(data));
            Recompute();
            return true;
        }

        public void MarkEmergencyAutoHealUsed()
        {
            EmergencyAutoHealUsed = true;
            Recompute();
        }

        /// <summary>객차 파괴 직전 응급 회복 유물을 시도한다.</summary>
        public bool TryTriggerEmergencyHeal(TrainState train)
        {
            if (train == null
                || EmergencyAutoHealUsed
                || Modifiers.EmergencyAutoHealFlat <= 0
                || train.CurrentHp > 0)
            {
                return false;
            }

            train.Heal(Modifiers.EmergencyAutoHealFlat);
            MarkEmergencyAutoHealUsed();
            return train.CurrentHp > 0;
        }

        public void Restore(string[] relicIds, bool emergencyAutoHealUsed, GameDatabase database)
        {
            _owned.Clear();
            EmergencyAutoHealUsed = emergencyAutoHealUsed;
            if (relicIds != null && database != null)
            {
                for (int i = 0; i < relicIds.Length; i++)
                {
                    string id = relicIds[i];
                    if (!string.IsNullOrWhiteSpace(id) && database.TryGetRelic(id, out RelicData data))
                    {
                        _owned.Add(new RelicRuntime(data));
                    }
                }
            }

            Recompute();
        }

        public string[] ToIdArray()
        {
            var ids = new string[_owned.Count];
            for (int i = 0; i < _owned.Count; i++)
            {
                ids[i] = _owned[i]?.Id ?? string.Empty;
            }

            return ids;
        }

        private void Recompute()
        {
            Modifiers = RelicEffectAggregator.Compute(_owned);
            Modifiers.EmergencyAutoHealUsed = EmergencyAutoHealUsed;
            Changed?.Invoke();
        }
    }

    public sealed class RelicManager
    {
        private readonly RunState _runState;
        private readonly GameDatabase _database;

        public RelicManager(RunState runState, GameDatabase database)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _database = database;
        }

        public RelicProgress Progress => _runState.Relics;

        public bool HasRelic(string relicId) => _runState.Relics.HasRelic(relicId);

        public bool TryAcquire(string relicId)
        {
            if (_database == null
                || string.IsNullOrWhiteSpace(relicId)
                || !_database.TryGetRelic(relicId, out RelicData data))
            {
                return false;
            }

            if (!_runState.Relics.TryAdd(data))
            {
                return false;
            }

            ApplyPersistentEffects();
            return true;
        }

        public void ApplyPersistentEffects()
        {
            RelicModifiers relicMods = _runState.Relics.Modifiers;
            int targetMaxHp = Math.Max(1, _runState.BaseTrainMaxHp + relicMods.TrainMaxHpFlat);
            int previousMax = _runState.Train.MaxHp;
            _runState.Train.SetMaxHp(targetMaxHp, healToFull: false);
            if (targetMaxHp > previousMax)
            {
                _runState.Train.Heal(targetMaxHp - previousMax);
            }

            AbilityEffectApplier.RefreshPassengerBuffs(_runState);
        }

        public void TryTriggerEmergencyHeal()
        {
            _runState.Relics.TryTriggerEmergencyHeal(_runState.Train);
        }
    }
}
