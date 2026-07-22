using System.IO;
using System.Text;
using LastTrain.Data;
using LastTrain.Simulation;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>헤드리스 자동 전투 시뮬레이터. Release 빌드에는 포함되지 않는다.</summary>
    public sealed class BattleSimulatorWindow : EditorWindow
    {
        private int _baseSeed = 42;
        private int _iterations = 100;
        private float _deltaTime = 0.1f;
        private float _maxSeconds = 300f;
        private int _startStation = 1;
        private int _maxStation = 3;
        private float _difficulty = 1f;
        private int _trainHp = 100;
        private int _coins = 50;
        private string _slotCsv =
            "passenger_office_worker:1,,,,passenger_delivery:1,,,,passenger_trainer:1";
        private string _abilityCsv = string.Empty;
        private string _lastSummary = string.Empty;
        private string _lastCsvPath = string.Empty;
        private Vector2 _scroll;

        [MenuItem("Tools/막차 생존/Debug/Battle Simulator")]
        public static void Open()
        {
            GetWindow<BattleSimulatorWindow>("Battle Simulator");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Headless Battle Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "GameObject 없이 전투 로직만 반복 실행합니다. 저장/메타/분석 이벤트는 발생하지 않습니다.",
                MessageType.Info);

            _baseSeed = EditorGUILayout.IntField("Base Seed", _baseSeed);
            _iterations = Mathf.Max(1, EditorGUILayout.IntField("Iterations", _iterations));
            _deltaTime = Mathf.Max(0.01f, EditorGUILayout.FloatField("Delta Time", _deltaTime));
            _maxSeconds = Mathf.Max(1f, EditorGUILayout.FloatField("Max Seconds / Run", _maxSeconds));
            _startStation = Mathf.Max(1, EditorGUILayout.IntField("Start Station Index", _startStation));
            _maxStation = Mathf.Max(_startStation, EditorGUILayout.IntField("Max Station Index", _maxStation));
            _difficulty = Mathf.Max(0.01f, EditorGUILayout.FloatField("Difficulty Mult", _difficulty));
            _trainHp = Mathf.Max(1, EditorGUILayout.IntField("Train HP", _trainHp));
            _coins = Mathf.Max(0, EditorGUILayout.IntField("Coins", _coins));

            EditorGUILayout.LabelField("Slots CSV (9 slots, passengerId:star, empty allowed)");
            _slotCsv = EditorGUILayout.TextArea(_slotCsv, GUILayout.MinHeight(40));
            EditorGUILayout.LabelField("Ability IDs CSV");
            _abilityCsv = EditorGUILayout.TextField(_abilityCsv);

            if (GUILayout.Button($"Run {_iterations} Simulations"))
            {
                RunSimulations();
            }

            if (!string.IsNullOrEmpty(_lastCsvPath) && GUILayout.Button("Reveal CSV"))
            {
                EditorUtility.RevealInFinder(_lastCsvPath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_lastSummary, GUILayout.MinHeight(180));
            EditorGUILayout.EndScrollView();
        }

        private void RunSimulations()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            if (database == null)
            {
                _lastSummary = "GameDatabase를 로드하지 못했습니다.";
                return;
            }

            var config = new BattleSimulationConfig
            {
                baseSeed = _baseSeed,
                iterations = _iterations,
                deltaTime = _deltaTime,
                maxSimulatedSeconds = _maxSeconds,
                startingStationIndex = _startStation,
                maxStationIndex = _maxStation,
                difficultyMultiplier = _difficulty,
                initialTrainHp = _trainHp,
                initialCoins = _coins,
                slots = ParseSlots(_slotCsv),
                abilityIds = ParseCsv(_abilityCsv),
                autoContinueAbilityRewards = true,
            };

            var simulator = new HeadlessCombatSimulator();
            BattleSimulationAggregate aggregate = simulator.RunBatch(config, database);

            string dir = Path.Combine(Application.dataPath, "../SimResults");
            _lastCsvPath = SimulationCsvWriter.Write(aggregate, dir);
            _lastSummary = BuildSummary(aggregate, _lastCsvPath);
            Debug.Log(_lastSummary);
            Repaint();
        }

        private static string BuildSummary(BattleSimulationAggregate a, string csvPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"iterations={a.Iterations}");
            sb.AppendLine($"wins={a.Wins} winRate={a.WinRate:P1}");
            sb.AppendLine(
                $"remainingHp avg={a.AvgRemainingHp:0.##} min={a.MinRemainingHp:0.##} max={a.MaxRemainingHp:0.##} std={a.StdDevRemainingHp:0.##}");
            sb.AppendLine(
                $"timeSec avg={a.AvgSimulatedSeconds:0.##} min={a.MinSimulatedSeconds:0.##} max={a.MaxSimulatedSeconds:0.##}");
            sb.AppendLine("avg damage by passenger:");
            foreach (var pair in a.AvgDamageByPassengerId)
            {
                sb.AppendLine($"  {pair.Key}: {pair.Value:0.##}");
            }

            sb.AppendLine("avg attacks by passenger:");
            foreach (var pair in a.AvgSkillTicksByPassengerId)
            {
                sb.AppendLine($"  {pair.Key}: {pair.Value:0.##}");
            }

            sb.AppendLine("avg train reaches by enemy:");
            foreach (var pair in a.AvgTrainReachesByEnemyId)
            {
                sb.AppendLine($"  {pair.Key}: {pair.Value:0.##}");
            }

            sb.AppendLine($"csv={csvPath}");
            return sb.ToString();
        }

        private static BattleSimulationSlotConfig[] ParseSlots(string csv)
        {
            var slots = new BattleSimulationSlotConfig[9];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new BattleSimulationSlotConfig();
            }

            if (string.IsNullOrWhiteSpace(csv))
            {
                return slots;
            }

            string[] parts = csv.Split(',');
            for (int i = 0; i < parts.Length && i < 9; i++)
            {
                string token = parts[i]?.Trim();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                string[] idStar = token.Split(':');
                slots[i].passengerId = idStar[0].Trim();
                slots[i].starLevel = idStar.Length > 1 && int.TryParse(idStar[1], out int star)
                    ? Mathf.Clamp(star, 1, 3)
                    : 1;
            }

            return slots;
        }

        private static string[] ParseCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return System.Array.Empty<string>();
            }

            string[] parts = csv.Split(',');
            var list = new System.Collections.Generic.List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string t = parts[i]?.Trim();
                if (!string.IsNullOrEmpty(t))
                {
                    list.Add(t);
                }
            }

            return list.ToArray();
        }
    }
}
