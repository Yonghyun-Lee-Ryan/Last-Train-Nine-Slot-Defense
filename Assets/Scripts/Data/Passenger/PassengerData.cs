using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 승객 정적 데이터. ScriptableObject 원본은 런타임에서 수정하지 않는다.
    /// 실제 회차 상태는 PassengerRuntime(개발 단위 3)에서 관리한다.
    /// </summary>
    [CreateAssetMenu(fileName = "Passenger_", menuName = "Last Train/Passenger Data")]
    public class PassengerData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Classification")]
        [SerializeField] private PassengerRole role;
        [SerializeField] private PassengerTag tags;

        [Header("Combat Base")]
        [SerializeField] private float baseAttack = 10f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float range = 5f;
        [SerializeField] private TargetPriority targetPriority = TargetPriority.Nearest;
        [SerializeField] private int maxTargetCount = 1;
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Header("Skill")]
        [SerializeField] private string skillId;

        [Header("Star Growth")]
        [SerializeField] private PassengerStarData[] starLevels =
        {
            PassengerStarData.CreateDefault(1),
            PassengerStarData.CreateDefault(2),
            PassengerStarData.CreateDefault(3)
        };

        [Header("Economy")]
        [SerializeField] private int sellPriceStar1 = 5;
        [SerializeField] private int sellPriceStar2 = 12;
        [SerializeField] private int sellPriceStar3 = 28;

        [Header("Meta")]
        [Tooltip("true면 메타 저장에 없어도 처음부터 소환 후보에 포함된다.")]
        [SerializeField] private bool startsUnlocked;

        public string Id => id;
        public string DisplayName => displayName;
        public PassengerRole Role => role;
        public PassengerTag Tags => tags;
        public float BaseAttack => baseAttack;
        public float AttackInterval => attackInterval;
        public float Range => range;
        public TargetPriority TargetPriority => targetPriority;
        public int MaxTargetCount => maxTargetCount;
        public DamageType DamageType => damageType;
        public string SkillId => skillId;
        public IReadOnlyList<PassengerStarData> StarLevels => starLevels;
        public int MaxStarLevel => starLevels != null && starLevels.Length > 0 ? starLevels.Length : 1;
        public bool StartsUnlocked => startsUnlocked;

        /// <summary>등급별 표시 이름. override가 없으면 기본 이름을 반환한다.</summary>
        public string GetDisplayNameAtStar(int starLevel)
        {
            if (TryGetStarData(starLevel, out PassengerStarData starData)
                && !string.IsNullOrWhiteSpace(starData.displayNameOverride))
            {
                return starData.displayNameOverride;
            }

            return displayName;
        }

        public bool TryGetStarData(int starLevel, out PassengerStarData starData)
        {
            starData = default;
            if (starLevels == null || starLevel < 1 || starLevel > starLevels.Length)
            {
                return false;
            }

            starData = starLevels[starLevel - 1];
            return true;
        }

        public float GetAttackAtStar(int starLevel)
        {
            if (!TryGetStarData(starLevel, out PassengerStarData starData))
            {
                return baseAttack;
            }

            return DataValidationUtility.CalculateAttack(baseAttack, starData.attackMultiplier);
        }

        public float GetAttackIntervalAtStar(int starLevel)
        {
            if (!TryGetStarData(starLevel, out PassengerStarData starData))
            {
                return attackInterval;
            }

            return DataValidationUtility.CalculateAttackInterval(attackInterval, starData.attackSpeedMultiplier);
        }

        public float GetRangeAtStar(int starLevel)
        {
            if (!TryGetStarData(starLevel, out PassengerStarData starData))
            {
                return range;
            }

            return range + starData.rangeBonus;
        }

        public float GetSkillValueMultiplier(int starLevel)
        {
            if (!TryGetStarData(starLevel, out PassengerStarData starData)
                || starData.skillValueMultiplier <= 0f)
            {
                return 1f;
            }

            return starData.skillValueMultiplier;
        }

        public int GetSellPrice(int starLevel)
        {
            return starLevel switch
            {
                1 => sellPriceStar1,
                2 => sellPriceStar2,
                3 => sellPriceStar3,
                _ => sellPriceStar1
            };
        }

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[PassengerData] '{name}' ID가 비어 있습니다.", this);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                Debug.LogWarning($"[PassengerData] '{name}' 표시 이름이 비어 있습니다.", this);
            }

            if (!DataValidationUtility.IsNonNegative(baseAttack))
            {
                Debug.LogWarning($"[PassengerData] '{id}' baseAttack은 0 이상이어야 합니다.", this);
            }

            if (!DataValidationUtility.IsPositive(attackInterval))
            {
                Debug.LogWarning($"[PassengerData] '{id}' attackInterval은 0보다 커야 합니다.", this);
            }

            if (!DataValidationUtility.IsPositive(range))
            {
                Debug.LogWarning($"[PassengerData] '{id}' range는 0보다 커야 합니다.", this);
            }

            if (maxTargetCount < 1)
            {
                Debug.LogWarning($"[PassengerData] '{id}' maxTargetCount는 1 이상이어야 합니다.", this);
            }

            ValidateStarLevels();
        }

        private void ValidateStarLevels()
        {
            if (starLevels == null || starLevels.Length == 0)
            {
                Debug.LogWarning($"[PassengerData] '{id}' starLevels가 비어 있습니다.", this);
                return;
            }

            for (int i = 0; i < starLevels.Length; i++)
            {
                int expectedStar = i + 1;
                PassengerStarData star = starLevels[i];

                if (star.starLevel != expectedStar)
                {
                    Debug.LogWarning(
                        $"[PassengerData] '{id}' starLevels[{i}] starLevel={star.starLevel}, expected={expectedStar}.",
                        this);
                }

                if (!DataValidationUtility.IsPositive(star.attackMultiplier))
                {
                    Debug.LogWarning($"[PassengerData] '{id}' {expectedStar}성 attackMultiplier가 유효하지 않습니다.", this);
                }

                if (!DataValidationUtility.IsPositive(star.attackSpeedMultiplier))
                {
                    Debug.LogWarning($"[PassengerData] '{id}' {expectedStar}성 attackSpeedMultiplier가 유효하지 않습니다.", this);
                }
            }
        }
    }
}
