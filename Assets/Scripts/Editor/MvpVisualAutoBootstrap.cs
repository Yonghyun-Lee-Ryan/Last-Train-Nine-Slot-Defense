using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// VisualDatabase가 없거나 PNG만 있을 때 에디터 로드 시 자동으로 임포트/빌드를 시도한다.
    /// </summary>
    [InitializeOnLoad]
    public static class MvpVisualAutoBootstrap
    {
        private const string SessionKey = "LastTrain.MvpVisualAutoBootstrapped";

        static MvpVisualAutoBootstrap()
        {
            EditorApplication.delayCall += TryBootstrap;
        }

        private static void TryBootstrap()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Art/Sprites"))
            {
                return;
            }

            VisualDatabase existing = AssetDatabase.LoadAssetAtPath<VisualDatabase>(VisualDatabaseLocator.AssetPath);
            if (existing != null && existing.Theme != null && existing.Passengers.Count > 0)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            string[] pngs = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" });
            if (pngs.Length == 0)
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            MvpArtImporter.ImportAllInternal(showDialog: false);
            MvpVisualDataBuilder.BuildAllInternal(showDialog: false);
            Debug.Log("[MvpVisualAutoBootstrap] VisualDatabase 자동 생성을 완료했습니다.");
        }
    }
}
