using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>VFX 1종의 프레임 애니메이션.</summary>
    [CreateAssetMenu(fileName = "VfxVisual_", menuName = "Last Train/VFX Visual Set")]
    public class VfxVisualSet : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private SpriteAnimationClip clip;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private float size = 64f;

        public string Id => id;
        public SpriteAnimationClip Clip => clip;
        public Color Tint => tint;
        public float Size => size > 0f ? size : 64f;
    }
}
