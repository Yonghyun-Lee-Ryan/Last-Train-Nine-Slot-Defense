using LastTrain.Data;
using LastTrain.Tutorial;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit30TutorialAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string Folder = "Assets/Data/Tutorial";

        [MenuItem("Tools/막차 생존/개발 단위 30 튜토리얼 단계 생성")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data/Tutorial"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Tutorial");
            }

            var steps = new[]
            {
                Create("step_summon", TutorialStepKind.SummonPassenger, "승객 소환",
                    "하단 소환 버튼을 눌러 승객 후보를 확인하세요.",
                    TutorialWaitEvent.SummonOpened, "SummonButton",
                    TutorialInputMask.Summon | TutorialInputMask.Acknowledge, true),
                Create("step_place", TutorialStepKind.PlacePassenger, "승객 배치",
                    "후보 중 하나를 선택해 빈 칸에 배치하세요.",
                    TutorialWaitEvent.PassengerPlaced, "SummonButton",
                    TutorialInputMask.Summon | TutorialInputMask.Acknowledge, true),
                Create("step_attack", TutorialStepKind.ObserveAutoAttack, "자동 공격",
                    "준비 완료를 누르면 승객이 자동으로 공격합니다. 전투를 시작해 보세요.",
                    TutorialWaitEvent.EnemyDamaged, "ReadyButton",
                    TutorialInputMask.Ready | TutorialInputMask.GridDrag | TutorialInputMask.Pause, true),
                Create("step_merge", TutorialStepKind.MergePassengers, "승객 합성",
                    "같은 승객을 겹치면 등급이 올라갑니다. 초록으로 강조된 칸을 합성해 보세요.",
                    TutorialWaitEvent.PassengersMerged, "GridRoot",
                    TutorialInputMask.GridDrag | TutorialInputMask.Summon | TutorialInputMask.Ready, true),
                Create("step_hp", TutorialStepKind.ExplainTrainHp, "객차 내구도",
                    "위쪽 객차 체력이 0이 되면 패배합니다. 적을 막아내세요.",
                    TutorialWaitEvent.Acknowledge, "TrainHpLabel",
                    TutorialInputMask.Acknowledge | TutorialInputMask.Ready | TutorialInputMask.GridDrag, true),
                Create("step_reward", TutorialStepKind.StationReward, "역 완료 보상",
                    "역을 클리어하면 보상을 받고 다음 역으로 진행합니다.",
                    TutorialWaitEvent.StationCompleted, "ReadyButton",
                    TutorialInputMask.Ready | TutorialInputMask.GridDrag | TutorialInputMask.Summon | TutorialInputMask.AbilityOffer, true),
                Create("step_ability", TutorialStepKind.SelectAbility, "능력 카드",
                    "능력 카드 3장 중 하나를 선택하세요.",
                    TutorialWaitEvent.AbilitySelected, "AbilityPanel",
                    TutorialInputMask.AbilityOffer | TutorialInputMask.Acknowledge, true),
                Create("step_boss", TutorialStepKind.BossHint, "보스 힌트",
                    "보스 역에서는 사전 경고를 확인하세요. 패턴을 읽고 대비합시다.",
                    TutorialWaitEvent.BossBriefingShown, "BossNameLabel",
                    TutorialInputMask.All, true),
            };

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database != null)
            {
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"튜토리얼 단계 {steps.Length}개 생성", "확인");
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
