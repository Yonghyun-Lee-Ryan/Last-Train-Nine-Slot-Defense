using System;
using System.IO;
using LastTrain.Data;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>플랫 벡터 PNG를 코드로 그리는 유틸리티.</summary>
    internal static class FlatVectorDrawUtility
    {
        public static Texture2D Create(int width, int height, Color clear = default)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color fill = clear.a <= 0f ? Color.clear : clear;
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fill;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static void FillRect(Texture2D tex, RectInt rect, Color color)
        {
            int xMin = Mathf.Clamp(rect.xMin, 0, tex.width);
            int yMin = Mathf.Clamp(rect.yMin, 0, tex.height);
            int xMax = Mathf.Clamp(rect.xMax, 0, tex.width);
            int yMax = Mathf.Clamp(rect.yMax, 0, tex.height);
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    BlendPixel(tex, x, y, color);
                }
            }
        }

        public static void FillRoundedRect(Texture2D tex, RectInt rect, int radius, Color fill, Color outline)
        {
            int xMin = rect.xMin;
            int yMin = rect.yMin;
            int xMax = rect.xMax;
            int yMax = rect.yMax;
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    if (!InsideRoundedRect(x, y, xMin, yMin, xMax, yMax, radius))
                    {
                        continue;
                    }

                    bool edge = !InsideRoundedRect(x, y, xMin + 2, yMin + 2, xMax - 2, yMax - 2, Mathf.Max(0, radius - 2));
                    BlendPixel(tex, x, y, edge ? outline : fill);
                }
            }
        }

        public static void FillCircle(Texture2D tex, Vector2 center, float radius, Color color)
        {
            int minX = Mathf.FloorToInt(center.x - radius);
            int maxX = Mathf.CeilToInt(center.x + radius);
            int minY = Mathf.FloorToInt(center.y - radius);
            int maxY = Mathf.CeilToInt(center.y + radius);
            float r2 = radius * radius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    if (dx * dx + dy * dy <= r2)
                    {
                        BlendPixel(tex, x, y, color);
                    }
                }
            }
        }

        public static void DrawOutlineCircle(Texture2D tex, Vector2 center, float radius, Color color, int thickness = 2)
        {
            for (int t = 0; t < thickness; t++)
            {
                float r = radius - t;
                int minX = Mathf.FloorToInt(center.x - r);
                int maxX = Mathf.CeilToInt(center.x + r);
                int minY = Mathf.FloorToInt(center.y - r);
                int maxY = Mathf.CeilToInt(center.y + r);
                float inner = (r - 1.5f) * (r - 1.5f);
                float outer = (r + 0.5f) * (r + 0.5f);
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float dx = x - center.x;
                        float dy = y - center.y;
                        float d2 = dx * dx + dy * dy;
                        if (d2 <= outer && d2 >= inner)
                        {
                            BlendPixel(tex, x, y, color);
                        }
                    }
                }
            }
        }

        public static void DrawLine(Texture2D tex, Vector2 a, Vector2 b, Color color, int thickness = 2)
        {
            float distance = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = Vector2.Lerp(a, b, t);
                FillCircle(tex, p, thickness * 0.5f, color);
            }
        }

        public static Texture2D CreateHorizontalSheet(int frameWidth, int frameHeight, int frameCount, Func<int, Texture2D> frameFactory)
        {
            var sheet = Create(frameWidth * frameCount, frameHeight);
            for (int i = 0; i < frameCount; i++)
            {
                Texture2D frame = frameFactory(i);
                CopyInto(sheet, frame, i * frameWidth, 0);
                UnityEngine.Object.DestroyImmediate(frame);
            }

            sheet.Apply();
            return sheet;
        }

        public static void CopyInto(Texture2D target, Texture2D source, int destX, int destY)
        {
            Color[] pixels = source.GetPixels();
            target.SetPixels(destX, destY, source.width, source.height, pixels);
        }

        public static void SavePng(Texture2D texture, string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        }

        public static void DrawSdCharacter(
            Texture2D tex,
            Color skin,
            Color clothes,
            Color accent,
            int frameIndex,
            bool attacking,
            bool skillPose)
        {
            int w = tex.width;
            int h = tex.height;
            float bob = Mathf.Sin(frameIndex * Mathf.PI * 0.5f) * 4f;
            var center = new Vector2(w * 0.5f, h * 0.42f + bob);
            FillCircle(tex, center + new Vector2(0f, 34f), 34f, skin);
            DrawOutlineCircle(tex, center + new Vector2(0f, 34f), 34f, VisualThemePalette.Outline, 3);
            FillRoundedRect(tex, new RectInt(w / 2 - 38, (int)(center.y - 10f), 76, 70), 16, clothes, VisualThemePalette.Outline);
            FillCircle(tex, center + new Vector2(-10f, 40f), 4f, VisualThemePalette.Outline);
            FillCircle(tex, center + new Vector2(10f, 40f), 4f, VisualThemePalette.Outline);

            float armAngle = attacking ? -35f - frameIndex * 8f : skillPose ? 25f + frameIndex * 6f : frameIndex * 5f;
            Vector2 arm = center + new Vector2(Mathf.Cos(armAngle * Mathf.Deg2Rad) * 28f, Mathf.Sin(armAngle * Mathf.Deg2Rad) * 18f + 18f);
            DrawLine(tex, center + new Vector2(0f, 18f), arm, skin, 8);
            FillCircle(tex, arm, 8f, accent);
        }

        public static void DrawEnemySilhouette(Texture2D tex, Color body, Color accent, int frameIndex, bool boss)
        {
            int w = tex.width;
            int h = tex.height;
            float sway = Mathf.Sin(frameIndex * Mathf.PI * 0.5f) * (boss ? 2f : 5f);
            var center = new Vector2(w * 0.5f + sway, h * 0.45f);
            float bodyW = boss ? 0.42f : 0.34f;
            FillRoundedRect(
                tex,
                new RectInt(
                    (int)(w * (0.5f - bodyW)),
                    (int)(center.y - 20f),
                    (int)(w * bodyW * 2f),
                    (int)(h * 0.42f)),
                boss ? 24 : 16,
                body,
                VisualThemePalette.Outline);
            FillCircle(tex, center + new Vector2(0f, 36f), boss ? 36f : 26f, accent);
            DrawOutlineCircle(tex, center + new Vector2(-8f, 40f), 5f, VisualThemePalette.AlertRed, 2);
            DrawOutlineCircle(tex, center + new Vector2(8f, 40f), 5f, VisualThemePalette.AlertRed, 2);
        }

        private static bool InsideRoundedRect(int x, int y, int xMin, int yMin, int xMax, int yMax, int radius)
        {
            if (x < xMin || x >= xMax || y < yMin || y >= yMax)
            {
                return false;
            }

            radius = Mathf.Max(0, radius);
            if (x >= xMin + radius && x < xMax - radius)
            {
                return true;
            }

            if (y >= yMin + radius && y < yMax - radius)
            {
                return true;
            }

            Vector2 corner = new Vector2(
                x < xMin + radius ? xMin + radius : xMax - radius - 1,
                y < yMin + radius ? yMin + radius : yMax - radius - 1);
            float dx = x - corner.x;
            float dy = y - corner.y;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height || color.a <= 0f)
            {
                return;
            }

            Color existing = tex.GetPixel(x, y);
            float alpha = color.a + existing.a * (1f - color.a);
            if (alpha <= 0f)
            {
                tex.SetPixel(x, y, Color.clear);
                return;
            }

            Color blended = new Color(
                (color.r * color.a + existing.r * existing.a * (1f - color.a)) / alpha,
                (color.g * color.a + existing.g * existing.a * (1f - color.a)) / alpha,
                (color.b * color.a + existing.b * existing.a * (1f - color.a)) / alpha,
                alpha);
            tex.SetPixel(x, y, blended);
        }
    }
}
