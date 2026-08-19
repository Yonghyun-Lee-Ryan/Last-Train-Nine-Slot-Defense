using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger;
using LastTrain.Passenger.Skills;
using LastTrain.Run;
using LastTrain.Save;
using LastTrain.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit46PassengerContentTests
    {
        private RunState _runState;
        private System.Action<string> _skillHandler;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
        }

        [TearDown]
        public void TearDown()
        {
            if (_skillHandler != null)
            {
                CombatVisualEvents.PassengerSkillActivated -= _skillHandler;
                _skillHandler = null;
            }

            _runState?.Dispose();
            _runState = null;
        }

        [Test]
        public void GameDatabase_HasTwelvePassengersIncludingUnit46()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assume.That(database, Is.Not.Null);
            Assert.AreEqual(12, database.Passengers.Count);
            Assert.IsTrue(ContainsPassenger(database, MetaProgressionDefaults.PassengerConductorId));
            Assert.IsTrue(ContainsPassenger(database, MetaProgressionDefaults.PassengerBaristaId));
            Assert.IsTrue(ContainsPassenger(database, MetaProgressionDefaults.PassengerSecurityId));
            Assert.IsTrue(ContainsPassenger(database, MetaProgressionDefaults.PassengerStudentId));
        }

        [Test]
        public void Unit46Passengers_HavePortraitsAndIdleFrames()
        {
            VisualDatabase visuals = VisualDatabaseLocator.Load();
            Assume.That(visuals, Is.Not.Null);
            string[] ids =
            {
                MetaProgressionDefaults.PassengerConductorId,
                MetaProgressionDefaults.PassengerBaristaId,
                MetaProgressionDefaults.PassengerSecurityId,
                MetaProgressionDefaults.PassengerStudentId,
            };

            for (int i = 0; i < ids.Length; i++)
            {
                Assert.IsTrue(visuals.TryGetPassengerVisual(ids[i], out PassengerVisualSet set), ids[i]);
                Assert.IsNotNull(set.GetPortraitOrFallback(), ids[i]);
                Assert.IsTrue(set.Idle.HasFrames, ids[i]);
            }
        }

        [Test]
        public void OfficeWorkerIdleSheet_ConsecutiveFramesDiffer()
        {
            const string path = "Assets/Art/Sprites/Characters/passenger_office_worker_idle_sheet.png";
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            Assert.IsTrue(tex.LoadImage(bytes));
            Assert.GreaterOrEqual(tex.width, 512);
            int frame = tex.height;
            Color[] a = tex.GetPixels(0, 0, frame, frame);
            Color[] b = tex.GetPixels(frame, 0, frame, frame);
            Object.DestroyImmediate(tex);

            int diffs = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    diffs++;
                }
            }

            Assert.Greater(diffs, a.Length / 50, "idle 프레임이 거의 동일합니다.");
        }

        [Test]
        public void NewPassengers_HaveDistinctSkillsWired()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assume.That(database, Is.Not.Null);
            AssertSkill(database, MetaProgressionDefaults.PassengerConductorId, PassengerSkillIds.ChainZap);
            AssertSkill(database, MetaProgressionDefaults.PassengerBaristaId, PassengerSkillIds.ScaldSplash);
            AssertSkill(database, MetaProgressionDefaults.PassengerSecurityId, PassengerSkillIds.PerimeterPulse);
            AssertSkill(database, MetaProgressionDefaults.PassengerStudentId, PassengerSkillIds.FocusShot);
        }

        [Test]
        public void LevelUnlocks_GrantUnit46PassengersAtLevels6To9()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.accountXp = MetaProgressionDefaults.AccountXpPerLevel * 8;
            meta.accountLevel = MetaProgressionService.CalculateAccountLevel(meta.accountXp);
            Assert.GreaterOrEqual(meta.accountLevel, MetaProgressionDefaults.StudentUnlockAccountLevel);

            var result = new RunResult(
                "u46-unlock",
                "line_default",
                true,
                RunEndReason.Victory,
                3,
                3,
                5,
                1,
                0,
                2,
                50,
                100,
                10,
                20,
                0,
                1,
                0,
                0);

            MetaApplyResult apply = MetaProgressionService.TryApplyRunResult(meta, result);
            Assert.IsTrue(apply.Applied);
            Assert.IsTrue(MetaProgressionService.IsPassengerUnlocked(meta, MetaProgressionDefaults.PassengerConductorId));
            Assert.IsTrue(MetaProgressionService.IsPassengerUnlocked(meta, MetaProgressionDefaults.PassengerBaristaId));
            Assert.IsTrue(MetaProgressionService.IsPassengerUnlocked(meta, MetaProgressionDefaults.PassengerSecurityId));
            Assert.IsTrue(MetaProgressionService.IsPassengerUnlocked(meta, MetaProgressionDefaults.PassengerStudentId));
        }

        [Test]
        public void ChainZap_DamagesPrimaryAndNearestNeighbor()
        {
            PassengerData data = CreatePassenger(PassengerSkillIds.ChainZap, 10f);
            PassengerRuntime runtime = Place(data);
            EnemyData enemyData = CreateEnemy();
            try
            {
                var primary = new EnemyRuntime(enemyData, 100f, Vector2.zero, "p");
                var secondary = new EnemyRuntime(enemyData, 100f, new Vector2(40f, 0f), "s");
                var far = new EnemyRuntime(enemyData, 100f, new Vector2(400f, 0f), "f");
                new ChainZapSkill().Tick(0f, BuildContext(runtime, new[] { primary, secondary, far }));
                Assert.Less(primary.CurrentHealth, 100f);
                Assert.Less(secondary.CurrentHealth, 100f);
                Assert.AreEqual(100f, far.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(enemyData);
            }
        }

        [Test]
        public void FocusShot_TargetsHighestHpEnemy()
        {
            EnemyData enemyData = CreateEnemy();
            try
            {
                var low = new EnemyRuntime(enemyData, 40f, Vector2.zero, "low");
                var high = new EnemyRuntime(enemyData, 120f, new Vector2(20f, 0f), "high");
                Assert.AreSame(high, FocusShotSkill.FindHighestHpTarget(new[] { low, high }, Vector2.zero, 200f));
            }
            finally
            {
                Object.DestroyImmediate(enemyData);
            }
        }

        [Test]
        public void PerimeterPulse_HitsEnemiesNearTrain()
        {
            EnemyData enemyData = CreateEnemy();
            try
            {
                var near = new EnemyRuntime(enemyData, 80f, new Vector2(10f, 0f), "near");
                var far = new EnemyRuntime(enemyData, 80f, new Vector2(300f, 0f), "far");
                int hits = PerimeterPulseSkill.ApplyNearTrain(
                    new[] { near, far },
                    trainTarget: Vector2.zero,
                    radius: 50f,
                    damage: 15f);
                Assert.AreEqual(1, hits);
                Assert.AreEqual(65f, near.CurrentHealth, 0.01f);
                Assert.AreEqual(80f, far.CurrentHealth, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(enemyData);
            }
        }

        [Test]
        public void SkillActivation_RaisesCombatVisualEvent()
        {
            string seen = null;
            _skillHandler = id => seen = id;
            CombatVisualEvents.PassengerSkillActivated += _skillHandler;

            PassengerData data = CreatePassenger(PassengerSkillIds.PaperThrow, 10f);
            PassengerRuntime runtime = Place(data);
            EnemyData enemyData = CreateEnemy();
            try
            {
                var enemy = new EnemyRuntime(enemyData, 50f, Vector2.zero);
                new PaperThrowSkill().Tick(0f, BuildContext(runtime, new[] { enemy }));
                Assert.AreEqual(runtime.InstanceId, seen);
            }
            finally
            {
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(enemyData);
            }
        }

        [Test]
        public void HeadlessBalanceSmoke_WithConductorStillCompletes()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assume.That(database, Is.Not.Null);
            Assume.That(database.TryGetPassenger(MetaProgressionDefaults.PassengerConductorId, out _), Is.True);

            var slots = new BattleSimulationSlotConfig[9];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new BattleSimulationSlotConfig();
            }

            slots[0] = new BattleSimulationSlotConfig
            {
                passengerId = MetaProgressionDefaults.PassengerConductorId,
                starLevel = 1,
            };
            slots[1] = new BattleSimulationSlotConfig
            {
                passengerId = "passenger_office_worker",
                starLevel = 1,
            };
            slots[2] = new BattleSimulationSlotConfig
            {
                passengerId = "passenger_delivery",
                starLevel = 1,
            };

            var config = new BattleSimulationConfig
            {
                baseSeed = 46,
                iterations = 1,
                deltaTime = 0.2f,
                maxSimulatedSeconds = 45f,
                startingStationIndex = 1,
                maxStationIndex = 1,
                difficultyMultiplier = 0.5f,
                initialTrainHp = 200,
                initialCoins = 50,
                slots = slots,
                abilityIds = System.Array.Empty<string>(),
            };

            BattleSimulationAggregate aggregate = new HeadlessCombatSimulator().RunBatch(config, database);
            Assert.AreEqual(1, aggregate.Runs.Count);
            Assert.Greater(aggregate.Runs[0].SimulatedSeconds, 0f);
        }

        private static bool ContainsPassenger(GameDatabase database, string id)
        {
            for (int i = 0; i < database.Passengers.Count; i++)
            {
                if (database.Passengers[i] != null && database.Passengers[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertSkill(GameDatabase database, string passengerId, string skillId)
        {
            Assert.IsTrue(database.TryGetPassenger(passengerId, out PassengerData data));
            Assert.AreEqual(skillId, data.SkillId);
            IPassengerSkill skill = PassengerSkillResolver.Create(data.SkillId);
            Assert.AreEqual(skillId, skill.SkillId);
            Assert.AreNotSame(NullPassengerSkill.Instance, skill);
        }

        private PassengerRuntime Place(PassengerData data)
        {
            var runtime = PassengerRuntime.Create(data);
            Assert.IsTrue(_runState.TryPlacePassenger(0, runtime));
            return runtime;
        }

        private static PassengerData CreatePassenger(string skillId, float attack)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "u46_temp";
            so.FindProperty("displayName").stringValue = "temp";
            so.FindProperty("baseAttack").floatValue = attack;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 6f;
            so.FindProperty("skillId").stringValue = skillId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static EnemyData CreateEnemy()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "u46_enemy";
            so.FindProperty("displayName").stringValue = "enemy";
            so.FindProperty("baseHealth").floatValue = 100f;
            so.FindProperty("moveSpeed").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static PassengerSkillContext BuildContext(PassengerRuntime runtime, EnemyRuntime[] enemies)
        {
            return new PassengerSkillContext(
                runtime,
                Vector2.zero,
                200f,
                enemies,
                null,
                AbilityModifiers.Empty,
                new Vector2(0f, 500f),
                Vector2.zero,
                null,
                new RandomService(1));
        }
    }
}
