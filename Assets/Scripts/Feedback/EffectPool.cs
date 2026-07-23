using LastTrain.Core;
using LastTrain.Release;
using LastTrain.UI;
using UnityEngine;

namespace LastTrain.Feedback
{
    /// <summary>전투 VFX 풀 래퍼. 저사양 모드에서 재생 빈도를 줄인다.</summary>
    public sealed class EffectPool : MonoBehaviour
    {
        [SerializeField] private UiVfxPool vfxPool;
        [SerializeField] [Range(0.1f, 1f)] private float lowFxPlayChance = 0.45f;

        public UiVfxPool InnerPool => vfxPool;

        public void Initialize(UiVfxPool pool)
        {
            vfxPool = pool;
            vfxPool?.Initialize();
        }

        public void Play(string vfxId, Vector2 worldPosition)
        {
            if (string.IsNullOrWhiteSpace(vfxId) || vfxPool == null)
            {
                return;
            }

            GameSettingsService settings = AppRoot.Instance?.GameSettings;
            if (settings != null && settings.LowFxMode && Random.value > lowFxPlayChance)
            {
                return;
            }

            vfxPool.Play(vfxId, worldPosition);
        }
    }
}
