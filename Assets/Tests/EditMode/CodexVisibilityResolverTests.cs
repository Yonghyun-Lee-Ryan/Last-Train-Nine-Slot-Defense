using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Save;
using LastTrain.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class CodexVisibilityResolverTests
    {
        private PassengerData _passenger;
        private EnemyData _enemy;
        private EnemyData _boss;
        private RelicData _relic;

        [SetUp]
        public void SetUp()
        {
            _passenger = CreatePassenger("passenger_test", "테스트 승객");
            _enemy = CreateEnemy("enemy_test", "테스트 적", EnemyType.Normal);
            _boss = CreateEnemy("boss_test", "테스트 보스", EnemyType.Boss);
            _relic = CreateRelic("relic_test", "테스트 유물", "유물 설명");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_passenger);
            Object.DestroyImmediate(_enemy);
            Object.DestroyImmediate(_boss);
            Object.DestroyImmediate(_relic);
        }

        [Test]
        public void BuildPassengerEntry_Undiscovered_ShowsLockedTitle()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            CodexEntryView view = CodexVisibilityResolver.BuildPassengerEntry(meta, _passenger, visuals: null);

            Assert.IsFalse(view.IsDiscovered);
            Assert.AreEqual(CodexVisibilityResolver.LockedTitle, view.Title);
            Assert.AreEqual(CodexVisibilityResolver.LockedDetail, view.Detail);
            StringAssert.DoesNotContain("테스트 승객", view.Title);
        }

        [Test]
        public void BuildPassengerEntry_Discovered_ShowsDisplayNameAndMastery()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.discoveredPassengerIds = new[] { "passenger_test" };
            meta.passengerMasteries = new[]
            {
                new MetaPassengerMasteryEntry
                {
                    passengerId = "passenger_test",
                    useCount = 4,
                    highestStar = 2,
                    bossKillParticipations = 1,
                },
            };

            CodexEntryView view = CodexVisibilityResolver.BuildPassengerEntry(meta, _passenger, visuals: null);

            Assert.IsTrue(view.IsDiscovered);
            Assert.AreEqual("테스트 승객", view.Title);
            StringAssert.Contains("역할: 공격", view.Detail);
            StringAssert.DoesNotContain("Attack", view.Detail);
            StringAssert.Contains("Lv.2", view.Detail);
            StringAssert.Contains("사용 4", view.Detail);
            StringAssert.Contains("보스 1", view.Detail);
        }

        [Test]
        public void BuildEnemyEntry_Discovered_ShowsStats()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.discoveredEnemyIds = new[] { "enemy_test" };

            CodexEntryView view = CodexVisibilityResolver.BuildEnemyEntry(
                meta,
                _enemy,
                visuals: null,
                CodexCategory.Enemy);

            Assert.IsTrue(view.IsDiscovered);
            Assert.AreEqual("테스트 적", view.Title);
            StringAssert.Contains("체력", view.Detail);
        }

        [Test]
        public void BuildBossEntry_UsesBossDiscoveryList()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.discoveredBossIds = new[] { "boss_test" };

            CodexEntryView view = CodexVisibilityResolver.BuildEnemyEntry(
                meta,
                _boss,
                visuals: null,
                CodexCategory.Boss);

            Assert.IsTrue(view.IsDiscovered);
            Assert.AreEqual("테스트 보스", view.Title);
            StringAssert.Contains("보스", view.Detail);
        }

        [Test]
        public void BuildRelicEntry_Undiscovered_IsLocked()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            CodexEntryView view = CodexVisibilityResolver.BuildRelicEntry(meta, _relic);

            Assert.IsFalse(view.IsDiscovered);
            Assert.AreEqual(CodexVisibilityResolver.LockedTitle, view.Title);
            StringAssert.DoesNotContain("테스트 유물", view.Title);
        }

        [Test]
        public void TryApplyRunResult_NewDiscovery_AppearsInCodex()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            var result = new RunResult(
                runId: "run-codex",
                lineId: "line_default",
                isVictory: false,
                endReason: RunEndReason.Defeat,
                reachedStationIndex: 0,
                completedStationCount: 0,
                enemiesKilled: 1,
                bossesKilled: 0,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 0,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0,
                discoveredPassengerIds: new[] { "passenger_test" },
                discoveredEnemyIds: new[] { "enemy_test" });

            MetaProgressionService.TryApplyRunResult(meta, result);

            CodexEntryView passengerView = CodexVisibilityResolver.BuildPassengerEntry(meta, _passenger, null);
            CodexEntryView enemyView = CodexVisibilityResolver.BuildEnemyEntry(meta, _enemy, null, CodexCategory.Enemy);

            Assert.IsTrue(passengerView.IsDiscovered);
            Assert.AreEqual("테스트 승객", passengerView.Title);
            Assert.IsTrue(enemyView.IsDiscovered);
            Assert.AreEqual("테스트 적", enemyView.Title);
            Assert.Contains("passenger_test", meta.pendingNewDiscoveryIds);
            Assert.Contains("enemy_test", meta.pendingNewDiscoveryIds);
        }

        private static PassengerData CreatePassenger(string id, string displayName)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static EnemyData CreateEnemy(string id, string displayName, EnemyType type)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("enemyType").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static RelicData CreateRelic(string id, string displayName, string description)
        {
            var data = ScriptableObject.CreateInstance<RelicData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
