using System.Collections.Generic;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>주변 적의 이동속도를 강화하고, 사망 시 버프를 제거한다.</summary>
    public sealed class NearbyEnemyBuffAbility : IEnemyAbility
    {
        public const float AuraRadius = 180f;
        public const float SpeedBonusMultiplier = 1.2f;

        private readonly HashSet<string> _buffedEnemyIds = new();
        private bool _stopped;

        public string AbilityId => EnemyAbilityIds.NearbyBuff;

        public void OnAttach(in EnemyAbilityContext context)
        {
            _stopped = false;
            _buffedEnemyIds.Clear();
        }

        public void Tick(float deltaTime, in EnemyAbilityContext context)
        {
            if (_stopped || context.Owner == null || !context.Owner.IsAlive)
            {
                return;
            }

            RefreshAura(context);
        }

        public void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context)
        {
        }

        public void OnOwnerDied(in EnemyAbilityContext context)
        {
            _stopped = true;
            ClearAllBuffs(context.ActiveEnemies);
            _buffedEnemyIds.Clear();
        }

        private void RefreshAura(in EnemyAbilityContext context)
        {
            var stillBuffed = new HashSet<string>();
            float radiusSq = AuraRadius * AuraRadius;
            Vector2 center = context.Owner.Position;

            for (int i = 0; i < context.ActiveEnemies.Count; i++)
            {
                EnemyRuntime enemy = context.ActiveEnemies[i];
                if (enemy == null || !enemy.IsAlive || enemy == context.Owner)
                {
                    continue;
                }

                if ((enemy.Position - center).sqrMagnitude > radiusSq)
                {
                    continue;
                }

                stillBuffed.Add(enemy.InstanceId);
                if (_buffedEnemyIds.Add(enemy.InstanceId))
                {
                    enemy.MoveSpeedMultiplier *= SpeedBonusMultiplier;
                }
            }

            RemoveExpiredBuffs(context.ActiveEnemies, stillBuffed);
        }

        private void RemoveExpiredBuffs(IReadOnlyList<EnemyRuntime> enemies, HashSet<string> stillBuffed)
        {
            if (_buffedEnemyIds.Count == 0)
            {
                return;
            }

            var toRemove = new List<string>();
            foreach (string id in _buffedEnemyIds)
            {
                if (!stillBuffed.Contains(id))
                {
                    toRemove.Add(id);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                RemoveBuffFromEnemy(FindEnemy(enemies, toRemove[i]));
                _buffedEnemyIds.Remove(toRemove[i]);
            }
        }

        private void ClearAllBuffs(IReadOnlyList<EnemyRuntime> enemies)
        {
            foreach (string id in _buffedEnemyIds)
            {
                RemoveBuffFromEnemy(FindEnemy(enemies, id));
            }
        }

        private static void RemoveBuffFromEnemy(EnemyRuntime enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.MoveSpeedMultiplier = Mathf.Max(0.1f, enemy.MoveSpeedMultiplier / SpeedBonusMultiplier);
        }

        private static EnemyRuntime FindEnemy(IReadOnlyList<EnemyRuntime> enemies, string instanceId)
        {
            if (enemies == null)
            {
                return null;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy != null && enemy.InstanceId == instanceId)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
