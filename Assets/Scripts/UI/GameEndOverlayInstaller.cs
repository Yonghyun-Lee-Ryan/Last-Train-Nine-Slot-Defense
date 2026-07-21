using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTrain.UI
{
    /// <summary>Game Scene에 종료 오버레이가 없으면 런타임에 붙인다.</summary>
    public static class GameEndOverlayInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallIfMissing()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Game")
            {
                return;
            }

            if (Object.FindObjectsByType<GameEndOverlayController>(FindObjectsInactive.Include).Length > 0)
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
            var go = new GameObject("GameEndOverlayRoot", typeof(RectTransform), typeof(GameEndOverlayController));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
