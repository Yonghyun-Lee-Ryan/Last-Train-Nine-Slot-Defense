using System;

namespace LastTrain.Run
{
    /// <summary>
    /// 승객 런타임 버프. 능력 카드·시너지·디버프 등에서 적용한다.
    /// </summary>
    public sealed class RuntimeBuff
    {
        public RuntimeBuff(string buffId, float attackPercentBonus = 0f, float attackSpeedPercentBonus = 0f)
        {
            if (string.IsNullOrWhiteSpace(buffId))
            {
                throw new ArgumentException("buffId는 비어 있을 수 없습니다.", nameof(buffId));
            }

            BuffId = buffId;
            AttackPercentBonus = attackPercentBonus;
            AttackSpeedPercentBonus = attackSpeedPercentBonus;
            StackCount = 1;
        }

        public string BuffId { get; }
        public float AttackPercentBonus { get; private set; }
        public float AttackSpeedPercentBonus { get; private set; }
        public int StackCount { get; private set; }

        public void AddStack(float attackBonusDelta = 0f, float attackSpeedBonusDelta = 0f)
        {
            StackCount++;
            AttackPercentBonus += attackBonusDelta;
            AttackSpeedPercentBonus += attackSpeedBonusDelta;
        }
    }
}
