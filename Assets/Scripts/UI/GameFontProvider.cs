using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>게임 전역에서 사용하는 OFL 한글 폰트를 로드하고 모든 legacy UI Text에 적용한다.</summary>
    public static class GameFontProvider
    {
        private const string ResourcePath = "Fonts/Jua-Regular";
        private static Font _cachedFont;
        private static bool _loggedMissingFont;

        public static Font Get()
        {
            if (_cachedFont == null)
            {
                _cachedFont = Resources.Load<Font>(ResourcePath);
                if (_cachedFont == null && !_loggedMissingFont)
                {
                    _loggedMissingFont = true;
                    Debug.LogError(
                        "[GameFontProvider] Resources/Fonts/Jua-Regular 로드 실패 — 한글이 ???로 보일 수 있습니다.");
                }
            }

            return _cachedFont != null
                ? _cachedFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static void ApplyTo(GameObject root)
        {
            Font font = Get();
            if (root == null || font == null)
            {
                return;
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = font;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyInitialScene()
        {
            ApplyToScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToScene(scene);
        }

        private static void ApplyToScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                ApplyTo(roots[i]);
            }
        }
    }
}
