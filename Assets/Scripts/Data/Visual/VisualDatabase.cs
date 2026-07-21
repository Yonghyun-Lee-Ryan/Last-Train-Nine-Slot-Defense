using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>모든 시각 ScriptableObject를 한곳에서 조회한다.</summary>
    [CreateAssetMenu(fileName = "VisualDatabase", menuName = "Last Train/Visual Database")]
    public class VisualDatabase : ScriptableObject
    {
        [SerializeField] private VisualTheme theme;
        [SerializeField] private PassengerVisualSet[] passengers;
        [SerializeField] private EnemyVisualSet[] enemies;
        [SerializeField] private ProjectileVisualSet[] projectiles;
        [SerializeField] private VfxVisualSet[] vfx;

        public VisualTheme Theme => theme;
        public IReadOnlyList<PassengerVisualSet> Passengers => passengers;
        public IReadOnlyList<EnemyVisualSet> Enemies => enemies;
        public IReadOnlyList<ProjectileVisualSet> Projectiles => projectiles;
        public IReadOnlyList<VfxVisualSet> Vfx => vfx;

        public bool TryGetPassengerVisual(string id, out PassengerVisualSet visual)
        {
            return TryFind(passengers, id, out visual);
        }

        public bool TryGetEnemyVisual(string id, out EnemyVisualSet visual)
        {
            return TryFind(enemies, id, out visual);
        }

        public bool TryGetProjectileVisual(string id, out ProjectileVisualSet visual)
        {
            if (TryFind(projectiles, id, out visual))
            {
                return true;
            }

            return TryFind(projectiles, "projectile_default", out visual);
        }

        public bool TryGetVfx(string id, out VfxVisualSet visual)
        {
            return TryFind(vfx, id, out visual);
        }

        private static bool TryFind<T>(T[] items, string id, out T result) where T : IDataWithId
        {
            result = default;
            if (items == null || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int i = 0; i < items.Length; i++)
            {
                T item = items[i];
                if (item != null && item.Id == id)
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }
    }
}
