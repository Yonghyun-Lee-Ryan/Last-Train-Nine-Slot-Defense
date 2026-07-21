using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.Data
{
    /// <summary>VisualTheme 로드 헬퍼.</summary>
    public static class VisualThemeLocator
    {
        public const string AssetPath = "Assets/Data/Visual/VisualTheme.asset";
        public const string ResourcesName = "VisualTheme";

        public static VisualTheme Load()
        {
            VisualTheme theme = Resources.Load<VisualTheme>(ResourcesName);
            if (theme != null)
            {
                return theme;
            }

#if UNITY_EDITOR
            theme = AssetDatabase.LoadAssetAtPath<VisualTheme>(AssetPath);
#endif
            return theme;
        }
    }
}
