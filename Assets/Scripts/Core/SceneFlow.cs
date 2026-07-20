using UnityEngine;

namespace LastTrain.Core
{
    /// <summary>
    /// Scene 전환 요청을 위한 얇은 헬퍼.
    /// UI 컨트롤러가 AppRoot 접근 코드를 중복 작성하지 않도록 한곳으로 모은다.
    /// 실제 전환은 AppRoot.SceneLoader가 수행한다.
    /// </summary>
    public static class SceneFlow
    {
        public static void Load(string sceneName)
        {
            AppRoot app = AppRoot.Instance;
            if (app == null || app.SceneLoader == null)
            {
                Debug.LogError(
                    $"[SceneFlow] AppRoot가 없어 '{sceneName}'으로 전환할 수 없습니다. " +
                    "Bootstrap Scene부터 실행했는지 확인하세요.");
                return;
            }

            app.SceneLoader.LoadScene(sceneName);
        }
    }
}
