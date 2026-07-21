using LastTrain.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene에 AbilityPanel이 없으면 런타임에 생성한다.
    /// (에디터 메뉴 Unit11 실행 전에도 역 보상 UI가 동작하도록)
    /// </summary>
    public static class AbilityPanelInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallIfMissing()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Game")
            {
                return;
            }

            AbilityPanelController[] existingPanels = Object.FindObjectsByType<AbilityPanelController>(
                FindObjectsInactive.Include);
            if (existingPanels.Length > 0)
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

            GameBattleBootstrap bootstrap = Object.FindAnyObjectByType<GameBattleBootstrap>();
            GameDatabase database = bootstrap != null ? bootstrap.GameDatabase : null;
            if (database == null)
            {
                database = Resources.Load<GameDatabase>("GameDatabase");
            }

            if (database == null)
            {
                Debug.LogWarning("[AbilityPanelInstaller] GameDatabase를 찾지 못해 AbilityPanel을 생성하지 않습니다.");
                return;
            }

            AbilityPanelUiBuilder.Build(parent, database, bootstrap);
        }
    }
}
