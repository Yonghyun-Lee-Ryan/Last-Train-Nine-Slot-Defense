using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTrain.Core
{
    /// <summary>
    /// 비동기 Scene 전환을 담당한다.
    /// Single 모드로 로드하여 이전 Scene을 자동 언로드한다.
    /// 전환 진행 중에는 중복 요청을 무시한다.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>전환 시작 시 발행. 인자는 대상 Scene 이름.</summary>
        public event Action<string> SceneLoadStarted;

        /// <summary>전환 진행률(0~1) 갱신 시 발행.</summary>
        public event Action<float> SceneLoadProgress;

        /// <summary>전환 완료 시 발행. 인자는 완료된 Scene 이름.</summary>
        public event Action<string> SceneLoadCompleted;

        public bool IsLoading { get; private set; }

        /// <summary>
        /// 지정한 Scene을 비동기로 로드한다.
        /// 이미 전환 중이면 요청을 무시한다.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene 이름이 비어 있어 전환할 수 없습니다.");
                return;
            }

            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] 이미 전환 중입니다. '{sceneName}' 요청을 무시합니다.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            SceneLoadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] '{sceneName}' Scene을 찾을 수 없습니다. Build Settings 등록을 확인하세요.");
                IsLoading = false;
                yield break;
            }

            while (!operation.isDone)
            {
                SceneLoadProgress?.Invoke(operation.progress);
                yield return null;
            }

            IsLoading = false;
            SceneLoadProgress?.Invoke(1f);
            SceneLoadCompleted?.Invoke(sceneName);
        }
    }
}
