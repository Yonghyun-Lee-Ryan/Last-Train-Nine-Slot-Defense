using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Mission;
using LastTrain.Save;
using LastTrain.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class HomeGoalResolverTests
    {
        [Test]
        public void Resolve_PrefersClaimableMission()
        {
            MissionData mission = CreateMission("m1", "일일 탑승");
            var views = new List<MissionProgressView>
            {
                new MissionProgressView(mission, 3, 3, completed: true, claimed: false, "2026-08-13"),
            };

            HomeGoalSnapshot goal = HomeGoalResolver.Resolve(
                views,
                difficulties: null,
                meta: new MetaSaveData(),
                activeSeasonDisplayName: "시즌A",
                hasContinueSave: true);

            Assert.AreEqual(HomeGoalKind.MissionClaim, goal.Kind);
            Assert.AreEqual("미션 열기", goal.CtaLabel);
            Object.DestroyImmediate(mission);
        }

        [Test]
        public void Resolve_MissionProgress_BeforeDifficulty()
        {
            MissionData mission = CreateMission("m2", "적 처치");
            var views = new List<MissionProgressView>
            {
                new MissionProgressView(mission, 1, 5, completed: false, claimed: false, "2026-08-13"),
            };
            DifficultyData hard = CreateDifficulty("hard", alwaysUnlocked: false);

            HomeGoalSnapshot goal = HomeGoalResolver.Resolve(
                views,
                new List<DifficultyData> { hard },
                meta: new MetaSaveData(),
                activeSeasonDisplayName: null,
                hasContinueSave: false);

            Assert.AreEqual(HomeGoalKind.MissionProgress, goal.Kind);
            StringAssert.Contains("1/5", goal.Body);
            Object.DestroyImmediate(mission);
            Object.DestroyImmediate(hard);
        }

        [Test]
        public void Resolve_DifficultyUnlock_BeforeSeason()
        {
            DifficultyData hard = CreateDifficulty(
                "hard",
                alwaysUnlocked: false,
                DifficultyUnlockType.AccountLevel);
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.accountLevel = 1;

            HomeGoalSnapshot goal = HomeGoalResolver.Resolve(
                missions: null,
                new List<DifficultyData> { hard },
                meta,
                activeSeasonDisplayName: "시즌 이벤트",
                hasContinueSave: false);

            Assert.AreEqual(HomeGoalKind.DifficultyUnlock, goal.Kind);
            Assert.AreEqual("플레이로 이동", goal.CtaLabel);
            Object.DestroyImmediate(hard);
        }

        [Test]
        public void Resolve_Season_ThenContinue_ThenStart()
        {
            HomeGoalSnapshot season = HomeGoalResolver.Resolve(
                null, null, new MetaSaveData(), "한여름 막차", hasContinueSave: true);
            Assert.AreEqual(HomeGoalKind.SeasonEvent, season.Kind);

            HomeGoalSnapshot cont = HomeGoalResolver.Resolve(
                null, null, new MetaSaveData(), null, hasContinueSave: true);
            Assert.AreEqual(HomeGoalKind.ContinueRun, cont.Kind);

            HomeGoalSnapshot start = HomeGoalResolver.Resolve(
                null, null, new MetaSaveData(), null, hasContinueSave: false);
            Assert.AreEqual(HomeGoalKind.StartRun, start.Kind);
        }

        [Test]
        public void HomeTabs_DefaultIsPlay()
        {
            MainMenuHomeTabs.Active = MainMenuHomeSection.Play;
            Assert.AreEqual(MainMenuHomeSection.Play, MainMenuHomeTabs.Active);
        }

        private static MissionData CreateMission(string id, string displayName)
        {
            var mission = ScriptableObject.CreateInstance<MissionData>();
            var so = new SerializedObject(mission);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return mission;
        }

        private static DifficultyData CreateDifficulty(
            string id,
            bool alwaysUnlocked,
            DifficultyUnlockType reqType = DifficultyUnlockType.AlwaysUnlocked)
        {
            var data = ScriptableObject.CreateInstance<DifficultyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            if (alwaysUnlocked)
            {
                so.FindProperty("unlockCondition").FindPropertyRelative("requirements").arraySize = 0;
            }
            else
            {
                SerializedProperty reqs = so.FindProperty("unlockCondition").FindPropertyRelative("requirements");
                reqs.arraySize = 1;
                SerializedProperty req = reqs.GetArrayElementAtIndex(0);
                req.FindPropertyRelative("unlockType").enumValueIndex = (int)reqType;
                req.FindPropertyRelative("requiredDifficultyId").stringValue = DifficultyIds.Normal;
                req.FindPropertyRelative("requiredAccountLevel").intValue = 99;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
