using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.DebugTools;
using LastTrain.Grid;
using LastTrain.Run;
using LastTrain.UI;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Play Mode 전투 치트 패널. Release 빌드에는 포함되지 않는다.</summary>
    public sealed class DebugPanelWindow : EditorWindow
    {
        private int _coins = 100;
        private int _trainHp = 100;
        private string _passengerId = "passenger_office_worker";
        private int _starLevel = 1;
        private int _slotIndex;
        private int _stationIndex = 1;
        private int _seed = 12345;
        private string _bossId = "enemy_boss_drunk_manager";
        private Vector2 _scroll;

        [MenuItem("Tools/막차 생존/Debug/Debug Panel")]
        public static void Open()
        {
            GetWindow<DebugPanelWindow>("Debug Panel");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Play Mode 전용", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서만 동작합니다.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawEconomy();
            DrawPassengers();
            DrawStationWave();
            DrawCombat();
            DrawSpeedAndFlags();
            DrawDamageLog();
            EditorGUILayout.EndScrollView();
        }

        private void DrawEconomy()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("재화 / 객차", EditorStyles.boldLabel);
            _coins = EditorGUILayout.IntField("코인", _coins);
            if (GUILayout.Button("코인 설정"))
            {
                if (TryGetRun(out RunState run))
                {
                    run.Currency.SetCoins(_coins);
                }
            }

            _trainHp = EditorGUILayout.IntField("객차 HP", _trainHp);
            if (GUILayout.Button("객차 HP 설정"))
            {
                if (TryGetRun(out RunState run))
                {
                    run.Train.SetCurrentHp(_trainHp);
                }
            }

            if (GUILayout.Button("객차 풀피"))
            {
                if (TryGetRun(out RunState run))
                {
                    run.Train.RestoreFull();
                }
            }
        }

        private void DrawPassengers()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("승객", EditorStyles.boldLabel);
            _passengerId = EditorGUILayout.TextField("Passenger ID", _passengerId);
            _starLevel = Mathf.Clamp(EditorGUILayout.IntField("등급", _starLevel), 1, 3);
            _slotIndex = Mathf.Clamp(EditorGUILayout.IntField("슬롯(0-8)", _slotIndex), 0, 8);

            if (GUILayout.Button("슬롯에 생성/교체"))
            {
                PlacePassenger(_passengerId, _slotIndex, _starLevel);
            }

            if (GUILayout.Button("슬롯 승객 제거"))
            {
                if (TryGetRun(out RunState run))
                {
                    run.TryConsumePassenger(_slotIndex, out _);
                    RefreshViews();
                }
            }

            if (GUILayout.Button("선택 슬롯 등급 변경"))
            {
                if (TryGetRun(out RunState run))
                {
                    PassengerRuntime p = run.GetPassengerAtSlot(_slotIndex);
                    if (p != null)
                    {
                        p.SetStarLevel(_starLevel);
                        RefreshViews();
                    }
                }
            }
        }

        private void DrawStationWave()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("역 / 웨이브", EditorStyles.boldLabel);
            _stationIndex = EditorGUILayout.IntField("역 Index", _stationIndex);

            if (GUILayout.Button("해당 역으로 이동 후 Preparing"))
            {
                JumpStation(_stationIndex);
            }

            if (GUILayout.Button("다음 웨이브 시작"))
            {
                GameBattleBootstrap bootstrap = Object.FindAnyObjectByType<GameBattleBootstrap>();
                bootstrap?.StationManager?.TryStartNextWave();
            }
        }

        private void DrawCombat()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("전투", EditorStyles.boldLabel);
            _bossId = EditorGUILayout.TextField("Boss/Enemy ID", _bossId);
            if (GUILayout.Button("보스/적 즉시 생성"))
            {
                SpawnEnemyById(_bossId);
            }

            if (GUILayout.Button("모든 적 제거"))
            {
                BattleManager battle = Object.FindAnyObjectByType<BattleManager>();
                battle?.ClearEnemies();
            }

            _seed = EditorGUILayout.IntField("Random Seed", _seed);
            if (GUILayout.Button("시드 고정 적용"))
            {
                DebugCombatSettings.FixedSeed = _seed;
                BattleManager battle = Object.FindAnyObjectByType<BattleManager>();
                battle?.ReseedSkillRandom(_seed);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("즉시 성공"))
            {
                AppRoot.Instance?.GameSession?.EndRun(RunEndReason.Victory, isVictory: true);
            }

            if (GUILayout.Button("즉시 실패"))
            {
                AppRoot.Instance?.GameSession?.EndRun(RunEndReason.Defeat, isVictory: false);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSpeedAndFlags()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("속도 / 플래그", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1x"))
            {
                SetSpeed(1f);
            }

            if (GUILayout.Button("2x"))
            {
                SetSpeed(2f);
            }

            if (GUILayout.Button("5x"))
            {
                SetSpeed(5f);
            }

            if (GUILayout.Button("10x"))
            {
                SetSpeed(10f);
            }

            EditorGUILayout.EndHorizontal();

            DebugCombatSettings.Invulnerable = EditorGUILayout.Toggle(
                "무적 모드",
                DebugCombatSettings.Invulnerable);
            DebugCombatSettings.LogDamage = EditorGUILayout.Toggle(
                "피해량 로그",
                DebugCombatSettings.LogDamage);
            if (DebugCombatSettings.LogDamage)
            {
                DebugCombatSettings.EnsureDamageLogSubscription();
            }
        }

        private void DrawDamageLog()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("피해 로그", EditorStyles.boldLabel);
            if (GUILayout.Button("로그 지우기"))
            {
                DebugCombatSettings.ClearDamageLog();
            }

            EditorGUILayout.TextArea(DebugCombatSettings.GetDamageLog(), GUILayout.MinHeight(120));
        }

        private static void SetSpeed(float speed)
        {
            DebugCombatSettings.BattleSpeed = speed;
            DebugCombatSettings.ApplyBattleSpeed();
        }

        private static bool TryGetRun(out RunState run)
        {
            run = AppRoot.Instance?.GameSession?.RunState;
            return run != null;
        }

        private static void PlacePassenger(string passengerId, int slot, int star)
        {
            if (!TryGetRun(out RunState run))
            {
                return;
            }

            GameDatabase db = GameDatabaseLocator.Load();
            if (db == null || !db.TryGetPassenger(passengerId, out PassengerData data) || data == null)
            {
                Debug.LogWarning($"[DebugPanel] passenger '{passengerId}' 없음");
                return;
            }

            if (!run.IsSlotEmpty(slot))
            {
                run.TryConsumePassenger(slot, out _);
            }

            run.TryPlacePassengerFromSave(slot, PassengerRuntime.Create(data, star));
            RefreshViews();
        }

        private static void RefreshViews()
        {
            GridManager grid = Object.FindAnyObjectByType<GridManager>();
            grid?.RefreshViews();
            BattleManager battle = Object.FindAnyObjectByType<BattleManager>();
            battle?.RefreshPassengerControllers();
        }

        private static void JumpStation(int stationIndex)
        {
            if (!TryGetRun(out RunState run))
            {
                return;
            }

            GameDatabase db = GameDatabaseLocator.Load();
            GameBattleBootstrap bootstrap = Object.FindAnyObjectByType<GameBattleBootstrap>();
            BattleManager battle = Object.FindAnyObjectByType<BattleManager>();
            if (db == null || bootstrap?.StationManager == null || !db.TryGetStationByIndex(stationIndex, out StationData station))
            {
                Debug.LogWarning($"[DebugPanel] stationIndex={stationIndex} 없음");
                return;
            }

            battle?.ClearEnemies();
            bootstrap.StationManager.Cancel();
            bootstrap.StationManager.BeginStation(station);
            battle?.SetStationDifficulty(station.DifficultyMultiplier);
        }

        private static void SpawnEnemyById(string enemyId)
        {
            GameDatabase db = GameDatabaseLocator.Load();
            BattleManager battle = Object.FindAnyObjectByType<BattleManager>();
            if (db == null || battle == null || !db.TryGetEnemy(enemyId, out EnemyData enemy) || enemy == null)
            {
                Debug.LogWarning($"[DebugPanel] enemy '{enemyId}' 없음");
                return;
            }

            if (TryGetRun(out RunState run) && run.Battle.CurrentPhase != RunPhase.Fighting)
            {
                run.Battle.SetPhase(RunPhase.Fighting);
            }

            battle.DebugForceSpawn(enemy);
        }
    }
}
