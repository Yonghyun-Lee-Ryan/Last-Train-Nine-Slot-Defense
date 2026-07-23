using UnityEngine;
using UnityEngine.Audio;

namespace LastTrain.Audio
{
    /// <summary>오디오 믹서·SFX 스로틀 설정. Resources에서 로드한다.</summary>
    [CreateAssetMenu(fileName = "AudioData", menuName = "LastTrain/Audio/Audio Data")]
    public sealed class AudioData : ScriptableObject
    {
        public const string ResourcesPath = "Audio/AudioData";

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string bgmVolumeParam = "BgmVolume";
        [SerializeField] private string sfxVolumeParam = "SfxVolume";
        [SerializeField] private float defaultSfxMinInterval = 0f;
        [SerializeField] private float combatHitMinInterval = 0.04f;
        [SerializeField] private float combatCritMinInterval = 0.08f;
        [SerializeField] private float enemyDeathMinInterval = 0.06f;
        [SerializeField] private float coinMinInterval = 0.05f;

        public AudioMixer Mixer => mixer;
        public string MasterVolumeParam => masterVolumeParam;
        public string BgmVolumeParam => bgmVolumeParam;
        public string SfxVolumeParam => sfxVolumeParam;

        public float GetSfxMinInterval(SfxId id)
        {
            return id switch
            {
                SfxId.CombatHit => Mathf.Max(0.01f, combatHitMinInterval),
                SfxId.CombatCrit => Mathf.Max(0.01f, combatCritMinInterval),
                SfxId.EnemyDeath => Mathf.Max(0.01f, enemyDeathMinInterval),
                SfxId.Coin => Mathf.Max(0.01f, coinMinInterval),
                _ => Mathf.Max(0f, defaultSfxMinInterval),
            };
        }

        public static AudioData LoadOrNull()
        {
            return Resources.Load<AudioData>(ResourcesPath);
        }
    }
}
