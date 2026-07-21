using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>생성된 PNG에 Sprite 임포트 설정과 시트 슬라이스를 적용한다.</summary>
    public static class MvpArtImporter
    {
        private const float PixelsPerUnit = 100f;

        [MenuItem("Tools/막차 생존/MVP Visual/2. Import And Slice Sprites")]
        public static void ImportAll()
        {
            ImportAllInternal(showDialog: true);
        }

        internal static void ImportAllInternal(bool showDialog)
        {
            string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" });
            int processed = 0;
            for (int i = 0; i < pngGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(pngGuids[i]);
                if (!path.EndsWith(".png"))
                {
                    continue;
                }

                ConfigureTexture(path);
                processed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", $"Sprite 임포트/슬라이스 {processed}건 처리했습니다.", "확인");
            }
        }

        public static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        public static Sprite[] LoadSheetSprites(string assetPath, int frameWidth, int frameHeight)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (sprites.Count == 0)
            {
                Sprite single = LoadSprite(assetPath);
                if (single != null)
                {
                    sprites.Add(single);
                }
            }

            return sprites.ToArray();
        }

        private static void ConfigureTexture(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = assetPath.Contains("_sheet")
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            if (!assetPath.Contains("_sheet"))
            {
                return;
            }

            if (!TryGetTextureSize(assetPath, out int width, out int height))
            {
                return;
            }

            int frameWidth = assetPath.Contains("/Enemies/enemy_boss") ? 256 : assetPath.Contains("/Enemies/") ? 128 : 256;
            if (assetPath.Contains("/VFX/"))
            {
                frameWidth = GuessVfxFrameSize(width);
            }

            int frameCount = Mathf.Max(1, width / frameWidth);
            int frameHeight = height;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            var rects = new SpriteRect[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"{baseName}_{i}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

            dataProvider.SetSpriteRects(rects);

            var nameIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameIdProvider != null)
            {
                var pairs = new List<SpriteNameFileIdPair>(frameCount);
                for (int i = 0; i < rects.Length; i++)
                {
                    pairs.Add(new SpriteNameFileIdPair(rects[i].name, rects[i].spriteID));
                }

                nameIdProvider.SetNameFileIdPairs(pairs);
            }

            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static int GuessVfxFrameSize(int sheetWidth)
        {
            if (sheetWidth % 80 == 0)
            {
                return 80;
            }

            if (sheetWidth % 72 == 0)
            {
                return 72;
            }

            if (sheetWidth % 64 == 0)
            {
                return 64;
            }

            if (sheetWidth % 56 == 0)
            {
                return 56;
            }

            if (sheetWidth % 52 == 0)
            {
                return 52;
            }

            if (sheetWidth % 48 == 0)
            {
                return 48;
            }

            return sheetWidth / 4;
        }

        private static bool TryGetTextureSize(string assetPath, out int width, out int height)
        {
            width = 0;
            height = 0;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return false;
            }

            width = texture.width;
            height = texture.height;
            return width > 0 && height > 0;
        }
    }
}
