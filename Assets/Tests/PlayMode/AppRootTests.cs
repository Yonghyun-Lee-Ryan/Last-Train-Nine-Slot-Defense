using System.Collections;
using System.Reflection;
using LastTrain.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastTrain.Tests.PlayMode
{
    public class AppRootTests
    {
        private GameObject _first;
        private GameObject _second;

        /// <summary>
        /// 테스트 중 실제 Scene 전환이 일어나지 않도록 autoLoadMainMenu를 끈 채로
        /// AppRoot 컴포넌트를 추가한다. private [SerializeField] 필드는 리플렉션으로 설정한다.
        /// </summary>
        private static AppRoot AddAppRootWithoutAutoLoad(GameObject go)
        {
            FieldInfo field = typeof(AppRoot).GetField(
                "autoLoadMainMenu", BindingFlags.NonPublic | BindingFlags.Instance);

            var appRoot = go.AddComponent<AppRoot>();
            field?.SetValue(appRoot, false);
            return appRoot;
        }

        [TearDown]
        public void TearDown()
        {
            if (_first != null)
            {
                Object.DestroyImmediate(_first);
            }

            if (_second != null)
            {
                Object.DestroyImmediate(_second);
            }
        }

        [UnityTest]
        public IEnumerator AppRoot_SingletonInstance_IsSetAfterAwake()
        {
            _first = new GameObject("AppRoot_First");
            AddAppRootWithoutAutoLoad(_first);

            yield return null;

            Assert.IsNotNull(AppRoot.Instance, "AppRoot.Instance가 설정되어야 합니다.");
            Assert.AreSame(_first.GetComponent<AppRoot>(), AppRoot.Instance);
            Assert.IsNotNull(AppRoot.Instance.SceneLoader, "SceneLoader가 초기화되어야 합니다.");
        }

        [UnityTest]
        public IEnumerator AppRoot_DuplicateInstance_IsDestroyed()
        {
            _first = new GameObject("AppRoot_First");
            AddAppRootWithoutAutoLoad(_first);
            yield return null;

            AppRoot firstInstance = AppRoot.Instance;

            _second = new GameObject("AppRoot_Second");
            AddAppRootWithoutAutoLoad(_second);
            yield return null;

            Assert.AreSame(firstInstance, AppRoot.Instance,
                "중복 AppRoot가 생성돼도 기존 인스턴스가 유지되어야 합니다.");

            // 중복 GameObject는 파괴 예약된다.
            Assert.IsTrue(_second == null || _second.GetComponent<AppRoot>() == null,
                "중복 AppRoot GameObject는 파괴되어야 합니다.");
        }
    }
}
