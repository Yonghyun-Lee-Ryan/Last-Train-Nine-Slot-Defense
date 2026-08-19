using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Synergy;
using LastTrain.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit47SynergyContentTests
    {
        private RunState _runState;
        private PassengerData _delivery;
        private PassengerData _conductor;
        private PassengerData _barista;
        private PassengerData _office;
        private PassengerData _police;
        private PassengerData _security;
        private PassengerData _student;
        private PassengerData _graduate;
        private PassengerData _cat;
        private SynergyData _nightCourier;
        private SynergyData _lastCall;
        private SynergyData _platformGuard;
        private SynergyData _examRush;
        private SynergyData _strayExpress;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());

            _delivery = CreatePassenger("delivery", PassengerTag.Delivery);
            _conductor = CreatePassenger("conductor", PassengerTag.Transit);
            _barista = CreatePassenger("barista", PassengerTag.Service);
            _office = CreatePassenger("office", PassengerTag.OfficeWorker);
            _police = CreatePassenger("police", PassengerTag.LawEnforcement);
            _security = CreatePassenger("security", PassengerTag.Security);
            _student = CreatePassenger("student", PassengerTag.Commute | PassengerTag.Academic);
            _graduate = CreatePassenger("graduate", PassengerTag.Academic);
            _cat = CreatePassenger("cat", PassengerTag.Lucky);

            _nightCourier = CreateSynergy(
                "synergy_night_courier",
                PassengerTag.Delivery | PassengerTag.Transit,
                2,
                SynergyEffectType.FastEnemyDamagePercent,
                20f);
            _lastCall = CreateSynergy(
                "synergy_last_call",
                PassengerTag.Service | PassengerTag.OfficeWorker,
                2,
                SynergyEffectType.AttackSpeedPercent,
                8f);
            _platformGuard = CreateSynergy(
                "synergy_platform_guard",
                PassengerTag.LawEnforcement | PassengerTag.Security,
                2,
                SynergyEffectType.AllAttackPercent,
                12f);
            _examRush = CreateSynergy(
                "synergy_exam_rush",
                PassengerTag.Commute | PassengerTag.Academic,
                2,
                SynergyEffectType.CritChancePercent,
                10f);
            _strayExpress = CreateSynergy(
                "synergy_stray_express",
                PassengerTag.Lucky | PassengerTag.Transit,
                2,
                SynergyEffectType.CritChancePercent,
                8f);
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_delivery);
            Object.DestroyImmediate(_conductor);
            Object.DestroyImmediate(_barista);
            Object.DestroyImmediate(_office);
            Object.DestroyImmediate(_police);
            Object.DestroyImmediate(_security);
            Object.DestroyImmediate(_student);
            Object.DestroyImmediate(_graduate);
            Object.DestroyImmediate(_cat);
            Object.DestroyImmediate(_nightCourier);
            Object.DestroyImmediate(_lastCall);
            Object.DestroyImmediate(_platformGuard);
            Object.DestroyImmediate(_examRush);
            Object.DestroyImmediate(_strayExpress);
        }

        [Test]
        public void GameDatabase_HasEightSynergiesIncludingUnit47()
        {
            GameDatabase db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            Assert.IsNotNull(db);
            Assert.AreEqual(8, db.Synergies.Count, "시너지 카탈로그는 8종이어야 한다.");

            Assert.IsTrue(db.TryGetSynergy("synergy_overtime", out _));
            Assert.IsTrue(db.TryGetSynergy("synergy_night_courier", out _));
            Assert.IsTrue(db.TryGetSynergy("synergy_last_call", out _));
            Assert.IsTrue(db.TryGetSynergy("synergy_platform_guard", out _));
            Assert.IsTrue(db.TryGetSynergy("synergy_exam_rush", out _));
            Assert.IsTrue(db.TryGetSynergy("synergy_stray_express", out _));
        }

        [Test]
        public void Unit47Synergies_ApplyExpectedModifiers()
        {
            Place(0, _delivery);
            Place(1, _conductor);
            Place(2, _barista);
            Place(3, _office);
            Place(4, _police);
            Place(5, _security);
            Place(6, _student);
            Place(7, _graduate);
            Place(8, _cat);

            SynergyData[] catalog =
            {
                _nightCourier, _lastCall, _platformGuard, _examRush, _strayExpress
            };
            SynergyEvaluation eval = SynergyEffectCalculator.Evaluate(catalog, _runState);
            Assert.AreEqual(5, eval.Active.Count);
            Assert.AreEqual(20f, eval.Modifiers.FastEnemyDamagePercent, 0.001f);
            Assert.AreEqual(8f, eval.Modifiers.GlobalAttackSpeedPercent, 0.001f);
            Assert.AreEqual(12f, eval.Modifiers.GlobalAttackPercent, 0.001f);
            Assert.AreEqual(18f, eval.Modifiers.CritChancePercent, 0.001f);
        }

        [Test]
        public void ExamRush_NeedsStudentPlusAnotherAcademic()
        {
            Place(0, _student);
            Assert.IsFalse(SynergyEffectCalculator.IsActive(_examRush, _runState));

            Place(1, _graduate);
            Assert.IsTrue(SynergyEffectCalculator.IsActive(_examRush, _runState));
        }

        [Test]
        public void HudFormatter_ShowsActiveAndInactiveContrast()
        {
            Place(0, _delivery);
            Place(1, _conductor);

            SynergyData[] catalog = { _nightCourier, _lastCall };
            _runState.Synergies.SetCatalog(catalog);
            string text = SynergyHudFormatter.Format(catalog, _runState);

            Assert.IsTrue(text.Contains(SynergyHudFormatter.ActiveColorHex));
            Assert.IsTrue(text.Contains(SynergyHudFormatter.InactiveColorHex));
            Assert.IsTrue(text.Contains("●"));
            Assert.IsTrue(text.Contains("○"));
            Assert.IsTrue(text.Contains("1/2") || text.Contains("0/2"));
        }

        [Test]
        public void HudLabel_LeftColumnFitsCatalog()
        {
            Assert.GreaterOrEqual(CombatTopHudLayout.SynergyMaxHeight, 180f);
            Assert.GreaterOrEqual(SynergyHudController.LabelFontSize, 16);
            Assert.Less(CombatTopHudLayout.GetSynergyTop(true), CombatTopHudLayout.GetSynergyTop(false));
        }

        private void Place(int slot, PassengerData data)
        {
            Assert.IsTrue(_runState.TryPlacePassenger(slot, PassengerRuntime.Create(data)));
        }

        private static PassengerData CreatePassenger(string id, PassengerTag tag)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("tags").enumValueFlag = (int)tag;
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static SynergyData CreateSynergy(
            string id,
            PassengerTag tags,
            int requiredCount,
            SynergyEffectType effectType,
            float effectValue)
        {
            var data = ScriptableObject.CreateInstance<SynergyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("requiredTags").enumValueFlag = (int)tags;
            so.FindProperty("requiredCount").intValue = requiredCount;
            so.FindProperty("requiredUniquePassengerCount").intValue = 0;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("effectValue").floatValue = effectValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
