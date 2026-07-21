using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>적 1종의 시각 에셋 묶음.</summary>
    [CreateAssetMenu(fileName = "EnemyVisual_", menuName = "Last Train/Enemy Visual Set")]
    public class EnemyVisualSet : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private SpriteAnimationClip move;
        [SerializeField] private SpriteAnimationClip hit;
        [SerializeField] private SpriteAnimationClip death;
        [SerializeField] private SpriteAnimationClip cast;
        [SerializeField] private SpriteAnimationClip enraged;
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField] private Vector2 displaySize = new(128f, 128f);

        public string Id => id;
        public SpriteAnimationClip Move => move;
        public SpriteAnimationClip Hit => hit;
        public SpriteAnimationClip Death => death;
        public SpriteAnimationClip Cast => cast;
        public SpriteAnimationClip Enraged => enraged;
        public Color AccentColor => accentColor;
        public Vector2 DisplaySize => displaySize;

        public Sprite GetMoveOrFallback()
        {
            return move.FirstFrame;
        }
    }
}
