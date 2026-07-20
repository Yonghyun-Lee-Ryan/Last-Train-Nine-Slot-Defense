using System;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 승객 등급(별)별 성장 수치.
    /// PassengerData에 포함되는 직렬화 가능한 구조체다.
    /// </summary>
    [Serializable]
    public struct PassengerStarData
    {
        [Tooltip("등급 (1~5). MVP에서는 1~3만 사용")]
        [Min(1)]
        public int starLevel;

        [Tooltip("해당 등급 표시 이름. 비어 있으면 PassengerData.displayName 사용")]
        public string displayNameOverride;

        [Tooltip("기본 공격력 배율")]
        [Min(0f)]
        public float attackMultiplier;

        [Tooltip("공격속도 배율. 1보다 크면 더 빠르게 공격")]
        [Min(0f)]
        public float attackSpeedMultiplier;

        [Tooltip("사거리 추가값")]
        public float rangeBonus;

        [Tooltip("스킬 수치 배율")]
        [Min(0f)]
        public float skillValueMultiplier;

        /// <summary>README 8.4 기본값을 기준으로 한 1~3성 프리셋.</summary>
        public static PassengerStarData CreateDefault(int starLevel)
        {
            return starLevel switch
            {
                1 => new PassengerStarData
                {
                    starLevel = 1,
                    attackMultiplier = 1.0f,
                    attackSpeedMultiplier = 1.0f,
                    rangeBonus = 0f,
                    skillValueMultiplier = 1.0f
                },
                2 => new PassengerStarData
                {
                    starLevel = 2,
                    attackMultiplier = 2.2f,
                    attackSpeedMultiplier = 1.05f,
                    rangeBonus = 0f,
                    skillValueMultiplier = 1.2f
                },
                3 => new PassengerStarData
                {
                    starLevel = 3,
                    attackMultiplier = 4.8f,
                    attackSpeedMultiplier = 1.1f,
                    rangeBonus = 0.5f,
                    skillValueMultiplier = 1.5f
                },
                4 => new PassengerStarData
                {
                    starLevel = 4,
                    attackMultiplier = 10.0f,
                    attackSpeedMultiplier = 1.15f,
                    rangeBonus = 0.75f,
                    skillValueMultiplier = 1.8f
                },
                5 => new PassengerStarData
                {
                    starLevel = 5,
                    attackMultiplier = 21.0f,
                    attackSpeedMultiplier = 1.2f,
                    rangeBonus = 1.0f,
                    skillValueMultiplier = 2.0f
                },
                _ => new PassengerStarData
                {
                    starLevel = Mathf.Max(1, starLevel),
                    attackMultiplier = 1.0f,
                    attackSpeedMultiplier = 1.0f
                }
            };
        }
    }
}
