using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace LastTrain.UI
{
    /// <summary>
    /// EventSystem의 UI 입력 모듈이 New Input System 모드와 맞게 준비됐는지 보장한다.
    /// </summary>
    public static class UiInputBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureAfterFirstScene()
        {
            EnsureActiveScene();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInScene(scene);
        }

        private static void EnsureActiveScene()
        {
            EnsureInScene(SceneManager.GetActiveScene());
        }

        private static void EnsureInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                EventSystem[] systems = roots[i].GetComponentsInChildren<EventSystem>(true);
                for (int s = 0; s < systems.Length; s++)
                {
                    EnsureModule(systems[s]);
                }
            }
        }

        private static void EnsureModule(EventSystem eventSystem)
        {
            if (eventSystem == null)
            {
                return;
            }

            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
            }

            InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();
            }

            module.enabled = true;
        }
    }
}
