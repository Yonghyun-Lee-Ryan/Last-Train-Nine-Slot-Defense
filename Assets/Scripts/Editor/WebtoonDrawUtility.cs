using System;
using LastTrain.Data;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>README 27 웹툰 스타일(두꺼운 외곽선·과장 표정·직업 소품) 프로시저럴 드로잉.</summary>
    internal static class WebtoonDrawUtility
    {
        private const int OutlineThickness = 4;

        public static void DrawPassenger(
            Texture2D tex,
            string passengerId,
            Color skin,
            Color clothes,
            Color accent,
            int frameIndex,
            PassengerPose pose)
        {
            int w = tex.width;
            int h = tex.height;
            float bob = pose == PassengerPose.Idle ? Mathf.Sin(frameIndex * Mathf.PI * 0.5f) * 5f : 0f;
            if (pose == PassengerPose.Merge)
            {
                bob = Mathf.Sin(frameIndex * Mathf.PI * 0.5f) * 8f;
            }

            var center = new Vector2(w * 0.5f, h * 0.38f + bob);
            float scale = pose == PassengerPose.Merge ? 1f + frameIndex * 0.04f : 1f;
            DrawSharedBody(tex, center, scale, skin, clothes, accent, pose, frameIndex);
            DrawPassengerHair(tex, passengerId, center, scale, accent, clothes);
            DrawPassengerFace(tex, center, scale, skin, pose, frameIndex);
            DrawPassengerProp(tex, passengerId, center, scale, accent, clothes, pose, frameIndex);
            if (pose == PassengerPose.Merge)
            {
                DrawSparkleRing(tex, center + new Vector2(0f, 10f), 52f + frameIndex * 10f, accent);
            }

            if (pose == PassengerPose.Hit)
            {
                DrawSurpriseMarks(tex, center + new Vector2(48f, 58f));
            }
        }

        public static void DrawEnemy(
            Texture2D tex,
            string enemyId,
            Color body,
            Color accent,
            int frameIndex,
            EnemyPose pose,
            bool boss)
        {
            int w = tex.width;
            int h = tex.height;
            float sway = pose == EnemyPose.Move ? Mathf.Sin(frameIndex * Mathf.PI * 0.5f) * (boss ? 3f : 6f) : 0f;
            var center = new Vector2(w * 0.5f + sway, h * 0.44f);
            Color fill = pose == EnemyPose.Death
                ? Color.Lerp(body, Color.black, 0.25f + frameIndex * 0.15f)
                : pose == EnemyPose.Hit
                    ? Color.Lerp(body, VisualThemePalette.TextLight, 0.35f)
                    : body;

            switch (enemyId)
            {
                case "enemy_fast":
                    DrawFastEnemy(tex, center, fill, accent, boss, pose, frameIndex);
                    break;
                case "enemy_tank":
                    DrawTankEnemy(tex, center, fill, accent, boss, pose, frameIndex);
                    break;
                case "enemy_boss_drunk_manager":
                    DrawDrunkBoss(tex, center, fill, accent, pose, frameIndex);
                    break;
                case "enemy_split_passenger":
                case "enemy_split_minion":
                    DrawSplitEnemy(tex, center, fill, accent, enemyId.EndsWith("minion"), pose, frameIndex);
                    break;
                case "enemy_aura_watcher":
                    DrawWatcherEnemy(tex, center, fill, accent, pose, frameIndex);
                    break;
                case "enemy_seat_blocker":
                    DrawSeatBlockerEnemy(tex, center, fill, accent, pose, frameIndex);
                    break;
                case "enemy_boss_final_conductor":
                    DrawConductorBoss(tex, center, fill, accent, pose, frameIndex);
                    break;
                default:
                    DrawNormalEnemy(tex, center, fill, accent, boss, pose, frameIndex);
                    break;
            }
        }

        public static void DrawUiIcon(Texture2D tex, string iconId, Color accent)
        {
            int size = tex.width;
            var c = new Vector2(size * 0.5f, size * 0.5f);
            FlatVectorDrawUtility.FillCircle(tex, c, size * 0.38f, VisualThemePalette.WithAlpha(VisualThemePalette.PanelDark, 0.92f));
            FlatVectorDrawUtility.DrawOutlineCircle(tex, c, size * 0.38f, VisualThemePalette.Outline, OutlineThickness);

            switch (iconId)
            {
                case "coin":
                    FlatVectorDrawUtility.FillCircle(tex, c, size * 0.2f, VisualThemePalette.CoinGold);
                    FlatVectorDrawUtility.DrawOutlineCircle(tex, c, size * 0.2f, VisualThemePalette.Outline, 2);
                    break;
                case "station":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(size / 2 - 14, size / 2 - 10, 28, 20), 4, accent, VisualThemePalette.Outline);
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(-10f, 14f), c + new Vector2(10f, 14f), VisualThemePalette.Outline, 3);
                    break;
                case "wave":
                    for (int i = -1; i <= 1; i++)
                    {
                        FlatVectorDrawUtility.DrawOutlineCircle(tex, c + new Vector2(i * 12f, 0f), 8f, accent, 2);
                    }
                    break;
                case "ready":
                    FlatVectorDrawUtility.FillCircle(tex, c, size * 0.16f, accent);
                    break;
                case "speed":
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(-14f, -8f), c + new Vector2(14f, 8f), accent, 5);
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(-6f, -8f), c + new Vector2(14f, 8f), VisualThemePalette.Outline, 2);
                    break;
                case "pause":
                    FlatVectorDrawUtility.FillRect(tex, new RectInt(size / 2 - 12, size / 2 - 14, 8, 28), accent);
                    FlatVectorDrawUtility.FillRect(tex, new RectInt(size / 2 + 4, size / 2 - 14, 8, 28), accent);
                    break;
                case "summon":
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(0f, -14f), c + new Vector2(0f, 14f), accent, 5);
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(-14f, 0f), c + new Vector2(14f, 0f), accent, 5);
                    break;
                case "sell":
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(-12f, -12f), c + new Vector2(12f, 12f), accent, 5);
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(12f, -12f), c + new Vector2(-12f, 12f), accent, 5);
                    break;
                case "reroll":
                    FlatVectorDrawUtility.DrawOutlineCircle(tex, c, 14f, accent, 3);
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(10f, 10f), c + new Vector2(18f, 18f), accent, 4);
                    break;
                case "ad":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(size / 2 - 16, size / 2 - 10, 32, 20), 4, accent, VisualThemePalette.Outline);
                    FlatVectorDrawUtility.FillRect(tex, new RectInt(size / 2 - 4, size / 2 - 2, 8, 8), VisualThemePalette.TextLight);
                    break;
                case "ability":
                    FlatVectorDrawUtility.FillCircle(tex, c + new Vector2(0f, 4f), 10f, accent);
                    FlatVectorDrawUtility.DrawLine(tex, c + new Vector2(0f, -14f), c + new Vector2(0f, -4f), accent, 4);
                    break;
                case "synergy":
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 120f * Mathf.Deg2Rad;
                        FlatVectorDrawUtility.FillCircle(tex, c + new Vector2(Mathf.Cos(a) * 12f, Mathf.Sin(a) * 12f), 6f, accent);
                    }
                    break;
                default:
                    FlatVectorDrawUtility.FillCircle(tex, c, size * 0.18f, accent);
                    break;
            }
        }

        public static void DrawTrainCar(Texture2D tex)
        {
            int w = tex.width;
            int h = tex.height;
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(8, 24, w - 16, h - 48), 18, VisualThemePalette.CarNavyLight, VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(24, 40, w - 48, h - 80), 12, VisualThemePalette.WindowGlow, VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillRect(tex, new RectInt(w / 2 - 30, 12, 60, 16), VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, new Vector2(w * 0.5f, h - 18f), 14f, VisualThemePalette.SeatFrame, 3);
        }

        public static void DrawEnemyRoute(Texture2D tex)
        {
            int w = tex.width;
            int h = tex.height;
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt(10, 20, w - 20, h - 40), 24, VisualThemePalette.WithAlpha(VisualThemePalette.PanelMid, 0.9f), VisualThemePalette.FluorescentTeal);
            for (int i = 0; i < 5; i++)
            {
                float x = 40f + i * ((w - 80f) / 4f);
                FlatVectorDrawUtility.DrawLine(tex, new Vector2(x, 40f), new Vector2(x, h - 40f), VisualThemePalette.WithAlpha(VisualThemePalette.FluorescentTealDim, 0.5f), 3);
            }

            FlatVectorDrawUtility.DrawLine(tex, new Vector2(30f, h * 0.5f), new Vector2(w - 30f, h * 0.5f), VisualThemePalette.WarningOrange, 5);
        }

        private static void DrawSharedBody(
            Texture2D tex,
            Vector2 center,
            float scale,
            Color skin,
            Color clothes,
            Color accent,
            PassengerPose pose,
            int frameIndex)
        {
            float headR = 36f * scale;
            Vector2 head = center + new Vector2(0f, 38f * scale);
            FlatVectorDrawUtility.FillCircle(tex, head, headR, skin);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, head, headR, VisualThemePalette.Outline, OutlineThickness);

            int bodyW = Mathf.RoundToInt(78f * scale);
            int bodyH = Mathf.RoundToInt(72f * scale);
            FlatVectorDrawUtility.FillRoundedRect(
                tex,
                new RectInt((int)(center.x - bodyW * 0.5f), (int)(center.y - 8f * scale), bodyW, bodyH),
                Mathf.RoundToInt(18f * scale),
                clothes,
                VisualThemePalette.Outline);

            float armAngle = pose switch
            {
                PassengerPose.Attack => -40f - frameIndex * 10f,
                PassengerPose.Skill => 30f + frameIndex * 8f,
                PassengerPose.Merge => frameIndex * 12f,
                PassengerPose.Hit => -20f + frameIndex * 6f,
                _ => frameIndex * 6f
            };
            Vector2 shoulder = center + new Vector2(0f, 20f * scale);
            Vector2 arm = shoulder + new Vector2(Mathf.Cos(armAngle * Mathf.Deg2Rad) * 34f * scale, Mathf.Sin(armAngle * Mathf.Deg2Rad) * 22f * scale);
            FlatVectorDrawUtility.DrawLine(tex, shoulder, arm, skin, Mathf.RoundToInt(9f * scale));
            FlatVectorDrawUtility.FillCircle(tex, arm, 9f * scale, accent);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, arm, 9f * scale, VisualThemePalette.Outline, 2);
        }

        private static void DrawPassengerHair(Texture2D tex, string id, Vector2 center, float scale, Color accent, Color clothes)
        {
            Color hair = Color.Lerp(clothes, VisualThemePalette.Outline, 0.35f);
            Vector2 head = center + new Vector2(0f, 38f * scale);
            switch (id)
            {
                case "passenger_delivery":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(head.x - 30f * scale), (int)(head.y + 8f * scale), (int)(60f * scale), (int)(18f * scale)), 8, accent, VisualThemePalette.Outline);
                    break;
                case "passenger_nurse":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(head.x - 34f * scale), (int)(head.y + 18f * scale), (int)(68f * scale), (int)(14f * scale)), 6, VisualThemePalette.TextLight, VisualThemePalette.Outline);
                    break;
                case "passenger_police":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(head.x - 32f * scale), (int)(head.y + 20f * scale), (int)(64f * scale), (int)(16f * scale)), 4, VisualThemePalette.Outline, VisualThemePalette.Outline);
                    break;
                case "passenger_cat":
                    FlatVectorDrawUtility.DrawLine(tex, head + new Vector2(-22f, 42f) * scale, head + new Vector2(-10f, 58f) * scale, accent, 5);
                    FlatVectorDrawUtility.DrawLine(tex, head + new Vector2(22f, 42f) * scale, head + new Vector2(10f, 58f) * scale, accent, 5);
                    break;
                default:
                    FlatVectorDrawUtility.FillCircle(tex, head + new Vector2(0f, 16f * scale), 34f * scale, hair);
                    FlatVectorDrawUtility.DrawOutlineCircle(tex, head + new Vector2(0f, 16f * scale), 34f * scale, VisualThemePalette.Outline, 3);
                    break;
            }
        }

        private static void DrawPassengerFace(Texture2D tex, Vector2 center, float scale, Color skin, PassengerPose pose, int frameIndex)
        {
            Vector2 head = center + new Vector2(0f, 38f * scale);
            float eyeY = 6f * scale;
            float eyeOffset = 12f * scale;
            float eyeR = pose == PassengerPose.Hit ? 7f * scale : 5f * scale;
            FlatVectorDrawUtility.FillCircle(tex, head + new Vector2(-eyeOffset, eyeY), eyeR, VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillCircle(tex, head + new Vector2(eyeOffset, eyeY), eyeR, VisualThemePalette.Outline);
            if (pose == PassengerPose.Hit)
            {
                FlatVectorDrawUtility.FillCircle(tex, head + new Vector2(-eyeOffset, eyeY + 2f), 2.5f * scale, VisualThemePalette.TextLight);
                FlatVectorDrawUtility.FillCircle(tex, head + new Vector2(eyeOffset, eyeY + 2f), 2.5f * scale, VisualThemePalette.TextLight);
            }

            if (pose == PassengerPose.Attack || pose == PassengerPose.Skill || pose == PassengerPose.Hit)
            {
                FlatVectorDrawUtility.FillCircle(tex, head + new Vector2(0f, -8f * scale), 6f * scale, VisualThemePalette.AlertRed);
            }
            else if (pose == PassengerPose.Merge)
            {
                FlatVectorDrawUtility.DrawLine(tex, head + new Vector2(-8f, -8f) * scale, head + new Vector2(8f, -8f) * scale, VisualThemePalette.Outline, 3);
            }
            else
            {
                FlatVectorDrawUtility.DrawLine(tex, head + new Vector2(-6f, -8f) * scale, head + new Vector2(6f, -8f) * scale, VisualThemePalette.Outline, 2);
            }
        }

        private static void DrawPassengerProp(
            Texture2D tex,
            string id,
            Vector2 center,
            float scale,
            Color accent,
            Color clothes,
            PassengerPose pose,
            int frameIndex)
        {
            Vector2 hand = center + new Vector2(34f * scale, 18f * scale);
            switch (id)
            {
                case "passenger_office_worker":
                    FlatVectorDrawUtility.FillRect(tex, new RectInt((int)(center.x - 8f * scale), (int)(center.y + 8f * scale), (int)(16f * scale), (int)(28f * scale)), accent);
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(hand.x - 10f), (int)(hand.y - 14f), 20, 24), 4, VisualThemePalette.PanelMid, VisualThemePalette.Outline);
                    break;
                case "passenger_delivery":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(hand.x - 14f), (int)(hand.y - 10f), 28, 22), 5, accent, VisualThemePalette.Outline);
                    break;
                case "passenger_trainer":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(hand.x - 16f), (int)(hand.y - 8f), 32, 16), 6, accent, VisualThemePalette.Outline);
                    FlatVectorDrawUtility.DrawLine(tex, hand + new Vector2(-16f, 0f), hand + new Vector2(16f, 0f), VisualThemePalette.Outline, 3);
                    break;
                case "passenger_nurse":
                    FlatVectorDrawUtility.DrawLine(tex, hand + new Vector2(0f, -10f), hand + new Vector2(0f, 10f), VisualThemePalette.TextLight, 4);
                    FlatVectorDrawUtility.DrawLine(tex, hand + new Vector2(-10f, 0f), hand + new Vector2(10f, 0f), VisualThemePalette.TextLight, 4);
                    break;
                case "passenger_developer":
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(hand.x - 16f), (int)(hand.y - 12f), 32, 22), 4, VisualThemePalette.PanelDark, VisualThemePalette.Outline);
                    FlatVectorDrawUtility.FillRect(tex, new RectInt((int)(hand.x - 10f), (int)(hand.y - 6f), 20, 10), accent);
                    break;
                case "passenger_graduate":
                    FlatVectorDrawUtility.FillRect(tex, new RectInt((int)(center.x - 26f * scale), (int)(center.y + 52f * scale), (int)(52f * scale), 6), VisualThemePalette.Outline);
                    FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(hand.x - 8f), (int)(hand.y - 14f), 16, 22), 3, VisualThemePalette.TextLight, VisualThemePalette.Outline);
                    break;
                case "passenger_police":
                    FlatVectorDrawUtility.FillCircle(tex, hand, 10f, VisualThemePalette.CoinGold);
                    FlatVectorDrawUtility.DrawOutlineCircle(tex, hand, 10f, VisualThemePalette.Outline, 2);
                    break;
                case "passenger_cat":
                    FlatVectorDrawUtility.DrawLine(tex, center + new Vector2(-20f, 24f) * scale, center + new Vector2(-34f, 30f) * scale, VisualThemePalette.Outline, 2);
                    FlatVectorDrawUtility.DrawLine(tex, center + new Vector2(20f, 24f) * scale, center + new Vector2(34f, 30f) * scale, VisualThemePalette.Outline, 2);
                    break;
            }

            if (pose == PassengerPose.Skill)
            {
                DrawSparkleRing(tex, center + new Vector2(0f, 20f), 40f + frameIndex * 8f, accent);
            }
        }

        private static void DrawNormalEnemy(Texture2D tex, Vector2 center, Color body, Color accent, bool boss, EnemyPose pose, int frame)
        {
            float r = boss ? 40f : 28f;
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(0f, 20f), r, body);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, center + new Vector2(0f, 20f), r, VisualThemePalette.Outline, OutlineThickness);
            DrawEnemyEyes(tex, center + new Vector2(0f, 28f), pose, accent);
        }

        private static void DrawFastEnemy(Texture2D tex, Vector2 center, Color body, Color accent, bool boss, EnemyPose pose, int frame)
        {
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(center.x - 34f), (int)(center.y - 6f), 68, 44), 18, body, VisualThemePalette.Outline);
            for (int i = 0; i < 3; i++)
            {
                FlatVectorDrawUtility.DrawLine(tex, center + new Vector2(-50f - i * 8f, 10f + i * 4f), center + new Vector2(-30f - i * 8f, 10f + i * 4f), accent, 3);
            }

            DrawEnemyEyes(tex, center + new Vector2(0f, 18f), pose, accent);
        }

        private static void DrawTankEnemy(Texture2D tex, Vector2 center, Color body, Color accent, bool boss, EnemyPose pose, int frame)
        {
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(center.x - 42f), (int)(center.y - 18f), 84, 64), 10, body, VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillRect(tex, new RectInt((int)(center.x - 50f), (int)(center.y + 8f), 12, 24), accent);
            FlatVectorDrawUtility.FillRect(tex, new RectInt((int)(center.x + 38f), (int)(center.y + 8f), 12, 24), accent);
            DrawEnemyEyes(tex, center + new Vector2(0f, 16f), pose, accent);
        }

        private static void DrawDrunkBoss(Texture2D tex, Vector2 center, Color body, Color accent, EnemyPose pose, int frame)
        {
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(center.x - 56f), (int)(center.y - 24f), 112, 88), 20, body, VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(center.x + 28f), (int)(center.y + 8f), 18, 42), 6, accent, VisualThemePalette.Outline);
            DrawEnemyEyes(tex, center + new Vector2(0f, 24f), pose, VisualThemePalette.WarningOrange);
        }

        private static void DrawSplitEnemy(Texture2D tex, Vector2 center, Color body, Color accent, bool minion, EnemyPose pose, int frame)
        {
            float split = minion ? 10f : 18f;
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(-split, 16f), minion ? 20f : 28f, body);
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(split, 16f), minion ? 20f : 28f, Color.Lerp(body, accent, 0.3f));
            FlatVectorDrawUtility.DrawOutlineCircle(tex, center + new Vector2(-split, 16f), minion ? 20f : 28f, VisualThemePalette.Outline, 3);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, center + new Vector2(split, 16f), minion ? 20f : 28f, VisualThemePalette.Outline, 3);
            DrawEnemyEyes(tex, center + new Vector2(0f, 22f), pose, accent);
        }

        private static void DrawWatcherEnemy(Texture2D tex, Vector2 center, Color body, Color accent, EnemyPose pose, int frame)
        {
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(0f, 18f), 34f, body);
            FlatVectorDrawUtility.DrawOutlineCircle(tex, center + new Vector2(0f, 18f), 34f, VisualThemePalette.Outline, OutlineThickness);
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(0f, 20f), 16f, accent);
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(0f, 20f), 6f, VisualThemePalette.Outline);
        }

        private static void DrawSeatBlockerEnemy(Texture2D tex, Vector2 center, Color body, Color accent, EnemyPose pose, int frame)
        {
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(center.x - 38f), (int)(center.y - 10f), 76, 52), 12, body, VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillRect(tex, new RectInt((int)(center.x - 30f), (int)(center.y + 16f), 60, 8), accent);
            DrawEnemyEyes(tex, center + new Vector2(0f, 14f), pose, accent);
        }

        private static void DrawConductorBoss(Texture2D tex, Vector2 center, Color body, Color accent, EnemyPose pose, int frame)
        {
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(center.x - 64f), (int)(center.y - 30f), 128, 96), 22, body, VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillRoundedRect(tex, new RectInt((int)(center.x - 40f), (int)(center.y + 34f), 80, 18), 6, VisualThemePalette.Outline, VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillRect(tex, new RectInt((int)(center.x - 8f), (int)(center.y + 44f), 16, 22), VisualThemePalette.CoinGold);
            DrawEnemyEyes(tex, center + new Vector2(0f, 26f), pose, VisualThemePalette.AlertRed);
            if (pose == EnemyPose.Enraged)
            {
                DrawSparkleRing(tex, center, 70f + frame * 12f, VisualThemePalette.AlertRed);
            }
        }

        private static void DrawEnemyEyes(Texture2D tex, Vector2 center, EnemyPose pose, Color accent)
        {
            float eye = pose == EnemyPose.Death ? 2f : 5f;
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(-10f, 0f), eye, accent);
            FlatVectorDrawUtility.FillCircle(tex, center + new Vector2(10f, 0f), eye, accent);
        }

        private static void DrawSparkleRing(Texture2D tex, Vector2 center, float radius, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                Vector2 p = center + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
                FlatVectorDrawUtility.FillCircle(tex, p, 4f, VisualThemePalette.WithAlpha(color, 0.85f));
            }
        }

        private static void DrawSurpriseMarks(Texture2D tex, Vector2 at)
        {
            FlatVectorDrawUtility.DrawLine(tex, at, at + new Vector2(8f, 12f), VisualThemePalette.WindowGlow, 3);
            FlatVectorDrawUtility.DrawLine(tex, at + new Vector2(12f, 0f), at + new Vector2(20f, 8f), VisualThemePalette.WindowGlow, 3);
        }
    }
}
