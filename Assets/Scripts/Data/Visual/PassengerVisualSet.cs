using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>승객 1종의 시각 에셋 묶음.</summary>
    [CreateAssetMenu(fileName = "PassengerVisual_", menuName = "Last Train/Passenger Visual Set")]
    public class PassengerVisualSet : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite portrait;
        [SerializeField] private SpriteAnimationClip idle;
        [SerializeField] private SpriteAnimationClip attack;
        [SerializeField] private SpriteAnimationClip skill;
        [SerializeField] private SpriteAnimationClip merge;
        [SerializeField] private SpriteAnimationClip hit;
        [SerializeField] private Color accentColor = Color.white;

        public string Id => id;
        public Sprite Portrait => portrait;
        public SpriteAnimationClip Idle => idle;
        public SpriteAnimationClip Attack => attack;
        public SpriteAnimationClip Skill => skill;
        public SpriteAnimationClip Merge => merge;
        public SpriteAnimationClip Hit => hit;
        public Color AccentColor => accentColor;

        public Sprite GetPortraitOrFallback()
        {
            if (portrait != null)
            {
                return portrait;
            }

            return idle.FirstFrame;
        }
    }
}
