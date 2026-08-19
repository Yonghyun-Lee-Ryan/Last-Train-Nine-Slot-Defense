using LastTrain.Data;
using LastTrain.Tutorial;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 45: FTUE 퀵스타트 — 8단계 → 5단계로 병합·단축.</summary>
    public static class Unit45FtueAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string ResourcesDatabasePath = "Assets/Resources/GameDatabase.asset";
        private const string Folder = "Assets/Data/Tutorial";

        [MenuItem("Tools/막차 생존/개발 단위 45 FTUE 퀵스타트 단계 적용")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data/Tutorial"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Tutorial");
            }

            var steps = new[]
            {
                Create("step_summon", TutorialStepKind.SummonPassenger, "소환",
                    "하단 소환을 눌러 승객을 뽑으세요.",
                    TutorialWaitEvent.SummonOpened, "SummonButton",
                    TutorialInputMask.Summon | TutorialInputMask.Acknowledge, true),
                Create("step_place", TutorialStepKind.PlacePassenger, "배치",
                    "후보를 골라 빈 칸에 배치하세요.",
                    TutorialWaitEvent.PassengerPlaced, "SummonButton",
                    TutorialInputMask.Summon | TutorialInputMask.Acknowledge, true),
                Create("step_attack", TutorialStepKind.ObserveAutoAttack, "전투 시작",
                    "준비 완료로 전투를 시작합니다. 객차 체력이 0이면 패배합니다.",
                    TutorialWaitEvent.EnemyDamaged, "ReadyButton",
                    TutorialInputMask.Ready | TutorialInputMask.GridDrag | TutorialInputMask.Pause
                    | TutorialInputMask.BattleSpeed, true),
                Create("step_merge", TutorialStepKind.MergePassengers, "합성",
                    "같은 승객을 겹치면 등급이 오릅니다.",
                    TutorialWaitEvent.PassengersMerged, "PassengerGrid",
                    TutorialInputMask.GridDrag | TutorialInputMask.Summon | TutorialInputMask.MergeUndo, true),
                Create("step_ability", TutorialStepKind.SelectAbility, "능력·보상",
                    "역을 클리어하면 보상을 받고, 능력 카드 중 하나를 고르세요.",
                    TutorialWaitEvent.AbilitySelected, "AbilityPanel",
                    TutorialInputMask.AbilityOffer | TutorialInputMask.Acknowledge
                    | TutorialInputMask.Ready | TutorialInputMask.GridDrag | TutorialInputMask.Summon, true),
            };

            ApplyToDatabase(DatabasePath, steps);
            ApplyToDatabase(ResourcesDatabasePath, steps);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"FTUE 퀵스타트 {steps.Length}단계 적용", "확인");
        }

        private static void ApplyToDatabase(string path, TutorialStepData[] steps)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(path);
            if (database == null)
            {
                return;
            }

            var so = new SerializedObject(database);
            SerializedProperty array = so.FindProperty("tutorialSteps");
            array.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static TutorialStepData Create(
            string id,
            TutorialStepKind kind,
            string title,
            string body,
            TutorialWaitEvent wait,
            string target,
            TutorialInputMask inputs,
            bool skip)
        {
            string path = $"{Folder}/Tutorial_{id}.asset";
            TutorialStepData data = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TutorialStepData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.EditorSet(id, kind, title, body, wait, target, inputs, skip);
            EditorUtility.SetDirty(data);
            return data;
        }
    }
}
