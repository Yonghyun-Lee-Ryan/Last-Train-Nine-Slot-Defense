using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>투사체 시각 프로필.</summary>
    [CreateAssetMenu(fileName = "ProjectileVisual_", menuName = "Last Train/Projectile Visual Set")]
    public class ProjectileVisualSet : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private float size = 32f;
        [SerializeField] private bool rotateTowardTarget = true;

        public string Id => id;
        public Sprite Sprite => sprite;
        public Color Tint => tint;
        public float Size => size > 0f ? size : 32f;
        public bool RotateTowardTarget => rotateTowardTarget;
    }
}
