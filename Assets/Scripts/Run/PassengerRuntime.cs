using System;
using LastTrain.Data;

namespace LastTrain.Run
{
    /// <summary>
    /// 승객 런타임 상태. PassengerData(정적)와 분리되어 등급·쿨타임·버프를 관리한다.
    /// </summary>
    public sealed class PassengerRuntime
    {
        public PassengerRuntime(PassengerData data, int starLevel, string instanceId)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("instanceId는 비어 있을 수 없습니다.", nameof(instanceId));
            }

            Data = data;
            InstanceId = instanceId;
            StarLevel = ClampStarLevel(starLevel, data.MaxStarLevel);
        }

        public PassengerData Data { get; }
        public string InstanceId { get; }
        public int StarLevel { get; private set; }
        public float AttackCooldownRemaining { get; private set; }
        public int GridSlotIndex { get; internal set; } = -1;

        private readonly System.Collections.Generic.List<RuntimeBuff> _buffs = new();

        public System.Collections.Generic.IReadOnlyList<RuntimeBuff> Buffs => _buffs;

        public static PassengerRuntime Create(PassengerData data, int starLevel = 1)
        {
            return new PassengerRuntime(data, starLevel, Guid.NewGuid().ToString("N"));
        }

        public bool TryUpgradeStar()
        {
            if (StarLevel >= Data.MaxStarLevel)
            {
                return false;
            }

            StarLevel++;
            AttackCooldownRemaining = 0f;
            return true;
        }

        public void SetStarLevel(int starLevel)
        {
            StarLevel = ClampStarLevel(starLevel, Data.MaxStarLevel);
        }

        public void ResetAttackCooldown()
        {
            AttackCooldownRemaining = 0f;
        }

        public void SetAttackCooldownRemaining(float value)
        {
            AttackCooldownRemaining = Math.Max(0f, value);
        }

        public void TickAttackCooldown(float deltaTime)
        {
            if (AttackCooldownRemaining <= 0f)
            {
                return;
            }

            AttackCooldownRemaining = Math.Max(0f, AttackCooldownRemaining - deltaTime);
        }

        public bool IsAttackReady => AttackCooldownRemaining <= 0f && !IsAttackBlocked;

        public float AttackBlockRemaining { get; private set; }

        public bool IsAttackBlocked => AttackBlockRemaining > 0f;

        public void SetAttackBlock(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                return;
            }

            AttackBlockRemaining = Math.Max(AttackBlockRemaining, durationSeconds);
        }

        public void TickAttackBlock(float deltaTime)
        {
            if (AttackBlockRemaining <= 0f)
            {
                return;
            }

            AttackBlockRemaining = Math.Max(0f, AttackBlockRemaining - deltaTime);
        }

        public void AddBuff(RuntimeBuff buff)
        {
            if (buff == null)
            {
                return;
            }

            for (int i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].BuffId == buff.BuffId)
                {
                    _buffs[i].AddStack(buff.AttackPercentBonus, buff.AttackSpeedPercentBonus);
                    return;
                }
            }

            _buffs.Add(buff);
        }

        public void RemoveBuff(string buffId)
        {
            if (string.IsNullOrWhiteSpace(buffId))
            {
                return;
            }

            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_buffs[i].BuffId == buffId)
                {
                    _buffs.RemoveAt(i);
                }
            }
        }

        public void ClearBuffs()
        {
            _buffs.Clear();
        }

        /// <summary>등급·버프를 반영한 최종 공격력.</summary>
        public float GetEffectiveAttack()
        {
            float attack = Data.GetAttackAtStar(StarLevel);
            float bonusPercent = SumBuffValues(b => b.AttackPercentBonus);
            return attack * (1f + bonusPercent / 100f);
        }

        /// <summary>등급·버프를 반영한 최종 공격 간격(초).</summary>
        public float GetEffectiveAttackInterval()
        {
            float interval = Data.GetAttackIntervalAtStar(StarLevel);
            float bonusPercent = SumBuffValues(b => b.AttackSpeedPercentBonus);
            float multiplier = 1f + bonusPercent / 100f;
            return multiplier > 0f ? interval / multiplier : interval;
        }

        public float GetEffectiveRange()
        {
            return Data.GetRangeAtStar(StarLevel);
        }

        /// <summary>등급을 반영한 스킬 수치 배율.</summary>
        public float GetEffectiveSkillMultiplier()
        {
            return Data.GetSkillValueMultiplier(StarLevel);
        }

        private float SumBuffValues(Func<RuntimeBuff, float> selector)
        {
            float sum = 0f;
            for (int i = 0; i < _buffs.Count; i++)
            {
                sum += selector(_buffs[i]);
            }

            return sum;
        }

        private static int ClampStarLevel(int starLevel, int maxStarLevel)
        {
            int max = Math.Max(1, maxStarLevel);
            return Math.Clamp(starLevel, 1, max);
        }
    }
}
