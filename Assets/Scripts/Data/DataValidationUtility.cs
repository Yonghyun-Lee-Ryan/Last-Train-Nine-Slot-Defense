using System.Collections.Generic;

namespace LastTrain.Data
{
    /// <summary>
    /// ScriptableObject OnValidate와 EditMode 테스트에서 공유하는 순수 검증 로직.
    /// </summary>
    public static class DataValidationUtility
    {
        public static bool IsValidId(string id)
        {
            return !string.IsNullOrWhiteSpace(id);
        }

        public static bool IsPositive(float value)
        {
            return value > 0f;
        }

        public static bool IsNonNegative(float value)
        {
            return value >= 0f;
        }

        public static bool IsPositive(int value)
        {
            return value > 0;
        }

        public static bool IsNonNegative(int value)
        {
            return value >= 0;
        }

        /// <summary>
        /// ID 목록에서 중복을 찾는다. 빈 ID는 무시한다.
        /// </summary>
        /// <returns>중복된 ID 목록 (고유)</returns>
        public static List<string> FindDuplicateIds(IReadOnlyList<string> ids)
        {
            var duplicates = new List<string>();
            if (ids == null || ids.Count == 0)
            {
                return duplicates;
            }

            var seen = new HashSet<string>();
            var reported = new HashSet<string>();

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (!IsValidId(id))
                {
                    continue;
                }

                if (!seen.Add(id) && reported.Add(id))
                {
                    duplicates.Add(id);
                }
            }

            return duplicates;
        }

        /// <summary>
        /// 승객 등급별 공격력을 계산한다. baseAttack × attackMultiplier.
        /// </summary>
        public static float CalculateAttack(float baseAttack, float attackMultiplier)
        {
            if (baseAttack < 0f || attackMultiplier < 0f)
            {
                return 0f;
            }

            return baseAttack * attackMultiplier;
        }

        /// <summary>
        /// 승객 등급별 공격 간격을 계산한다. baseInterval ÷ attackSpeedMultiplier.
        /// 배율이 클수록 더 빠르게 공격한다.
        /// </summary>
        public static float CalculateAttackInterval(float baseInterval, float attackSpeedMultiplier)
        {
            if (baseInterval <= 0f || attackSpeedMultiplier <= 0f)
            {
                return baseInterval > 0f ? baseInterval : 1f;
            }

            return baseInterval / attackSpeedMultiplier;
        }

        /// <summary>
        /// 적 최종 체력 = 기본 체력 × 역 난도 계수 × 노선 난도 계수.
        /// </summary>
        public static float CalculateEnemyHealth(float baseHealth, float stationDifficulty, float lineDifficulty)
        {
            if (baseHealth < 0f)
            {
                return 0f;
            }

            float station = stationDifficulty > 0f ? stationDifficulty : 1f;
            float line = lineDifficulty > 0f ? lineDifficulty : 1f;
            return baseHealth * station * line;
        }
    }
}
