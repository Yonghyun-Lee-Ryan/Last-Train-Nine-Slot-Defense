using LastTrain.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTrain.UI
{
    public static class NonCombatPanelInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnStartup()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            InstallIfMissing(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InstallIfMissing(scene);
        }

        private static void InstallIfMissing(Scene scene)
        {
            if (!scene.IsValid() || scene.name != SceneNames.Game)
            {
                return;
            }

            if (Object.FindAnyObjectByType<NonCombatPanelController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var host = new GameObject("NonCombatPanelHost", typeof(NonCombatPanelController));
            host.transform.SetParent(canvas.transform, false);
            host.transform.SetAsLastSibling();
        }
    }
}
