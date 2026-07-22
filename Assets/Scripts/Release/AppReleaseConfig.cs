using UnityEngine;

namespace LastTrain.Release
{
    /// <summary>앱 버전·스토어 메타·법적 링크를 중앙에서 관리한다.</summary>
    [CreateAssetMenu(fileName = "AppReleaseConfig", menuName = "Last Train/Release/App Release Config")]
    public sealed class AppReleaseConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "막차 생존";
        [SerializeField] private string androidPackageName = "com.lasttrain.nineslotdefense";

        [Header("Versioning")]
        [SerializeField] private string versionName = "0.1.0";
        [SerializeField] private int androidBundleVersionCode = 1;

        [Header("Legal")]
        [TextArea(2, 6)]
        [SerializeField] private string privacyPolicyUrl = "https://example.com/lasttrain/privacy";
        [TextArea(2, 6)]
        [SerializeField] private string dataDeletionNotice =
            "앱 데이터 삭제 시 진행도, 메타 보상, 설정이 기기에서 제거됩니다.";

        public string DisplayName => displayName;
        public string AndroidPackageName => androidPackageName;
        public string VersionName => versionName;
        public int AndroidBundleVersionCode => Mathf.Max(1, androidBundleVersionCode);
        public string PrivacyPolicyUrl => privacyPolicyUrl;
        public string DataDeletionNotice => dataDeletionNotice;
    }
}
