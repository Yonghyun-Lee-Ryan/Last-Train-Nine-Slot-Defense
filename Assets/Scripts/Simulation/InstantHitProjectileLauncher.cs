using LastTrain.Battle;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Simulation
{
    /// <summary>헤드리스 시뮬용 즉시 명중 발사체.</summary>
    public sealed class InstantHitProjectileLauncher : IProjectileLauncher
    {
        public string LastPassengerId { get; private set; } = string.Empty;
        public float LastDamage { get; private set; }

        public void Launch(Vector2 origin, EnemyRuntime target, float damage, string passengerId = null)
        {
            LastPassengerId = passengerId ?? string.Empty;
            LastDamage = damage;
            if (target == null)
            {
                return;
            }

            DamageService.ApplyDamage(target, damage);
        }
    }
}
