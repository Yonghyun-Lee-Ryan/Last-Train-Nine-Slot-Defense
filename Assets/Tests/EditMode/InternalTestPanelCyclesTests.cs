using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.LiveOps;
using LastTrain.Mission;
using LastTrain.Save;
using LastTrain.Simulation;
using LastTrain.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tests.EditMode
{
    /// <summary>
    /// 내부테스트 5인 × 5사이클 헤드리스 매트릭스.
    /// AppRoot/터치 UX/실기기는 포함하지 않으며, 전투 루프·모드·카탈로그·UI 레이아웃을 검증한다.
    /// </summary>
    public class InternalTestPanelCyclesTests
    {
        private const string ReportFileName = "internal_test_panel_release_25.json";

        [Test]
        public void Catalog_ReleaseModesDifficultiesAndVisualsAreWired()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assert.IsNotNull(database);

            for (int i = 0; i < DifficultyIds.Ordered.Length; i++)
            {
                Assert.IsTrue(
                    database.TryGetDifficulty(DifficultyIds.Ordered[i], out _),
                    DifficultyIds.Ordered[i]);
            }

            Assert.AreEqual(12, database.Passengers.Count);
            Assert.AreEqual(8, database.Synergies.Count);
            Assert.AreEqual(10, database.Events.Count);
            Assert.AreEqual(16, database.Relics.Count);
            Assert.AreEqual(6, database.DailyRules.Count);
            Assert.AreEqual(5, database.GetRouteStationCount(RouteIds.Quick));
            Assert.IsNotNull(database.EndlessRoute);

            VisualDatabase visuals = VisualDatabaseLocator.Load();
            Assert.IsNotNull(visuals);
            for (int i = 0; i < database.Passengers.Count; i++)
            {
                PassengerData passenger = database.Passengers[i];
                Assert.IsNotNull(passenger);
                Assert.IsTrue(
                    visuals.TryGetPassengerVisual(passenger.Id, out PassengerVisualSet set),
                    "visual missing: " + passenger.Id);
                Assert.IsNotNull(set.GetPortraitOrFallback(), "portrait missing: " + passenger.Id);
                Assert.IsTrue(set.Idle.HasFrames, "idle missing: " + passenger.Id);
            }

            var live = new LiveEventService(LocalLiveEventProvider.FromResources());
            live.RefreshCatalog();
            Assert.Greater(live.Catalog.Count, 0);

            DailyRuleData today = DailyRunService.ResolveToday(database.DailyRules);
            Assert.IsNotNull(today);
        }

        [Test]
        public void Ui_MenuWidthCapAndNineSliceMatchReleaseChrome()
        {
            string[] spritePaths =
            {
                "Assets/Art/Sprites/UI/button_normal.png",
                "Assets/Art/Sprites/UI/panel.png",
                "Assets/Art/Sprites/UI/card_frame.png",
            };

            for (int i = 0; i < spritePaths.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(spritePaths[i]) as TextureImporter;
                Assert.IsNotNull(importer, spritePaths[i]);
                Assert.AreEqual(24f, importer.spriteBorder.x, spritePaths[i]);
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            try
            {
                var safeGo = new GameObject("SafeArea", typeof(RectTransform));
                safeGo.transform.SetParent(canvasGo.transform, false);
                var start = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
                start.transform.SetParent(safeGo.transform, false);
                MainMenuHomeTabs.Active = MainMenuHomeSection.Play;
                MainMenuUiLayout.Apply(safeGo.transform);

                Transform content = safeGo.transform.Find("MainMenuScroll/Viewport/MainMenuContent");
                Assert.IsNotNull(content);
                LayoutElement startLayout = FindNamed(content, "StartButton")?.GetComponent<LayoutElement>();
                Assert.IsNotNull(startLayout);
                Assert.AreEqual(UiButtonStyler.MenuActionMaxWidth, startLayout.preferredWidth);
                Assert.AreEqual(UiButtonStyler.MenuPrimaryHeight, startLayout.preferredHeight);
            }
            finally
            {
                MainMenuHomeTabs.Active = MainMenuHomeSection.Play;
                UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void Panel_FiveTestersTimesFiveCycles_CompleteWithoutException()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assume.That(database, Is.Not.Null);

            CycleSpec[] cycles = BuildMatrix();
            Assert.AreEqual(25, cycles.Length);

            var sim = new HeadlessCombatSimulator();
            var rows = new CycleResult[cycles.Length];
            int exceptions = 0;
            int completed = 0;
            int wins = 0;

            for (int i = 0; i < cycles.Length; i++)
            {
                CycleSpec spec = cycles[i];
                var row = new CycleResult
                {
                    TesterId = spec.TesterId,
                    TesterName = spec.TesterName,
                    Cycle = spec.Cycle,
                    Focus = spec.Focus,
                    Mode = spec.Mode,
                    Difficulty = spec.Difficulty,
                    DailyRuleId = spec.DailyRuleId ?? string.Empty,
                    Seed = spec.Seed,
                };

                try
                {
                    BattleSimulationConfig config = CreateConfig(spec);
                    BattleSimulationRunResult run = sim.RunOnce(config, database, spec.Seed);
                    row.Victory = run.IsVictory;
                    row.RemainingHp = run.RemainingTrainHp;
                    row.MaxHp = run.TrainMaxHp;
                    row.Seconds = run.SimulatedSeconds;
                    row.Station = run.ReachedStationIndex;
                    row.Kills = run.EnemiesKilled;
                    row.BossKills = run.BossesKilled;
                    row.Coins = run.RemainingCoins;
                    row.Timed = run.SimulatedSeconds > 0.01f;
                    if (row.Timed)
                    {
                        completed++;
                    }

                    if (run.IsVictory)
                    {
                        wins++;
                    }
                }
                catch (Exception ex)
                {
                    exceptions++;
                    row.Error = ex.GetType().Name + ": " + ex.Message;
                }

                rows[i] = row;
            }

            WriteReport(rows, completed, wins, exceptions);

            Assert.AreEqual(0, exceptions, "헤드리스 사이클에서 예외가 발생했습니다. 리포트: " + ReportPath());
            Assert.AreEqual(25, completed, "시뮬 시간이 0인 사이클이 있습니다. 리포트: " + ReportPath());
        }

        [Test]
        public void PanelMatrix_CoversAllModesDifficultiesAndDailyRules()
        {
            CycleSpec[] cycles = BuildMatrix();
            var modes = new HashSet<string>();
            var diffs = new HashSet<string>();
            var dailies = new HashSet<string>();
            for (int i = 0; i < cycles.Length; i++)
            {
                modes.Add(cycles[i].Mode);
                diffs.Add(cycles[i].Difficulty);
                if (!string.IsNullOrEmpty(cycles[i].DailyRuleId))
                {
                    dailies.Add(cycles[i].DailyRuleId);
                }
            }

            Assert.IsTrue(modes.Contains("line1"));
            Assert.IsTrue(modes.Contains("quick"));
            Assert.IsTrue(modes.Contains("daily"));
            Assert.IsTrue(modes.Contains("endless"));
            Assert.IsTrue(diffs.Contains(DifficultyIds.Normal));
            Assert.IsTrue(diffs.Contains(DifficultyIds.Express));
            Assert.IsTrue(diffs.Contains(DifficultyIds.MidnightExpress));
            Assert.IsTrue(diffs.Contains(DifficultyIds.NonstopHell));
            Assert.AreEqual(6, dailies.Count);
            Assert.IsTrue(dailies.Contains("daily_rule_rush_hour"));
            Assert.IsTrue(dailies.Contains("daily_rule_locked_seat"));
            Assert.IsTrue(dailies.Contains("daily_rule_no_dwell"));
            Assert.IsTrue(dailies.Contains("daily_rule_summon_tax"));
            Assert.IsTrue(dailies.Contains("daily_rule_lost_and_found"));
            Assert.IsTrue(dailies.Contains("daily_rule_cheap_summon"));
        }

        private static CycleSpec[] BuildMatrix()
        {
            return new[]
            {
                Spec("T1", "김하린", 1, "코어루프", "line1", DifficultyIds.Normal, false, false, null, 1, 3, 90f, 8101,
                    "passenger_office_worker",
                    "passenger_delivery",
                    "passenger_trainer"),
                Spec("T1", "김하린", 2, "코어루프", "line1", DifficultyIds.Express, false, false, null, 1, 3, 90f, 8102,
                    "passenger_office_worker",
                    "passenger_delivery",
                    "passenger_nurse"),
                Spec("T1", "김하린", 3, "코어루프", "line1", DifficultyIds.MidnightExpress, false, false, null, 1, 2, 90f, 8103,
                    "passenger_office_worker",
                    "passenger_trainer",
                    "passenger_developer"),
                Spec("T1", "김하린", 4, "코어루프", "quick", DifficultyIds.Normal, false, false, null, 1, 5, 120f, 8104,
                    "passenger_office_worker",
                    MetaProgressionDefaults.PassengerBaristaId,
                    "passenger_delivery"),
                Spec("T1", "김하린", 5, "코어루프", "endless", DifficultyIds.Normal, false, true, null, 1, 4, 90f, 8105,
                    "passenger_office_worker",
                    "passenger_delivery",
                    "passenger_trainer"),

                Spec("T2", "박준서", 1, "난이도밸런스", "line1", DifficultyIds.Express, false, false, null, 1, 3, 90f, 8201,
                    "passenger_nurse",
                    "passenger_developer",
                    "passenger_graduate"),
                Spec("T2", "박준서", 2, "난이도밸런스", "line1", DifficultyIds.MidnightExpress, false, false, null, 1, 2, 90f, 8202,
                    "passenger_nurse",
                    "passenger_police",
                    "passenger_developer"),
                Spec("T2", "박준서", 3, "난이도밸런스", "line1", DifficultyIds.NonstopHell, false, false, null, 1, 2, 90f, 8203,
                    "passenger_police",
                    "passenger_trainer",
                    "passenger_developer"),
                Spec("T2", "박준서", 4, "난이도밸런스", "quick", DifficultyIds.Express, false, false, null, 1, 5, 120f, 8204,
                    "passenger_office_worker",
                    "passenger_nurse",
                    MetaProgressionDefaults.PassengerSecurityId),
                Spec("T2", "박준서", 5, "난이도밸런스", "endless", DifficultyIds.Express, false, true, null, 1, 3, 90f, 8205,
                    "passenger_developer",
                    "passenger_graduate",
                    "passenger_office_worker"),

                Spec("T3", "이서연", 1, "모드콘텐츠", "daily", DifficultyIds.Normal, true, false, "daily_rule_rush_hour", 1, 3, 90f, 8301,
                    MetaProgressionDefaults.PassengerConductorId,
                    MetaProgressionDefaults.PassengerBaristaId,
                    MetaProgressionDefaults.PassengerSecurityId,
                    MetaProgressionDefaults.PassengerStudentId),
                Spec("T3", "이서연", 2, "모드콘텐츠", "daily", DifficultyIds.Express, true, false, "daily_rule_locked_seat", 1, 3, 90f, 8302,
                    "passenger_office_worker",
                    "passenger_delivery",
                    MetaProgressionDefaults.PassengerConductorId,
                    "passenger_trainer",
                    "passenger_nurse"),
                Spec("T3", "이서연", 3, "모드콘텐츠", "daily", DifficultyIds.MidnightExpress, true, false, "daily_rule_no_dwell", 1, 2, 90f, 8303,
                    MetaProgressionDefaults.PassengerStudentId,
                    "passenger_graduate",
                    "passenger_office_worker"),
                Spec("T3", "이서연", 4, "모드콘텐츠", "quick", DifficultyIds.MidnightExpress, false, false, null, 1, 5, 120f, 8304,
                    MetaProgressionDefaults.PassengerBaristaId,
                    MetaProgressionDefaults.PassengerConductorId,
                    "passenger_cat"),
                Spec("T3", "이서연", 5, "모드콘텐츠", "endless", DifficultyIds.MidnightExpress, false, true, null, 1, 3, 90f, 8305,
                    MetaProgressionDefaults.PassengerSecurityId,
                    "passenger_police",
                    "passenger_office_worker"),

                Spec("T4", "최민재", 1, "진행안정", "daily", DifficultyIds.Normal, true, false, "daily_rule_summon_tax", 1, 3, 90f, 8401,
                    "passenger_police",
                    "passenger_cat",
                    "passenger_office_worker"),
                Spec("T4", "최민재", 2, "진행안정", "daily", DifficultyIds.Express, true, false, "daily_rule_lost_and_found", 1, 3, 90f, 8402,
                    "passenger_office_worker",
                    "passenger_delivery",
                    "passenger_trainer"),
                Spec("T4", "최민재", 3, "진행안정", "daily", DifficultyIds.Normal, true, false, "daily_rule_cheap_summon", 1, 3, 90f, 8403,
                    "passenger_office_worker",
                    MetaProgressionDefaults.PassengerConductorId,
                    "passenger_delivery"),
                Spec("T4", "최민재", 4, "진행안정", "line1", DifficultyIds.NonstopHell, false, false, null, 1, 2, 90f, 8404,
                    "passenger_trainer",
                    "passenger_police",
                    "passenger_nurse"),
                Spec("T4", "최민재", 5, "진행안정", "endless", DifficultyIds.NonstopHell, false, true, null, 1, 3, 90f, 8405,
                    "passenger_cat",
                    MetaProgressionDefaults.PassengerStudentId,
                    "passenger_office_worker"),

                Spec("T5", "정유나", 1, "UI비주얼", "line1", DifficultyIds.Normal, false, false, null, 1, 3, 90f, 8501,
                    MetaProgressionDefaults.PassengerConductorId,
                    MetaProgressionDefaults.PassengerBaristaId,
                    MetaProgressionDefaults.PassengerSecurityId,
                    MetaProgressionDefaults.PassengerStudentId,
                    "passenger_office_worker"),
                Spec("T5", "정유나", 2, "UI비주얼", "quick", DifficultyIds.NonstopHell, false, false, null, 1, 5, 150f, 8502,
                    MetaProgressionDefaults.PassengerBaristaId,
                    "passenger_office_worker",
                    MetaProgressionDefaults.PassengerStudentId),
                Spec("T5", "정유나", 3, "UI비주얼", "line1", DifficultyIds.MidnightExpress, false, false, null, 1, 2, 90f, 8503,
                    MetaProgressionDefaults.PassengerStudentId,
                    "passenger_graduate",
                    "passenger_office_worker"),
                Spec("T5", "정유나", 4, "UI비주얼", "line1", DifficultyIds.NonstopHell, false, false, null, 1, 2, 90f, 8504,
                    MetaProgressionDefaults.PassengerSecurityId,
                    "passenger_police",
                    "passenger_trainer"),
                Spec("T5", "정유나", 5, "UI비주얼", "endless", DifficultyIds.Normal, false, true, null, 1, 4, 90f, 8505,
                    "passenger_cat",
                    MetaProgressionDefaults.PassengerBaristaId,
                    "passenger_graduate"),
            };
        }

        private static CycleSpec Spec(
            string testerId,
            string testerName,
            int cycle,
            string focus,
            string mode,
            string difficulty,
            bool daily,
            bool endless,
            string dailyRuleId,
            int startStation,
            int maxStation,
            float maxSeconds,
            int seed,
            params string[] passengers)
        {
            return new CycleSpec
            {
                TesterId = testerId,
                TesterName = testerName,
                Cycle = cycle,
                Focus = focus,
                Mode = mode,
                Difficulty = difficulty,
                IsDaily = daily,
                IsEndless = endless,
                DailyRuleId = dailyRuleId,
                StartStation = startStation,
                MaxStation = maxStation,
                MaxSeconds = maxSeconds,
                Seed = seed,
                Passengers = passengers,
            };
        }

        private static BattleSimulationConfig CreateConfig(CycleSpec spec)
        {
            var slots = new BattleSimulationSlotConfig[Run.RunState.GridSlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new BattleSimulationSlotConfig();
            }

            int count = Math.Min(spec.Passengers.Length, slots.Length);
            for (int i = 0; i < count; i++)
            {
                slots[i] = new BattleSimulationSlotConfig
                {
                    passengerId = spec.Passengers[i],
                    starLevel = 1,
                };
            }

            string lineId = RouteIds.Default;
            if (spec.IsEndless || string.Equals(spec.Mode, "endless", StringComparison.Ordinal))
            {
                lineId = RouteIds.Endless;
            }
            else if (string.Equals(spec.Mode, "quick", StringComparison.Ordinal))
            {
                lineId = RouteIds.Quick;
            }

            return new BattleSimulationConfig
            {
                baseSeed = spec.Seed,
                iterations = 1,
                deltaTime = 0.2f,
                maxSimulatedSeconds = spec.MaxSeconds,
                startingStationIndex = spec.StartStation,
                maxStationIndex = spec.MaxStation,
                difficultyMultiplier = 1f,
                difficultyId = spec.Difficulty,
                lineId = lineId,
                isDailyRun = spec.IsDaily,
                isEndlessRun = spec.IsEndless,
                dailyRuleId = spec.DailyRuleId ?? string.Empty,
                initialTrainHp = 100,
                initialCoins = 50,
                slots = slots,
                abilityIds = Array.Empty<string>(),
                autoContinueAbilityRewards = true,
            };
        }

        private static void WriteReport(CycleResult[] rows, int completed, int wins, int exceptions)
        {
            string path = ReportPath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder(8192);
            sb.AppendLine("{");
            sb.AppendLine("  \"protocol\": \"internal-test-release-5x5-headless\",");
            sb.AppendLine("  \"date\": \"" + DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "\",");
            sb.AppendLine("  \"completed\": " + completed + ",");
            sb.AppendLine("  \"wins\": " + wins + ",");
            sb.AppendLine("  \"exceptions\": " + exceptions + ",");
            sb.AppendLine("  \"runs\": [");
            for (int i = 0; i < rows.Length; i++)
            {
                CycleResult row = rows[i];
                sb.Append("    {");
                sb.Append("\"tester\":\"").Append(Escape(row.TesterId)).Append("\",");
                sb.Append("\"name\":\"").Append(Escape(row.TesterName)).Append("\",");
                sb.Append("\"cycle\":").Append(row.Cycle).Append(",");
                sb.Append("\"focus\":\"").Append(Escape(row.Focus)).Append("\",");
                sb.Append("\"mode\":\"").Append(Escape(row.Mode)).Append("\",");
                sb.Append("\"difficulty\":\"").Append(Escape(row.Difficulty)).Append("\",");
                sb.Append("\"dailyRule\":\"").Append(Escape(row.DailyRuleId)).Append("\",");
                sb.Append("\"seed\":").Append(row.Seed).Append(",");
                sb.Append("\"victory\":").Append(row.Victory ? "true" : "false").Append(",");
                sb.Append("\"timed\":").Append(row.Timed ? "true" : "false").Append(",");
                sb.Append("\"hp\":").Append(row.RemainingHp).Append(",");
                sb.Append("\"maxHp\":").Append(row.MaxHp).Append(",");
                sb.Append("\"seconds\":").Append(row.Seconds.ToString("0.00", CultureInfo.InvariantCulture)).Append(",");
                sb.Append("\"station\":").Append(row.Station).Append(",");
                sb.Append("\"kills\":").Append(row.Kills).Append(",");
                sb.Append("\"bossKills\":").Append(row.BossKills).Append(",");
                sb.Append("\"coins\":").Append(row.Coins).Append(",");
                sb.Append("\"error\":\"").Append(Escape(row.Error)).Append("\"");
                sb.Append(" }");
                if (i < rows.Length - 1)
                {
                    sb.Append(",");
                }

                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string ReportPath()
        {
            string root = Application.dataPath.Replace("\\", "/");
            if (root.EndsWith("/Assets"))
            {
                root = root.Substring(0, root.Length - "/Assets".Length);
            }

            return Path.Combine(root, "BalanceReports", ReportFileName);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private sealed class CycleSpec
        {
            public string TesterId;
            public string TesterName;
            public int Cycle;
            public string Focus;
            public string Mode;
            public string Difficulty;
            public bool IsDaily;
            public bool IsEndless;
            public string DailyRuleId;
            public int StartStation;
            public int MaxStation;
            public float MaxSeconds;
            public int Seed;
            public string[] Passengers;
        }

        private sealed class CycleResult
        {
            public string TesterId;
            public string TesterName;
            public int Cycle;
            public string Focus;
            public string Mode;
            public string Difficulty;
            public string DailyRuleId;
            public int Seed;
            public bool Victory;
            public bool Timed;
            public int RemainingHp;
            public int MaxHp;
            public float Seconds;
            public int Station;
            public int Kills;
            public int BossKills;
            public int Coins;
            public string Error = string.Empty;
        }
    }
}
