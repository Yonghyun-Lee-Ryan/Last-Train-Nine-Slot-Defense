using System;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>보스 체력 비율에 따른 단계 전환 임계값.</summary>
    [Serializable]
    public struct BossPhaseThresholds
    {
        [Range(0f, 1f)]
        [Tooltip("0이면 비활성. 예: 0.6 = 60% 이하에서 DoorOpen")]
        public float doorOpenHealthRatio;

        [Range(0f, 1f)]
        [Tooltip("예: 0.3 = 30% 이하에서 Enraged")]
        public float enrageHealthRatio;

        public float DoorOpenHealthRatio => Mathf.Clamp01(doorOpenHealthRatio);
        public float EnrageHealthRatio => Mathf.Clamp01(enrageHealthRatio);

        public static BossPhaseThresholds Create(float doorOpen, float enrage)
        {
            return new BossPhaseThresholds
            {
                doorOpenHealthRatio = doorOpen,
                enrageHealthRatio = enrage,
            };
        }

        public static BossPhaseThresholds DefaultFinalBoss => Create(0.6f, 0.3f);
        public static BossPhaseThresholds DefaultMidBoss => Create(0f, 0.3f);
    }
}
