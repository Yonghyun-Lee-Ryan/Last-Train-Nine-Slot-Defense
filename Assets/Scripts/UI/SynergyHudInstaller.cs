using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTrain.UI
{
    /// <summary>Game Scene에 SynergyHud가 없으면 런타임에 붙인다.</summary>
    public static class SynergyHudInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallIfMissing()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Game")
            {
                return;
            }

            if (Object.FindObjectsByType<SynergyHudController>(FindObjectsInactive.Include).Length > 0)
            {
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;
            var go = new GameObject("SynergyHud", typeof(RectTransform), typeof(SynergyHudController));
            go.transform.SetParent(parent, false);
        }
    }
}
