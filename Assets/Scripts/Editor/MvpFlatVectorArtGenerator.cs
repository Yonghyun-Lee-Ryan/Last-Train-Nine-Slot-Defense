using System.Collections.Generic;
using System.IO;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>README 27 웹툰 스타일 PNG 에셋을 프로시저럴로 생성한다.</summary>
    public static class MvpFlatVectorArtGenerator
    {
        private const int FrameCount = 4;

        [MenuItem("Tools/막차 생존/MVP Visual/1. Generate Webtoon Style PNGs")]
        public static void GenerateAll()
        {
            GenerateAllInternal(showDialog: true);
        }

        internal static void GenerateAllInternal(bool showDialog)
        {
            if (showDialog && !EditorUtility.DisplayDialog(
                    "웹툰 스타일 PNG 생성",
                    "Assets/Art/Sprites 아래 UI, 환경, 캐릭터, 적, 투사체, VFX PNG를 README 27 방향으로 재생성합니다.\n기존 파일을 덮어씁니다.",
                    "생성",
                    "취소"))
            {
                return;
            }

            EnsureFolders();
            GenerateStyleBoard();
            GenerateEnvironment();
            GenerateUi();
            GeneratePassengers();
            GenerateEnemies();
            GenerateProjectiles();
            GenerateVfx();
            AssetDatabase.Refresh();
            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", "웹툰 스타일 PNG 생성이 완료되었습니다.", "확인");
            }
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets/Art/Source");
            CreateFolder("Assets/Art/Sprites/UI");
            CreateFolder("Assets/Art/Sprites/Environment");
            CreateFolder("Assets/Art/Sprites/Characters");
            CreateFolder("Assets/Art/Sprites/Enemies");
            CreateFolder("Assets/Art/Sprites/Projectiles");
            CreateFolder("Assets/Art/Sprites/VFX");
        }

        private static void GenerateStyleBoard()
        {
            var board = FlatVectorDrawUtility.Create(540, 960);
            FlatVectorDrawUtility.FillRect(board, new RectInt(0, 0, 540, 960), VisualThemePalette.CarNavy);
            FlatVectorDrawUtility.FillRect(board, new RectInt(0, 0, 540, 80), VisualThemePalette.PanelDark);
            FlatVectorDrawUtility.FillRect(board, new RectInt(0, 880, 540, 80), VisualThemePalette.PanelDark);

            var defs = new[]
            {
                ("passenger_office_worker", 0, new Color(0.95f, 0.82f, 0.68f), VisualThemePalette.PassengerAccent[0]),
                ("passenger_delivery", 1, new Color(0.92f, 0.78f, 0.62f), VisualThemePalette.PassengerAccent[1]),
                ("passenger_trainer", 2, new Color(0.88f, 0.72f, 0.58f), VisualThemePalette.PassengerAccent[2]),
                ("passenger_nurse", 3, new Color(0.96f, 0.84f, 0.74f), VisualThemePalette.PassengerAccent[3])
            };

            for (int i = 0; i < defs.Length; i++)
            {
                (string id, _, Color skin, Color accent) = defs[i];
                var mini = FlatVectorDrawUtility.Create(128, 128);
                Color clothes = Color.Lerp(accent, VisualThemePalette.PanelDark, 0.25f);
                WebtoonDrawUtility.DrawPassenger(mini, id, skin, clothes, accent, 0, PassengerPose.Idle);
                mini.Apply();
                FlatVectorDrawUtility.CopyInto(board, mini, 40 + i * 120, 120);
                Object.DestroyImmediate(mini);
            }

            FlatVectorDrawUtility.SavePng(board, "Assets/Art/Source/style_board_webtoon.png");
            Object.DestroyImmediate(board);
        }

        private static void GenerateEnvironment()
        {
            var bg = FlatVectorDrawUtility.Create(540, 960);
            FlatVectorDrawUtility.FillRect(bg, new RectInt(0, 0, 540, 960), VisualThemePalette.CarNavy);
            FlatVectorDrawUtility.FillRect(bg, new RectInt(0, 720, 540, 240), VisualThemePalette.CarNavyLight);
            for (int i = 0; i < 6; i++)
            {
                FlatVectorDrawUtility.FillRoundedRect(
                    bg,
                    new RectInt(40 + i * 80, 820, 60, 90),
                    8,
                    VisualThemePalette.SeatFrame,
                    VisualThemePalette.Outline);
            }

            FlatVectorDrawUtility.FillRect(bg, new RectInt(0, 900, 540, 20), VisualThemePalette.FluorescentTealDim);
            FlatVectorDrawUtility.FillRect(bg, new RectInt(180, 0, 180, 28), VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.SavePng(bg, "Assets/Art/Sprites/Environment/subway_background.png");
            Object.DestroyImmediate(bg);

            var menuBg = FlatVectorDrawUtility.Create(540, 960);
            FlatVectorDrawUtility.FillRect(menuBg, new RectInt(0, 0, 540, 960), VisualThemePalette.CarNavy);
            FlatVectorDrawUtility.FillRect(menuBg, new RectInt(0, 680, 540, 280), VisualThemePalette.CarNavyLight);
            FlatVectorDrawUtility.FillRect(menuBg, new RectInt(0, 900, 540, 20), VisualThemePalette.FluorescentTealDim);
            FlatVectorDrawUtility.FillRect(menuBg, new RectInt(40, 760, 460, 10), VisualThemePalette.WithAlpha(VisualThemePalette.FluorescentTeal, 0.5f));
            FlatVectorDrawUtility.FillRect(menuBg, new RectInt(120, 40, 300, 18), VisualThemePalette.WithAlpha(VisualThemePalette.FluorescentTealDim, 0.65f));
            FlatVectorDrawUtility.SavePng(menuBg, "Assets/Art/Sprites/Environment/main_menu_background.png");
            Object.DestroyImmediate(menuBg);

            var lane = FlatVectorDrawUtility.Create(540, 220);
            WebtoonDrawUtility.DrawEnemyRoute(lane);
            FlatVectorDrawUtility.SavePng(lane, "Assets/Art/Sprites/Environment/spawn_lane.png");
            FlatVectorDrawUtility.SavePng(lane, "Assets/Art/Sprites/Environment/enemy_route_v.png");
            Object.DestroyImmediate(lane);

            var train = FlatVectorDrawUtility.Create(220, 180);
            WebtoonDrawUtility.DrawTrainCar(train);
            FlatVectorDrawUtility.SavePng(train, "Assets/Art/Sprites/Environment/train_target_car.png");
            Object.DestroyImmediate(train);

            var seat = FlatVectorDrawUtility.Create(220, 160);
            FlatVectorDrawUtility.FillRoundedRect(
                seat,
                new RectInt(8, 8, 204, 144),
                14,
                VisualThemePalette.WithAlpha(VisualThemePalette.PanelDark, 0.55f),
                VisualThemePalette.SeatFrame);
            FlatVectorDrawUtility.SavePng(seat, "Assets/Art/Sprites/Environment/seat_frame.png");
            Object.DestroyImmediate(seat);

            var highlight = FlatVectorDrawUtility.Create(220, 160);
            FlatVectorDrawUtility.FillRoundedRect(
                highlight,
                new RectInt(8, 8, 204, 144),
                14,
                VisualThemePalette.WithAlpha(VisualThemePalette.FluorescentTeal, 0.35f),
                VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.SavePng(highlight, "Assets/Art/Sprites/Environment/seat_highlight.png");
            Object.DestroyImmediate(highlight);
        }

        private static void GenerateUi()
        {
            const int slice = 96;
            const int radius = 20;
            SaveRounded("Assets/Art/Sprites/UI/panel.png", slice, slice, VisualThemePalette.PanelMid, VisualThemePalette.Outline, radius);
            SaveRounded("Assets/Art/Sprites/UI/button_normal.png", slice, slice, VisualThemePalette.FluorescentTealDim, VisualThemePalette.Outline, radius);
            SaveRounded("Assets/Art/Sprites/UI/button_pressed.png", slice, slice, VisualThemePalette.FluorescentTeal, VisualThemePalette.Outline, radius);
            SaveRounded("Assets/Art/Sprites/UI/button_disabled.png", slice, slice, VisualThemePalette.PanelMid, VisualThemePalette.SeatFrame, radius);
            SaveRounded("Assets/Art/Sprites/UI/card_frame.png", slice, slice, VisualThemePalette.PanelDark, VisualThemePalette.WarningOrange, radius);
            SaveRounded("Assets/Art/Sprites/UI/popup_dim.png", 64, 64, VisualThemePalette.WithAlpha(VisualThemePalette.Outline, 0.72f), Color.clear, 0);
            SaveBar("Assets/Art/Sprites/UI/hp_bar_bg.png", VisualThemePalette.PanelDark);
            SaveBar("Assets/Art/Sprites/UI/hp_bar_fill.png", VisualThemePalette.VictoryGreen);
            SaveBar("Assets/Art/Sprites/UI/boss_hp_bar_fill.png", VisualThemePalette.AlertRed);

            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_coin.png", "coin", VisualThemePalette.CoinGold);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_station.png", "station", VisualThemePalette.WindowGlow);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_wave.png", "wave", VisualThemePalette.FluorescentTeal);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_ready.png", "ready", VisualThemePalette.VictoryGreen);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_speed.png", "speed", VisualThemePalette.WarningOrange);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_pause.png", "pause", VisualThemePalette.TextLight);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_summon.png", "summon", VisualThemePalette.FluorescentTeal);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_sell.png", "sell", VisualThemePalette.AlertRed);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_reroll.png", "reroll", VisualThemePalette.WindowGlow);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_ad.png", "ad", VisualThemePalette.CoinGold);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_ability.png", "ability", VisualThemePalette.PassengerAccent[5]);
            SaveWebtoonIcon("Assets/Art/Sprites/UI/icon_synergy.png", "synergy", VisualThemePalette.PassengerAccent[4]);

            SaveStarFrame("Assets/Art/Sprites/UI/star_frame_1.png", VisualThemePalette.SeatFrame);
            SaveStarFrame("Assets/Art/Sprites/UI/star_frame_2.png", VisualThemePalette.TextLight);
            SaveStarFrame("Assets/Art/Sprites/UI/star_frame_3.png", VisualThemePalette.CoinGold);

            SaveTitle("Assets/Art/Sprites/UI/main_menu_title.png");
            SaveBanner("Assets/Art/Sprites/UI/result_victory_banner.png", VisualThemePalette.VictoryGreen);
            SaveBanner("Assets/Art/Sprites/UI/result_defeat_banner.png", VisualThemePalette.DefeatRed);
        }

        /// <summary>기존 슬라이스 GUID를 유지한 채 PNG만 덮어쓴다.</summary>
        internal static void RegeneratePassengerAndEnemySprites()
        {
            EnsureFolders();
            GeneratePassengers();
            GenerateEnemies();
        }

        private static void GeneratePassengers()
        {
            var defs = new[]
            {
                ("passenger_office_worker", 0, new Color(0.95f, 0.82f, 0.68f), VisualThemePalette.PassengerAccent[0]),
                ("passenger_delivery", 1, new Color(0.92f, 0.78f, 0.62f), VisualThemePalette.PassengerAccent[1]),
                ("passenger_trainer", 2, new Color(0.88f, 0.72f, 0.58f), VisualThemePalette.PassengerAccent[2]),
                ("passenger_nurse", 3, new Color(0.96f, 0.84f, 0.74f), VisualThemePalette.PassengerAccent[3]),
                ("passenger_developer", 4, new Color(0.9f, 0.8f, 0.66f), VisualThemePalette.PassengerAccent[4]),
                ("passenger_graduate", 5, new Color(0.93f, 0.8f, 0.7f), VisualThemePalette.PassengerAccent[5]),
                ("passenger_police", 6, new Color(0.9f, 0.78f, 0.65f), VisualThemePalette.PassengerAccent[6]),
                ("passenger_cat", 7, new Color(0.98f, 0.9f, 0.75f), VisualThemePalette.PassengerAccent[7]),
                ("passenger_conductor", 8, new Color(0.91f, 0.8f, 0.66f), VisualThemePalette.PassengerAccent[8]),
                ("passenger_barista", 9, new Color(0.94f, 0.82f, 0.7f), VisualThemePalette.PassengerAccent[9]),
                ("passenger_security", 10, new Color(0.88f, 0.76f, 0.62f), VisualThemePalette.PassengerAccent[10]),
                ("passenger_student", 11, new Color(0.96f, 0.84f, 0.72f), VisualThemePalette.PassengerAccent[11])
            };

            for (int i = 0; i < defs.Length; i++)
            {
                (string id, _, Color skin, Color accent) = defs[i];
                Color clothes = Color.Lerp(accent, VisualThemePalette.PanelDark, 0.25f);
                SaveCharacterPortrait(id, skin, clothes, accent);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_idle_sheet.png", id, skin, clothes, accent, PassengerPose.Idle, true);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_attack_sheet.png", id, skin, clothes, accent, PassengerPose.Attack, false);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_skill_sheet.png", id, skin, clothes, accent, PassengerPose.Skill, false);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_merge_sheet.png", id, skin, clothes, accent, PassengerPose.Merge, false);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_hit_sheet.png", id, skin, clothes, accent, PassengerPose.Hit, false);
            }
        }

        internal static void GenerateUnit46Passengers()
        {
            EnsureFolders();
            var defs = new[]
            {
                ("passenger_conductor", new Color(0.91f, 0.8f, 0.66f), VisualThemePalette.PassengerAccent[8]),
                ("passenger_barista", new Color(0.94f, 0.82f, 0.7f), VisualThemePalette.PassengerAccent[9]),
                ("passenger_security", new Color(0.88f, 0.76f, 0.62f), VisualThemePalette.PassengerAccent[10]),
                ("passenger_student", new Color(0.96f, 0.84f, 0.72f), VisualThemePalette.PassengerAccent[11])
            };

            for (int i = 0; i < defs.Length; i++)
            {
                (string id, Color skin, Color accent) = defs[i];
                Color clothes = Color.Lerp(accent, VisualThemePalette.PanelDark, 0.25f);
                SaveCharacterPortrait(id, skin, clothes, accent);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_idle_sheet.png", id, skin, clothes, accent, PassengerPose.Idle, true);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_attack_sheet.png", id, skin, clothes, accent, PassengerPose.Attack, false);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_skill_sheet.png", id, skin, clothes, accent, PassengerPose.Skill, false);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_merge_sheet.png", id, skin, clothes, accent, PassengerPose.Merge, false);
                SaveCharacterSheet($"Assets/Art/Sprites/Characters/{id}_hit_sheet.png", id, skin, clothes, accent, PassengerPose.Hit, false);
            }
        }

        private static void GenerateEnemies()
        {
            SaveEnemy("enemy_normal", VisualThemePalette.EnemyAccent[0], false);
            SaveEnemy("enemy_fast", VisualThemePalette.EnemyAccent[1], false);
            SaveEnemy("enemy_tank", VisualThemePalette.EnemyAccent[2], false);
            SaveEnemy("enemy_boss_drunk_manager", VisualThemePalette.EnemyAccent[3], true);
            SaveEnemy("enemy_split_passenger", VisualThemePalette.EnemyAccent[4], false);
            SaveEnemy("enemy_split_minion", VisualThemePalette.EnemyAccent[5], false);
            SaveEnemy("enemy_aura_watcher", VisualThemePalette.EnemyAccent[6], false);
            SaveEnemy("enemy_seat_blocker", VisualThemePalette.EnemyAccent[7], false);
            SaveEnemy("enemy_boss_final_conductor", VisualThemePalette.EnemyAccent[8], true);
        }

        private static void GenerateProjectiles()
        {
            SaveProjectile("projectile_default", VisualThemePalette.CoinGold, 32);
            SaveProjectile("projectile_office_worker", VisualThemePalette.TextLight, 28);
            SaveProjectile("projectile_delivery", VisualThemePalette.PassengerAccent[1], 30);
            SaveProjectile("projectile_trainer", VisualThemePalette.PassengerAccent[2], 34);
            SaveProjectile("projectile_nurse", VisualThemePalette.PassengerAccent[3], 26);
            SaveProjectile("projectile_developer", VisualThemePalette.PassengerAccent[4], 24);
            SaveProjectile("projectile_graduate", VisualThemePalette.PassengerAccent[5], 36);
            SaveProjectile("projectile_police", VisualThemePalette.PassengerAccent[6], 30);
            SaveProjectile("projectile_cat", VisualThemePalette.PassengerAccent[7], 28);
            SaveProjectile("projectile_turret", VisualThemePalette.FluorescentTeal, 22);
        }

        private static void GenerateVfx()
        {
            SaveBurst("Assets/Art/Sprites/VFX/vfx_hit_sheet.png", VisualThemePalette.TextLight, 48);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_crit_sheet.png", VisualThemePalette.WarningOrange, 64);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_death_sheet.png", VisualThemePalette.AlertRed, 56);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_coin_sheet.png", VisualThemePalette.CoinGold, 40);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_summon_sheet.png", VisualThemePalette.FluorescentTeal, 52);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_merge_sheet.png", VisualThemePalette.CoinGold, 60);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_sell_sheet.png", VisualThemePalette.SeatFrame, 44);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_knockback_sheet.png", VisualThemePalette.WindowGlow, 72);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_heal_sheet.png", VisualThemePalette.VictoryGreen, 56);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_turret_spawn_sheet.png", VisualThemePalette.PassengerAccent[4], 48);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_aoe_sheet.png", VisualThemePalette.PassengerAccent[5], 80);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_boss_enrage_sheet.png", VisualThemePalette.AlertRed, 72);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_boss_portal_sheet.png", VisualThemePalette.EnemyAccent[3], 64);
            SaveBurst("Assets/Art/Sprites/VFX/vfx_debuff_pulse_sheet.png", VisualThemePalette.DefeatRed, 96);
        }

        private static void SaveCharacterPortrait(string id, Color skin, Color clothes, Color accent)
        {
            var tex = FlatVectorDrawUtility.Create(256, 256);
            WebtoonDrawUtility.DrawPassenger(tex, id, skin, clothes, accent, 0, PassengerPose.Idle);
            FlatVectorDrawUtility.SavePng(tex, $"Assets/Art/Sprites/Characters/{id}_portrait.png");
            Object.DestroyImmediate(tex);
        }

        private static void SaveCharacterSheet(
            string path,
            string id,
            Color skin,
            Color clothes,
            Color accent,
            PassengerPose pose,
            bool loop)
        {
            _ = loop;
            Texture2D sheet = FlatVectorDrawUtility.CreateHorizontalSheet(256, 256, FrameCount, frame =>
            {
                var tex = FlatVectorDrawUtility.Create(256, 256);
                WebtoonDrawUtility.DrawPassenger(tex, id, skin, clothes, accent, frame, pose);
                tex.Apply();
                return tex;
            });
            FlatVectorDrawUtility.SavePng(sheet, path);
            Object.DestroyImmediate(sheet);
        }

        private static void SaveEnemy(string id, Color accent, bool boss)
        {
            SaveEnemySheet($"Assets/Art/Sprites/Enemies/{id}_move_sheet.png", id, accent, boss, EnemyPose.Move);
            SaveEnemySheet($"Assets/Art/Sprites/Enemies/{id}_hit_sheet.png", id, accent, boss, EnemyPose.Hit, 2);
            SaveEnemySheet($"Assets/Art/Sprites/Enemies/{id}_death_sheet.png", id, accent, boss, EnemyPose.Death);
            if (boss)
            {
                SaveEnemySheet($"Assets/Art/Sprites/Enemies/{id}_cast_sheet.png", id, accent, true, EnemyPose.Cast);
                SaveEnemySheet($"Assets/Art/Sprites/Enemies/{id}_enraged_sheet.png", id, accent, true, EnemyPose.Enraged);
            }
        }

        private static void SaveEnemySheet(string path, string id, Color accent, bool boss, EnemyPose pose, int count = FrameCount)
        {
            int frameSize = boss ? 256 : 128;
            Texture2D sheet = FlatVectorDrawUtility.CreateHorizontalSheet(frameSize, frameSize, count, frame =>
            {
                var tex = FlatVectorDrawUtility.Create(frameSize, frameSize);
                Color body = Color.Lerp(accent, VisualThemePalette.PanelDark, pose == EnemyPose.Hit ? 0.45f : 0.2f);
                WebtoonDrawUtility.DrawEnemy(tex, id, body, accent, frame, pose, boss);
                tex.Apply();
                return tex;
            });
            FlatVectorDrawUtility.SavePng(sheet, path);
            Object.DestroyImmediate(sheet);
        }

        private static void SaveProjectile(string id, Color color, int size)
        {
            var tex = FlatVectorDrawUtility.Create(size, size);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(2, 2, size - 4, size - 4), size / 4, color, VisualThemePalette.Outline);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, new Vector2(size * 0.5f, size * 0.5f), size * 0.22f, VisualThemePalette.Outline, 2);
            FlatVectorDrawUtility.SavePng(tex, $"Assets/Art/Sprites/Projectiles/{id}.png");
            Object.DestroyImmediate(tex);
        }

        private static void SaveBurst(string path, Color color, int size)
        {
            Texture2D sheet = FlatVectorDrawUtility.CreateHorizontalSheet(size, size, FrameCount, frame =>
            {
                var tex = FlatVectorDrawUtility.Create(size, size);
                float radius = size * (0.15f + frame * 0.12f);
                FlatVectorDrawUtility.FillCircle(
                    tex,
                    new Vector2(size * 0.5f, size * 0.5f),
                    radius,
                    VisualThemePalette.WithAlpha(color, 1f - frame * 0.18f));
                FlatVectorDrawUtility.DrawOutlineCircle(
                    tex,
                    new Vector2(size * 0.5f, size * 0.5f),
                    radius,
                    VisualThemePalette.WithAlpha(VisualThemePalette.Outline, 0.8f),
                    3);
                tex.Apply();
                return tex;
            });
            FlatVectorDrawUtility.SavePng(sheet, path);
            Object.DestroyImmediate(sheet);
        }

        private static void SaveRounded(string path, int w, int h, Color fill, Color outline, int radius)
        {
            var tex = FlatVectorDrawUtility.Create(w, h);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(0, 0, w, h), radius, fill, outline);
            FlatVectorDrawUtility.SavePng(tex, path);
            Object.DestroyImmediate(tex);
        }

        private static void SaveBar(string path, Color fill)
        {
            var tex = FlatVectorDrawUtility.Create(256, 32);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(0, 0, 256, 32), 12, fill, VisualThemePalette.Outline);
            FlatVectorDrawUtility.SavePng(tex, path);
            Object.DestroyImmediate(tex);
        }

        private static void SaveWebtoonIcon(string path, string iconId, Color accent)
        {
            var tex = FlatVectorDrawUtility.Create(64, 64);
            WebtoonDrawUtility.DrawUiIcon(tex, iconId, accent);
            FlatVectorDrawUtility.SavePng(tex, path);
            Object.DestroyImmediate(tex);
        }

        private static void SaveStarFrame(string path, Color color)
        {
            var tex = FlatVectorDrawUtility.Create(220, 160);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(4, 4, 212, 152), 12, Color.clear, color);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, new Vector2(24f, 24f), 8f, color, 2);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, new Vector2(196f, 24f), 8f, color, 2);
            FlatVectorDrawUtility.SavePng(tex, path);
            Object.DestroyImmediate(tex);
        }

        private static void SaveTitle(string path)
        {
            var tex = FlatVectorDrawUtility.Create(640, 180);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(0, 0, 640, 180), 24, VisualThemePalette.PanelDark, VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.FillRect(tex, new RectInt(40, 70, 560, 40), VisualThemePalette.FluorescentTealDim);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(24, 24, 80, 48), 10, VisualThemePalette.CarNavyLight, VisualThemePalette.Outline);
            FlatVectorDrawUtility.SavePng(tex, path);
            Object.DestroyImmediate(tex);
        }

        private static void SaveBanner(string path, Color color)
        {
            var tex = FlatVectorDrawUtility.Create(720, 160);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(0, 0, 720, 160), 28, color, VisualThemePalette.Outline);
            FlatVectorDrawUtility.SavePng(tex, path);
            Object.DestroyImmediate(tex);
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
