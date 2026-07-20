using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene에서 BattleManager를 초기화하고,
    /// 개발 단위 6 테스트용 적을 주기적으로 스폰한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameBattleBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private Grid.GridManager gridManager;
        [SerializeField] private EnemyData[] debugSpawnEnemies;
        [SerializeField] private float spawnInterval = 2.5f;
        [SerializeField] private int maxConcurrentEnemies = 6;
        [SerializeField] private bool autoSpawn = true;

        private float _spawnTimer;
        private int _spawnIndex;

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
            _spawnTimer = spawnInterval;
        }

        private void Update()
        {
            if (!autoSpawn || battleManager == null || debugSpawnEnemies == null || debugSpawnEnemies.Length == 0)
            {
                return;
            }

            if (battleManager.EnemyRegistry.Enemies.Count >= maxConcurrentEnemies)
            {
                return;
            }

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            _spawnTimer = spawnInterval;
            EnemyData data = debugSpawnEnemies[_spawnIndex % debugSpawnEnemies.Length];
            _spawnIndex++;
            battleManager.SpawnEnemy(data);
        }
    }
}
