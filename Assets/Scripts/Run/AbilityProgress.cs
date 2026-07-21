using System;
using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Run
{
    /// <summary>회차 내 능력 카드 선택·후보·리롤 진행 상태.</summary>
    public sealed class AbilityProgress
    {
        public event Action OffersChanged;
        public event Action SelectionChanged;

        private readonly List<AbilityData> _selected = new();
        private readonly List<AbilityData> _currentOffers = new();
        private readonly Dictionary<string, int> _stackCounts = new();

        public int FreeRerollsUsed { get; private set; }
        public int AdRerollsUsed { get; private set; }
        public bool HasActiveOffers => _currentOffers.Count > 0;
        public bool IsSelectingReward { get; private set; }
        public IReadOnlyList<AbilityData> Selected => _selected;
        public IReadOnlyList<AbilityData> CurrentOffers => _currentOffers;
        public AbilityModifiers Modifiers { get; private set; } = AbilityModifiers.Empty;

        public void Reset()
        {
            FreeRerollsUsed = 0;
            AdRerollsUsed = 0;
            IsSelectingReward = false;
            _selected.Clear();
            _stackCounts.Clear();
            Modifiers = AbilityModifiers.Empty;
            ClearOffers();
            SelectionChanged?.Invoke();
        }

        public void BeginRewardSelection()
        {
            IsSelectingReward = true;
        }

        public void EndRewardSelection()
        {
            IsSelectingReward = false;
            ClearOffers();
        }

        public int GetStackCount(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return 0;
            }

            return _stackCounts.TryGetValue(abilityId, out int count) ? count : 0;
        }

        public bool CanSelect(AbilityData ability)
        {
            if (ability == null || string.IsNullOrWhiteSpace(ability.Id))
            {
                return false;
            }

            int stacks = GetStackCount(ability.Id);
            if (stacks <= 0)
            {
                return true;
            }

            if (!ability.AllowDuplicate)
            {
                return false;
            }

            return stacks < Math.Max(1, ability.MaxStack);
        }

        public void AddSelected(AbilityData ability)
        {
            if (ability == null || string.IsNullOrWhiteSpace(ability.Id))
            {
                throw new ArgumentException("ability가 유효하지 않습니다.", nameof(ability));
            }

            if (!CanSelect(ability))
            {
                throw new InvalidOperationException($"능력 '{ability.Id}'를 더 이상 선택할 수 없습니다.");
            }

            if (!_stackCounts.ContainsKey(ability.Id))
            {
                _selected.Add(ability);
                _stackCounts[ability.Id] = 1;
            }
            else
            {
                _stackCounts[ability.Id]++;
            }

            SelectionChanged?.Invoke();
        }

        public void SetModifiers(AbilityModifiers modifiers)
        {
            Modifiers = modifiers ?? AbilityModifiers.Empty;
        }

        public void RecordFreeReroll()
        {
            FreeRerollsUsed++;
        }

        public void RecordAdReroll()
        {
            AdRerollsUsed++;
        }

        public void SetOffers(IReadOnlyList<AbilityData> offers)
        {
            _currentOffers.Clear();
            if (offers != null)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i] != null)
                    {
                        _currentOffers.Add(offers[i]);
                    }
                }
            }

            OffersChanged?.Invoke();
        }

        public void ClearOffers()
        {
            if (_currentOffers.Count == 0)
            {
                return;
            }

            _currentOffers.Clear();
            OffersChanged?.Invoke();
        }

        public AbilityData GetOffer(int index)
        {
            if (index < 0 || index >= _currentOffers.Count)
            {
                return null;
            }

            return _currentOffers[index];
        }

        /// <summary>선택 목록을 스택 수만큼 펼친다(효과 합산용).</summary>
        public List<AbilityData> ExpandSelectedWithStacks()
        {
            var expanded = new List<AbilityData>();
            for (int i = 0; i < _selected.Count; i++)
            {
                AbilityData ability = _selected[i];
                int stacks = GetStackCount(ability.Id);
                for (int s = 0; s < stacks; s++)
                {
                    expanded.Add(ability);
                }
            }

            return expanded;
        }
    }
}
