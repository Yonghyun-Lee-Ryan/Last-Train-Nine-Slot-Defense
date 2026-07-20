using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>발사체 발사. ProjectilePool 또는 테스트 더블이 구현한다.</summary>
    public interface IProjectileLauncher
    {
        void Launch(Vector2 origin, EnemyRuntime target, float damage);
    }
}
