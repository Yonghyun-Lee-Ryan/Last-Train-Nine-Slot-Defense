using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class DifficultyProgressServiceTests
    {
        [Test]
        public void Normal_IsAlwaysUnlocked()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            DifficultyData normal = CreateDifficulty(DifficultyIds.Normal, alwaysUnlocked: true);
            Assert.IsTrue(DifficultyProgressService.IsUnlocked(normal, meta));
        }

        [Test]
        public void Express_RequiresNormalBossClear()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            DifficultyData express = CreateDifficulty(
                DifficultyIds.Express,
                reqType: DifficultyUnlockType.DefeatFinalBossOnDifficulty,
                reqDifficulty: DifficultyIds.Normal);

            Assert.IsFalse(DifficultyProgressService.IsUnlocked(express, meta));

            var result = new RunResult(
                "run-1",
                "line1",
                isVictory: true,
                RunEndReason.Victory,
                reachedStationIndex: 5,
                completedStationCount: 5,
                enemiesKilled: 10,
                bossesKilled: 1,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 50,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0,
                difficultyId: DifficultyIds.Normal);

            DifficultyProgressService.ApplyRunResult(meta, result, runScore: 100, elapsedSeconds: 120f, usedAds: false);
            Assert.IsTrue(DifficultyProgressService.IsUnlocked(express, meta));
        }

        [Test]
        public void SelectionState_LocksDuringContinue()
        {
            DifficultySelectionState.UnlockSelection();
            DifficultySelectionState.Select(DifficultyIds.Express);
            DifficultySelectionState.LockToContinueSave(DifficultyIds.Normal);

            DifficultySelectionState.Select(DifficultyIds.Express);
            Assert.AreEqual(DifficultyIds.Normal, DifficultySelectionState.SelectedDifficultyId);
            Assert.IsTrue(DifficultySelectionState.IsLockedByContinue);
        }

        private static DifficultyData CreateDifficulty(
            string id,
            bool alwaysUnlocked = false,
            DifficultyUnlockType reqType = DifficultyUnlockType.AlwaysUnlocked,
            string reqDifficulty = null)
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
                req.FindPropertyRelative("requiredDifficultyId").stringValue = reqDifficulty ?? string.Empty;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
