using System;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene에서 BattleManager를 초기화하고,
    /// 개발 단위 5 테스트용 적을 스폰한다. (적 이동은 개발 단위 6)
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameBattleBootstrap : MonoBehaviour
    {
        [Serializable]
        private struct DebugEnemySpawn
        {
            public EnemyData enemyData;
            public Vector2 canvasWorldPosition;
        }

        [SerializeField] private BattleManager battleManager;
        [SerializeField] private Grid.GridManager gridManager;
        [SerializeField] private DebugEnemySpawn[] debugEnemies;
        [SerializeField] private float stationDifficulty = 1f;

        private void Start()
        {
            if (battleManager == null)
            {
                Debug.LogError("[GameBattleBootstrap] battleManager가 연결되지 않았습니다.", this);
                return;
            }

            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<Grid.GridManager>();
            }

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameSession.HasActiveRun)
            {
                Debug.LogWarning("[GameBattleBootstrap] 활성 RunState가 없습니다. GameGridBootstrap 이후 실행되도록 순서를 확인하세요.", this);
                return;
            }

            RunState runState = appRoot.GameSession.RunState;
            battleManager.Initialize(runState, gridManager);
            SpawnDebugEnemies();
        }

        private void SpawnDebugEnemies()
        {
            if (debugEnemies == null || debugEnemies.Length == 0)
            {
                return;
            }

            battleManager.ClearEnemies();

            for (int i = 0; i < debugEnemies.Length; i++)
            {
                DebugEnemySpawn spawn = debugEnemies[i];
                if (spawn.enemyData == null)
                {
                    continue;
                }

                float maxHealth = spawn.enemyData.GetScaledHealth(stationDifficulty);
                var runtime = new EnemyRuntime(spawn.enemyData, maxHealth, spawn.canvasWorldPosition);
                battleManager.RegisterEnemy(runtime);
            }
        }
    }
}
