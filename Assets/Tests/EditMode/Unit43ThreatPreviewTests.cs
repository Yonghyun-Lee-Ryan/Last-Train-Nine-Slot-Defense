using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Run;
using LastTrain.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tests.EditMode
{
    public class Unit43ThreatPreviewTests
    {
        private readonly List<Object> _owned = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void ResolveUpcoming_Preparing_ReturnsFirstWaveTypesInOrder()
        {
            EnemyData fast = CreateEnemy("fast", EnemyType.Fast);
            EnemyData tank = CreateEnemy("tank", EnemyType.Tank);
            StationData station = CreateStation(
                StationType.Normal,
                CreateWave("w0", (fast, 2), (tank, 1)),
                CreateWave("w1", (CreateEnemy("elite", EnemyType.Elite), 1)));

            IReadOnlyList<ThreatPreviewEntry> entries = ThreatPreviewResolver.ResolveUpcoming(
                station,
                currentWaveIndex: 0,
                RunPhase.Preparing);

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("fast", entries[0].EnemyId);
            Assert.AreEqual(EnemyType.Fast, entries[0].EnemyType);
            Assert.AreEqual(2, entries[0].Count);
            Assert.AreEqual("tank", entries[1].EnemyId);
            Assert.AreEqual(EnemyType.Tank, entries[1].EnemyType);
        }

        [Test]
        public void ResolveUpcoming_Fighting_ReturnsNextWaveNotCurrent()
        {
            EnemyData current = CreateEnemy("now", EnemyType.Normal);
            EnemyData next = CreateEnemy("next", EnemyType.Elite);
            StationData station = CreateStation(
                StationType.Normal,
                CreateWave("w0", (current, 3)),
                CreateWave("w1", (next, 2)));

            IReadOnlyList<ThreatPreviewEntry> entries = ThreatPreviewResolver.ResolveUpcoming(
                station,
                currentWaveIndex: 0,
                RunPhase.Fighting);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("next", entries[0].EnemyId);
            Assert.AreEqual(EnemyType.Elite, entries[0].EnemyType);
            Assert.AreEqual(2, entries[0].Count);
        }

        [Test]
        public void ResolveUpcoming_LastWaveFighting_ReturnsEmpty()
        {
            StationData station = CreateStation(
                StationType.Normal,
                CreateWave("only", (CreateEnemy("n", EnemyType.Normal), 1)));

            IReadOnlyList<ThreatPreviewEntry> entries = ThreatPreviewResolver.ResolveUpcoming(
                station,
                currentWaveIndex: 0,
                RunPhase.Fighting);

            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void ResolveUpcoming_GroupsSameEnemyId()
        {
            EnemyData split = CreateEnemy("split", EnemyType.Split);
            WaveData wave = CreateWave("w", (split, 2), (split, 3));
            IReadOnlyList<ThreatPreviewEntry> entries = ThreatPreviewResolver.CollectWaveEntries(wave);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(5, entries[0].Count);
            Assert.AreEqual(EnemyType.Split, entries[0].EnemyType);
        }

        [Test]
        public void ResolveUpcoming_ShopStation_ReturnsEmpty()
        {
            StationData shop = CreateStation(StationType.Shop);
            IReadOnlyList<ThreatPreviewEntry> entries = ThreatPreviewResolver.ResolveUpcoming(
                shop,
                0,
                RunPhase.Preparing);
            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void StationBriefing_CollectThreatTypes_OmitsTextLabels()
        {
            var briefing = new StationBriefing
            {
                HasFastEnemy = true,
                HasTankEnemy = true,
                HasEliteEnemy = true,
                BossPatternHint = "문 개방",
            };

            IReadOnlyList<EnemyType> types = briefing.CollectThreatTypes();
            Assert.AreEqual(4, types.Count);
            Assert.AreEqual(EnemyType.Fast, types[0]);
            Assert.AreEqual(EnemyType.Tank, types[1]);
            Assert.AreEqual(EnemyType.Elite, types[2]);
            Assert.AreEqual(EnemyType.Boss, types[3]);
            Assert.AreNotEqual(
                EnemyTypeIconPalette.ColorFor(EnemyType.Fast),
                EnemyTypeIconPalette.ColorFor(EnemyType.Tank));
            Assert.AreNotEqual(
                EnemyTypeIconPalette.SpriteFor(EnemyType.Fast),
                EnemyTypeIconPalette.SpriteFor(EnemyType.Tank));
        }

        [Test]
        public void BossPhaseBriefing_NonBoss_ShouldNotShow()
        {
            StationData station = CreateStation(
                StationType.Normal,
                CreateWave("w", (CreateEnemy("n", EnemyType.Normal), 1)));
            BossPhaseBriefing card = BossPhaseBriefingResolver.Build(station, new StationBriefing());
            Assert.IsFalse(card.ShouldShow);
            Assert.AreEqual(0, card.PhaseCount);
        }

        [Test]
        public void BossPhaseBriefing_MidBoss_TwoPhasesFromBriefingFlags()
        {
            EnemyData boss = CreateBoss("mid", doorOpen: 0f, enrage: 0.3f);
            StationData station = CreateStation(
                StationType.Boss,
                CreateWave("boss_wave", (boss, 1)));
            var briefing = StationBriefingBuilder.Build(station, null);

            BossPhaseBriefing card = BossPhaseBriefingResolver.Build(station, briefing);

            Assert.IsTrue(card.ShouldShow);
            Assert.IsFalse(card.HasDoorOpen);
            Assert.AreEqual(2, card.PhaseCount);
            Assert.AreEqual(BossPhase.Normal, card.Segments[0].Phase);
            Assert.AreEqual(BossPhase.Enraged, card.Segments[1].Phase);
            Assert.IsTrue(ContainsType(card.ThreatTypes, EnemyType.Boss));
        }

        [Test]
        public void BossPhaseBriefing_FinalBoss_ThreePhasesWithDoorOpen()
        {
            EnemyData boss = CreateBoss("final", doorOpen: 0.6f, enrage: 0.3f);
            StationData station = CreateStation(
                StationType.Boss,
                CreateWave("boss_wave", (boss, 1), (CreateEnemy("fast", EnemyType.Fast), 2)));
            var so = new SerializedObject(station);
            so.FindProperty("bossPatternHint").stringValue = "문 개방 후 광폭화";
            so.ApplyModifiedPropertiesWithoutUndo();
            var briefing = StationBriefingBuilder.Build(station, null);

            BossPhaseBriefing card = BossPhaseBriefingResolver.Build(station, briefing);

            Assert.IsTrue(card.ShouldShow);
            Assert.IsTrue(card.HasDoorOpen);
            Assert.AreEqual(3, card.PhaseCount);
            Assert.AreEqual(BossPhase.Normal, card.Segments[0].Phase);
            Assert.AreEqual(BossPhase.DoorOpen, card.Segments[1].Phase);
            Assert.AreEqual(BossPhase.Enraged, card.Segments[2].Phase);
            Assert.IsTrue(ContainsType(card.ThreatTypes, EnemyType.Fast));
            Assert.IsTrue(ContainsType(card.ThreatTypes, EnemyType.Boss));
            Assert.Greater(card.Segments[0].Span, card.Segments[1].Span);
        }

        [Test]
        public void WaveThreatTicker_Bind_ShowsTypeNameAndCount()
        {
            var root = new GameObject("TickerTest", typeof(RectTransform));
            _owned.Add(root);
            var space = root.GetComponent<RectTransform>();
            WaveThreatTickerView ticker = WaveThreatTickerView.Ensure(space);
            var entries = new[]
            {
                new ThreatPreviewEntry("fast", EnemyType.Fast, 3),
                new ThreatPreviewEntry("tank", EnemyType.Tank, 1),
            };

            ticker.Bind(entries, caption: "이번 웨이브");

            Assert.IsTrue(ticker.IsShowing);
            Assert.AreEqual(2, ticker.IconCount);
            Assert.AreEqual("이번 웨이브", ticker.CaptionText);
            Text[] labels = ticker.GetComponentsInChildren<Text>();
            Assert.GreaterOrEqual(labels.Length, 3);
            Assert.IsTrue(System.Array.Exists(labels, t => t.text.Contains("빠름") && t.text.Contains("3")));
            Assert.IsTrue(System.Array.Exists(labels, t => t.text.Contains("탱커")));
        }

        [Test]
        public void WaveThreatTicker_BindEmpty_Hides()
        {
            var root = new GameObject("TickerHide", typeof(RectTransform));
            _owned.Add(root);
            WaveThreatTickerView ticker = WaveThreatTickerView.Ensure(root.GetComponent<RectTransform>());
            ticker.Bind(new[] { new ThreatPreviewEntry("n", EnemyType.Normal, 1) });
            ticker.Bind(System.Array.Empty<ThreatPreviewEntry>());
            Assert.IsFalse(ticker.IsShowing);
            Assert.AreEqual(0, ticker.IconCount);
        }

        [Test]
        public void BossBriefingCard_Show_BuildsPhaseAndThreatIcons()
        {
            var root = new GameObject("BossCardTest", typeof(RectTransform));
            _owned.Add(root);
            BossBriefingCardView card = BossBriefingCardView.Ensure(root.GetComponent<RectTransform>());
            var briefing = new BossPhaseBriefing
            {
                ShouldShow = true,
                Segments = new[]
                {
                    new BossPhaseSegment(BossPhase.Normal, 1f, 0.6f),
                    new BossPhaseSegment(BossPhase.DoorOpen, 0.6f, 0.3f),
                    new BossPhaseSegment(BossPhase.Enraged, 0.3f, 0f),
                },
                ThreatTypes = new[] { EnemyType.Fast, EnemyType.Boss },
            };

            card.Show(briefing);

            Assert.IsTrue(card.IsShowing);
            Assert.IsFalse(card.IsDimBlocking);
            Assert.AreEqual(3, card.PhaseSegmentCount);
            Assert.AreEqual(2, card.ThreatIconCount);

            card.Hide();
            Assert.IsFalse(card.IsShowing);
            Assert.IsFalse(card.IsDimBlocking);
            Transform dim = card.transform.Find("Dim");
            Assert.IsTrue(dim == null || !dim.gameObject.activeSelf);
        }

        [Test]
        public void BossBriefingCard_Ensure_DoesNotLeaveBlockingDim()
        {
            var root = new GameObject("BossCardEnsure", typeof(RectTransform));
            _owned.Add(root);
            BossBriefingCardView card = BossBriefingCardView.Ensure(root.GetComponent<RectTransform>());
            Assert.IsFalse(card.IsShowing);
            Assert.IsFalse(card.IsDimBlocking);
            Transform dim = card.transform.Find("Dim");
            Assert.IsTrue(dim == null || !dim.gameObject.activeSelf || !card.gameObject.activeSelf);
        }

        private EnemyData CreateEnemy(string id, EnemyType type)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("enemyType").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
            _owned.Add(data);
            return data;
        }

        private EnemyData CreateBoss(string id, float doorOpen, float enrage)
        {
            EnemyData data = CreateEnemy(id, EnemyType.Boss);
            var so = new SerializedObject(data);
            SerializedProperty thresholds = so.FindProperty("bossPhaseThresholds");
            thresholds.FindPropertyRelative("doorOpenHealthRatio").floatValue = doorOpen;
            thresholds.FindPropertyRelative("enrageHealthRatio").floatValue = enrage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private WaveData CreateWave(string id, params (EnemyData enemy, int count)[] spawns)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            var so = new SerializedObject(wave);
            so.FindProperty("id").stringValue = id;
            SerializedProperty spawnsProp = so.FindProperty("spawns");
            spawnsProp.arraySize = spawns.Length;
            for (int i = 0; i < spawns.Length; i++)
            {
                SerializedProperty element = spawnsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("enemy").objectReferenceValue = spawns[i].enemy;
                element.FindPropertyRelative("count").intValue = spawns[i].count;
                element.FindPropertyRelative("spawnInterval").floatValue = 0f;
                element.FindPropertyRelative("spawnDelay").floatValue = 0f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            _owned.Add(wave);
            return wave;
        }

        private StationData CreateStation(StationType type, params WaveData[] waves)
        {
            var station = ScriptableObject.CreateInstance<StationData>();
            var so = new SerializedObject(station);
            so.FindProperty("id").stringValue = $"station_{type}";
            so.FindProperty("displayName").stringValue = type.ToString();
            so.FindProperty("stationType").enumValueIndex = (int)type;
            so.FindProperty("stationIndex").intValue = 1;
            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.arraySize = waves.Length;
            for (int i = 0; i < waves.Length; i++)
            {
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            _owned.Add(station);
            return station;
        }

        private static bool ContainsType(IReadOnlyList<EnemyType> types, EnemyType expected)
        {
            if (types == null)
            {
                return false;
            }

            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] == expected)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
