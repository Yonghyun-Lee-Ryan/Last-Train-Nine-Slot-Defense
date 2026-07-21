using LastTrain.Data;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Game Scene에 능력 카드 선택 UI를 추가한다.</summary>
    public static class Unit11AbilitySceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 11 능력 카드 UI 추가 (Game Scene)")]
        public static void BuildAbilityUi()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 11 능력 카드 UI",
                    "Game Scene에 능력 카드 선택 패널을 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            if (database == null)
            {
                EditorUtility.DisplayDialog("오류", "GameDatabase를 찾지 못했습니다.", "확인");
                return;
            }

            // 전설 카드는 중복 불가 예시
            AbilityData diverse = AssetDatabase.LoadAssetAtPath<AbilityData>(
                "Assets/Data/Abilities/Ability_Diverse.asset");
            if (diverse != null)
            {
                var diverseSo = new SerializedObject(diverse);
                diverseSo.FindProperty("allowDuplicate").boolValue = false;
                diverseSo.FindProperty("maxStack").intValue = 1;
                diverseSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(diverse);
            }

            EnsureExtraAbilities(database);

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("오류", "Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            SceneBuilderCleanup.CleanupGeneratedDuplicates(scene);
            SceneBuilderCleanup.DestroyAllComponents<AbilityPanelController>(scene);
            SceneBuilderCleanup.DestroyAllNamed(scene, "AbilityOwnedListLabel");

            GameBattleBootstrap bootstrap = SceneBuilderCleanup.FindFirstInScene<GameBattleBootstrap>(scene);
            CreateAbilityPanel(parent, database, bootstrap);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "완료",
                "능력 카드 UI를 추가했습니다.\n역 완료(GrantsAbilityChoice) 시 후보 3장이 표시됩니다.",
                "확인");
        }

        private static void EnsureExtraAbilities(GameDatabase database)
        {
            AbilityData attackSpeed = CreateAbilityAsset(
                "Assets/Data/Abilities/Ability_AttackSpeed.asset",
                "ability_attack_speed",
                "빠른 손놀림",
                "모든 승객 공격속도 +10%",
                Rarity.Common,
                AbilityEffectType.PassengerAttackSpeedPercent,
                10f,
                null,
                allowDuplicate: true);

            AbilityData summonCost = CreateAbilityAsset(
                "Assets/Data/Abilities/Ability_SummonCost.asset",
                "ability_summon_cost",
                "할인 티켓",
                "소환 비용 증가량 -1",
                Rarity.Rare,
                AbilityEffectType.SummonCostIncreaseReduction,
                1f,
                null,
                allowDuplicate: true);

            AbilityData sellBoost = CreateAbilityAsset(
                "Assets/Data/Abilities/Ability_SellBoost.asset",
                "ability_sell_boost",
                "중고 거래",
                "승객 판매 가격 +20%",
                Rarity.Rare,
                AbilityEffectType.SellPricePercent,
                20f,
                null,
                allowDuplicate: true);

            var so = new SerializedObject(database);
            SerializedProperty abilities = so.FindProperty("abilities");
            AppendAbilityIfMissing(abilities, attackSpeed);
            AppendAbilityIfMissing(abilities, summonCost);
            AppendAbilityIfMissing(abilities, sellBoost);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void AppendAbilityIfMissing(SerializedProperty abilities, AbilityData ability)
        {
            if (abilities == null || ability == null)
            {
                return;
            }

            for (int i = 0; i < abilities.arraySize; i++)
            {
                if (abilities.GetArrayElementAtIndex(i).objectReferenceValue == ability)
                {
                    return;
                }
            }

            int index = abilities.arraySize;
            abilities.arraySize++;
            abilities.GetArrayElementAtIndex(index).objectReferenceValue = ability;
        }

        private static AbilityData CreateAbilityAsset(
            string path,
            string id,
            string displayName,
            string description,
            Rarity rarity,
            AbilityEffectType effectType,
            float effectValue,
            string targetPassengerId,
            bool allowDuplicate)
        {
            var data = AssetDatabase.LoadAssetAtPath<AbilityData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AbilityData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("effectValue").floatValue = effectValue;
            so.FindProperty("targetPassengerId").stringValue = targetPassengerId ?? string.Empty;
            so.FindProperty("allowDuplicate").boolValue = allowDuplicate;
            so.FindProperty("maxStack").intValue = allowDuplicate ? 99 : 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void CreateAbilityPanel(
            Transform parent,
            GameDatabase database,
            GameBattleBootstrap bootstrap)
        {
            AbilityPanelController controller = AbilityPanelUiBuilder.Build(parent, database, bootstrap);
            if (controller == null)
            {
                return;
            }
        }
    }
}
