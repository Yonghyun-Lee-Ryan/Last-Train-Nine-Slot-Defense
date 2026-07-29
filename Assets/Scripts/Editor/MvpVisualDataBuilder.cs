using System.Collections.Generic;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>VisualTheme, VisualDatabase, VisualSet ScriptableObject를 생성/갱신한다.</summary>
    public static class MvpVisualDataBuilder
    {
        private const string VisualRoot = "Assets/Data/Visual";
        private const string DatabasePath = "Assets/Data/Visual/VisualDatabase.asset";
        private const string ThemePath = "Assets/Data/Visual/VisualTheme.asset";
        private const string ResourcesDatabasePath = "Assets/Resources/VisualDatabase.asset";
        private const string ResourcesThemePath = "Assets/Resources/VisualTheme.asset";

        [MenuItem("Tools/막차 생존/MVP Visual/3. Build Visual ScriptableObjects")]
        public static void BuildAll()
        {
            BuildAllInternal(showDialog: true);
        }

        internal static void BuildAllInternal(bool showDialog)
        {
            EnsureFolder("Assets/Data/Visual");
            EnsureFolder("Assets/Resources");

            VisualTheme theme = CreateOrLoad<VisualTheme>(ThemePath);
            AssignTheme(theme);

            var passengers = new List<PassengerVisualSet>();
            CreatePassengerVisual(passengers, "passenger_office_worker", 0);
            CreatePassengerVisual(passengers, "passenger_delivery", 1);
            CreatePassengerVisual(passengers, "passenger_trainer", 2);
            CreatePassengerVisual(passengers, "passenger_nurse", 3);
            CreatePassengerVisual(passengers, "passenger_developer", 4);
            CreatePassengerVisual(passengers, "passenger_graduate", 5);
            CreatePassengerVisual(passengers, "passenger_police", 6);
            CreatePassengerVisual(passengers, "passenger_cat", 7);

            var enemies = new List<EnemyVisualSet>();
            CreateEnemyVisual(enemies, "enemy_normal", 128, 0);
            CreateEnemyVisual(enemies, "enemy_fast", 128, 1);
            CreateEnemyVisual(enemies, "enemy_tank", 160, 2);
            CreateBossVisual(enemies, "enemy_boss_drunk_manager", 3);
            CreateEnemyVisual(enemies, "enemy_split_passenger", 140, 4);
            CreateEnemyVisual(enemies, "enemy_split_minion", 96, 5);
            CreateEnemyVisual(enemies, "enemy_aura_watcher", 136, 6);
            CreateEnemyVisual(enemies, "enemy_seat_blocker", 144, 7);
            CreateBossVisual(enemies, "enemy_boss_final_conductor", 8);

            var projectiles = new List<ProjectileVisualSet>();
            CreateProjectile(projectiles, "projectile_default", "projectile_default");
            CreateProjectile(projectiles, "projectile_office_worker", "projectile_office_worker");
            CreateProjectile(projectiles, "projectile_delivery", "projectile_delivery");
            CreateProjectile(projectiles, "projectile_trainer", "projectile_trainer");
            CreateProjectile(projectiles, "projectile_nurse", "projectile_nurse");
            CreateProjectile(projectiles, "projectile_developer", "projectile_developer");
            CreateProjectile(projectiles, "projectile_graduate", "projectile_graduate");
            CreateProjectile(projectiles, "projectile_police", "projectile_police");
            CreateProjectile(projectiles, "projectile_cat", "projectile_cat");
            CreateProjectile(projectiles, "projectile_turret", "projectile_turret");

            var vfx = new List<VfxVisualSet>();
            CreateVfx(vfx, "vfx_hit", "vfx_hit_sheet.png", 48);
            CreateVfx(vfx, "vfx_crit", "vfx_crit_sheet.png", 64);
            CreateVfx(vfx, "vfx_death", "vfx_death_sheet.png", 56);
            CreateVfx(vfx, "vfx_coin", "vfx_coin_sheet.png", 40);
            CreateVfx(vfx, "vfx_summon", "vfx_summon_sheet.png", 52);
            CreateVfx(vfx, "vfx_merge", "vfx_merge_sheet.png", 60);
            CreateVfx(vfx, "vfx_sell", "vfx_sell_sheet.png", 44);
            CreateVfx(vfx, "vfx_knockback", "vfx_knockback_sheet.png", 72);
            CreateVfx(vfx, "vfx_heal", "vfx_heal_sheet.png", 56);
            CreateVfx(vfx, "vfx_turret_spawn", "vfx_turret_spawn_sheet.png", 48);
            CreateVfx(vfx, "vfx_aoe", "vfx_aoe_sheet.png", 80);
            CreateVfx(vfx, "vfx_boss_enrage", "vfx_boss_enrage_sheet.png", 72);
            CreateVfx(vfx, "vfx_boss_portal", "vfx_boss_portal_sheet.png", 64);
            CreateVfx(vfx, "vfx_debuff_pulse", "vfx_debuff_pulse_sheet.png", 96);

            VisualDatabase database = CreateOrLoad<VisualDatabase>(DatabasePath);
            AssignDatabase(database, theme, passengers, enemies, projectiles, vfx);

            CopyDatabaseToResources(database);
            CopyThemeToResources(theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", "Visual ScriptableObject 생성이 완료되었습니다.", "확인");
                Selection.activeObject = database;
            }
        }

        private static void AssignTheme(VisualTheme theme)
        {
            SerializedObject so = new SerializedObject(theme);
            AssignSprite(so, "subwayBackground", "Assets/Art/Sprites/Environment/subway_background.png");
            AssignSprite(so, "mainMenuBackground", "Assets/Art/Sprites/Environment/main_menu_background.png");
            AssignSprite(so, "spawnLane", "Assets/Art/Sprites/Environment/enemy_route_v.png");
            AssignSprite(so, "trainTarget", "Assets/Art/Sprites/Environment/train_target_car.png");
            AssignSprite(so, "seatFrame", "Assets/Art/Sprites/Environment/seat_frame.png");
            AssignSprite(so, "seatHighlight", "Assets/Art/Sprites/Environment/seat_highlight.png");
            AssignSprite(so, "panel", "Assets/Art/Sprites/UI/panel.png");
            AssignSprite(so, "buttonNormal", "Assets/Art/Sprites/UI/button_normal.png");
            AssignSprite(so, "buttonPressed", "Assets/Art/Sprites/UI/button_pressed.png");
            AssignSprite(so, "buttonDisabled", "Assets/Art/Sprites/UI/button_disabled.png");
            AssignSprite(so, "cardFrame", "Assets/Art/Sprites/UI/card_frame.png");
            AssignSprite(so, "popupDim", "Assets/Art/Sprites/UI/popup_dim.png");
            AssignSprite(so, "hpBarFill", "Assets/Art/Sprites/UI/hp_bar_fill.png");
            AssignSprite(so, "hpBarBackground", "Assets/Art/Sprites/UI/hp_bar_bg.png");
            AssignSprite(so, "bossHpBarFill", "Assets/Art/Sprites/UI/boss_hp_bar_fill.png");
            AssignSprite(so, "mainMenuTitle", "Assets/Art/Sprites/UI/main_menu_title.png");
            AssignSprite(so, "resultVictoryBanner", "Assets/Art/Sprites/UI/result_victory_banner.png");
            AssignSprite(so, "resultDefeatBanner", "Assets/Art/Sprites/UI/result_defeat_banner.png");
            AssignSprite(so, "iconCoin", "Assets/Art/Sprites/UI/icon_coin.png");
            AssignSprite(so, "iconStation", "Assets/Art/Sprites/UI/icon_station.png");
            AssignSprite(so, "iconWave", "Assets/Art/Sprites/UI/icon_wave.png");
            AssignSprite(so, "iconReady", "Assets/Art/Sprites/UI/icon_ready.png");
            AssignSprite(so, "iconSpeed", "Assets/Art/Sprites/UI/icon_speed.png");
            AssignSprite(so, "iconPause", "Assets/Art/Sprites/UI/icon_pause.png");
            AssignSprite(so, "iconSummon", "Assets/Art/Sprites/UI/icon_summon.png");
            AssignSprite(so, "iconSell", "Assets/Art/Sprites/UI/icon_sell.png");
            AssignSprite(so, "iconReroll", "Assets/Art/Sprites/UI/icon_reroll.png");
            AssignSprite(so, "iconAd", "Assets/Art/Sprites/UI/icon_ad.png");
            AssignSprite(so, "iconAbility", "Assets/Art/Sprites/UI/icon_ability.png");
            AssignSprite(so, "iconSynergy", "Assets/Art/Sprites/UI/icon_synergy.png");
            AssignSprite(so, "starFrame1", "Assets/Art/Sprites/UI/star_frame_1.png");
            AssignSprite(so, "starFrame2", "Assets/Art/Sprites/UI/star_frame_2.png");
            AssignSprite(so, "starFrame3", "Assets/Art/Sprites/UI/star_frame_3.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePassengerVisual(List<PassengerVisualSet> list, string id, int accentIndex)
        {
            string path = $"{VisualRoot}/PassengerVisual_{id}.asset";
            PassengerVisualSet visual = CreateOrLoad<PassengerVisualSet>(path);
            SerializedObject so = new SerializedObject(visual);
            so.FindProperty("id").stringValue = id;
            AssignSprite(so, "portrait", $"Assets/Art/Sprites/Characters/{id}_portrait.png");
            AssignClip(so, "idle", $"Assets/Art/Sprites/Characters/{id}_idle_sheet.png", 256, 256, true);
            AssignClip(so, "attack", $"Assets/Art/Sprites/Characters/{id}_attack_sheet.png", 256, 256, false);
            AssignClip(so, "skill", $"Assets/Art/Sprites/Characters/{id}_skill_sheet.png", 256, 256, false);
            AssignClip(so, "merge", $"Assets/Art/Sprites/Characters/{id}_merge_sheet.png", 256, 256, false);
            AssignClip(so, "hit", $"Assets/Art/Sprites/Characters/{id}_hit_sheet.png", 256, 256, false);
            so.FindProperty("accentColor").colorValue = VisualThemePalette.PassengerAccent[accentIndex];
            so.ApplyModifiedPropertiesWithoutUndo();
            list.Add(visual);
        }

        private static void CreateEnemyVisual(List<EnemyVisualSet> list, string id, int size, int accentIndex)
        {
            string path = $"{VisualRoot}/EnemyVisual_{id}.asset";
            EnemyVisualSet visual = CreateOrLoad<EnemyVisualSet>(path);
            SerializedObject so = new SerializedObject(visual);
            so.FindProperty("id").stringValue = id;
            AssignClip(so, "move", $"Assets/Art/Sprites/Enemies/{id}_move_sheet.png", 128, 128, true);
            AssignClip(so, "hit", $"Assets/Art/Sprites/Enemies/{id}_hit_sheet.png", 128, 128, false);
            AssignClip(so, "death", $"Assets/Art/Sprites/Enemies/{id}_death_sheet.png", 128, 128, false);
            so.FindProperty("accentColor").colorValue = VisualThemePalette.EnemyAccent[accentIndex];
            so.FindProperty("displaySize").vector2Value = new Vector2(size, size);
            so.ApplyModifiedPropertiesWithoutUndo();
            list.Add(visual);
        }

        private static void CreateBossVisual(List<EnemyVisualSet> list, string id, int accentIndex)
        {
            string path = $"{VisualRoot}/EnemyVisual_{id}.asset";
            EnemyVisualSet visual = CreateOrLoad<EnemyVisualSet>(path);
            SerializedObject so = new SerializedObject(visual);
            so.FindProperty("id").stringValue = id;
            AssignClip(so, "move", $"Assets/Art/Sprites/Enemies/{id}_move_sheet.png", 256, 256, true);
            AssignClip(so, "hit", $"Assets/Art/Sprites/Enemies/{id}_hit_sheet.png", 256, 256, false);
            AssignClip(so, "death", $"Assets/Art/Sprites/Enemies/{id}_death_sheet.png", 256, 256, false);
            AssignClip(so, "cast", $"Assets/Art/Sprites/Enemies/{id}_cast_sheet.png", 256, 256, false);
            AssignClip(so, "enraged", $"Assets/Art/Sprites/Enemies/{id}_enraged_sheet.png", 256, 256, true);
            so.FindProperty("accentColor").colorValue = VisualThemePalette.EnemyAccent[accentIndex];
            so.FindProperty("displaySize").vector2Value = new Vector2(320f, 320f);
            so.ApplyModifiedPropertiesWithoutUndo();
            list.Add(visual);
        }

        private static void CreateProjectile(List<ProjectileVisualSet> list, string id, string spriteName)
        {
            string path = $"{VisualRoot}/ProjectileVisual_{id}.asset";
            ProjectileVisualSet visual = CreateOrLoad<ProjectileVisualSet>(path);
            SerializedObject so = new SerializedObject(visual);
            so.FindProperty("id").stringValue = id;
            AssignSprite(so, "sprite", $"Assets/Art/Sprites/Projectiles/{spriteName}.png");
            so.FindProperty("tint").colorValue = Color.white;
            so.FindProperty("size").floatValue = 32f;
            so.FindProperty("rotateTowardTarget").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            list.Add(visual);
        }

        private static void CreateVfx(List<VfxVisualSet> list, string id, string fileName, float size)
        {
            string path = $"{VisualRoot}/VfxVisual_{id}.asset";
            VfxVisualSet visual = CreateOrLoad<VfxVisualSet>(path);
            SerializedObject so = new SerializedObject(visual);
            so.FindProperty("id").stringValue = id;
            AssignClip(so, "clip", $"Assets/Art/Sprites/VFX/{fileName}", (int)size, (int)size, false);
            so.FindProperty("tint").colorValue = Color.white;
            so.FindProperty("size").floatValue = size;
            so.ApplyModifiedPropertiesWithoutUndo();
            list.Add(visual);
        }

        private static void AssignDatabase(
            VisualDatabase database,
            VisualTheme theme,
            List<PassengerVisualSet> passengers,
            List<EnemyVisualSet> enemies,
            List<ProjectileVisualSet> projectiles,
            List<VfxVisualSet> vfx)
        {
            SerializedObject so = new SerializedObject(database);
            so.FindProperty("theme").objectReferenceValue = theme;
            AssignArray(so, "passengers", passengers);
            AssignArray(so, "enemies", enemies);
            AssignArray(so, "projectiles", projectiles);
            AssignArray(so, "vfx", vfx);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CopyDatabaseToResources(VisualDatabase database)
        {
            if (AssetDatabase.LoadAssetAtPath<VisualDatabase>(ResourcesDatabasePath) != null)
            {
                AssetDatabase.DeleteAsset(ResourcesDatabasePath);
            }

            AssetDatabase.CopyAsset(DatabasePath, ResourcesDatabasePath);
        }

        private static void CopyThemeToResources(VisualTheme theme)
        {
            if (AssetDatabase.LoadAssetAtPath<VisualTheme>(ResourcesThemePath) != null)
            {
                AssetDatabase.DeleteAsset(ResourcesThemePath);
            }

            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(theme), ResourcesThemePath);
        }

        private static void AssignClip(SerializedObject so, string propertyName, string sheetPath, int frameWidth, int frameHeight, bool loop)
        {
            Sprite[] frames = MvpArtImporter.LoadSheetSprites(sheetPath, frameWidth, frameHeight);
            SerializedProperty clipProperty = so.FindProperty(propertyName);
            SerializedProperty framesProperty = clipProperty.FindPropertyRelative("frames");
            framesProperty.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            clipProperty.FindPropertyRelative("framesPerSecond").floatValue = 8f;
            clipProperty.FindPropertyRelative("loop").boolValue = loop;
        }

        private static void AssignSprite(SerializedObject so, string propertyName, string assetPath)
        {
            so.FindProperty(propertyName).objectReferenceValue = MvpArtImporter.LoadSprite(assetPath);
        }

        private static void AssignArray<T>(SerializedObject so, string propertyName, List<T> items) where T : Object
        {
            SerializedProperty array = so.FindProperty(propertyName);
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
