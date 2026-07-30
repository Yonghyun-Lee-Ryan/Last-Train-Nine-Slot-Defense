using System;
using System.IO;
using LastTrain.Release;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// 서명 비밀번호 입력 → Bundle Version Code +1 → Release AAB 생성.
    /// 비밀번호는 세션에만 적용되며 프로젝트에 저장하지 않는다.
    /// </summary>
    public sealed class AndroidReleaseAabWizard : EditorWindow
    {
        private const string PrefKeystorePath = "LastTrain.Release.KeystorePath";
        private const string PrefKeyAlias = "LastTrain.Release.KeyAlias";

        private string _keystorePath = string.Empty;
        private string _keystorePass = string.Empty;
        private string _keyAlias = string.Empty;
        private string _keyAliasPass = string.Empty;
        private bool _samePasswordForKey = true;
        private bool _bumpVersionCode = true;
        private bool _isBuilding;
        private string _status = string.Empty;
        private Vector2 _scroll;

        [MenuItem("Tools/막차 생존/Release/서명·버전업 후 Release AAB 빌드", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<AndroidReleaseAabWizard>(true, "Release AAB 빌드", true);
            window.minSize = new Vector2(520f, 460f);
            window.LoadDefaults();
            window.Show();
        }

        private void LoadDefaults()
        {
            _keystorePath = EditorPrefs.GetString(PrefKeystorePath, string.Empty);
            if (string.IsNullOrWhiteSpace(_keystorePath))
            {
                _keystorePath = NormalizeKeystorePath(PlayerSettings.Android.keystoreName);
            }

            _keyAlias = EditorPrefs.GetString(PrefKeyAlias, string.Empty);
            if (string.IsNullOrWhiteSpace(_keyAlias))
            {
                _keyAlias = string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName)
                    ? "lasttrain-release"
                    : PlayerSettings.Android.keyaliasName;
            }

            _samePasswordForKey = true;
            _bumpVersionCode = true;
            _status = string.Empty;
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(_isBuilding))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                EditorGUILayout.LabelField("Google Play Release AAB", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "1) 키스토어·비밀번호 입력\n" +
                    "2) Bundle Version Code +1 (Play 업로드마다 필수)\n" +
                    "3) Release AAB → Builds/Android/\n\n" +
                    "비밀번호는 프로젝트에 저장되지 않습니다.\n" +
                    "Gradle 실패 시 대부분 Keystore/Key Alias 비밀번호 오류입니다.",
                    MessageType.Info);

                AppReleaseConfig config = ReleaseConfigSync.LoadOrCreateConfig();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("버전", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Version Name", config != null ? config.VersionName : "-");
                    int currentCode = config != null
                        ? config.AndroidBundleVersionCode
                        : PlayerSettings.Android.bundleVersionCode;
                    EditorGUILayout.LabelField("현재 Bundle Version Code", currentCode.ToString());
                    _bumpVersionCode = EditorGUILayout.ToggleLeft("빌드 성공 시 Version Code +1", _bumpVersionCode);
                    if (_bumpVersionCode)
                    {
                        EditorGUILayout.LabelField(
                            "성공 후 Code",
                            (currentCode + 1).ToString(),
                            EditorStyles.boldLabel);
                    }
                }

                EditorGUILayout.Space(8f);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("서명 (Keystore)", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    _keystorePath = EditorGUILayout.TextField("Keystore 경로", _keystorePath);
                    if (GUILayout.Button("찾기…", GUILayout.Width(64f)))
                    {
                        string startDir = string.IsNullOrEmpty(_keystorePath)
                            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                            : Path.GetDirectoryName(NormalizeKeystorePath(_keystorePath));
                        string picked = EditorUtility.OpenFilePanel("Android Keystore", startDir ?? "", "keystore");
                        if (!string.IsNullOrEmpty(picked))
                        {
                            _keystorePath = picked.Replace('\\', '/');
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    _keystorePass = EditorGUILayout.PasswordField("Keystore 비밀번호", _keystorePass);
                    _keyAlias = EditorGUILayout.TextField("Key Alias", _keyAlias);
                    _samePasswordForKey = EditorGUILayout.ToggleLeft(
                        "Key Alias 비밀번호 = Keystore 비밀번호",
                        _samePasswordForKey);
                    if (!_samePasswordForKey)
                    {
                        _keyAliasPass = EditorGUILayout.PasswordField("Key Alias 비밀번호", _keyAliasPass);
                    }
                }

                EditorGUILayout.Space(12f);
                bool canBuild = CanBuild(out string reason);
                using (new EditorGUI.DisabledScope(!canBuild))
                {
                    if (GUILayout.Button(
                            _isBuilding ? "빌드 중…" : "Release AAB 빌드",
                            GUILayout.Height(40f)))
                    {
                        ScheduleBuild();
                    }
                }

                if (!canBuild && !string.IsNullOrEmpty(reason))
                {
                    EditorGUILayout.HelpBox(reason, MessageType.Warning);
                }

                if (!string.IsNullOrEmpty(_status))
                {
                    EditorGUILayout.Space(8f);
                    MessageType type = _status.StartsWith("완료", StringComparison.Ordinal)
                        ? MessageType.Info
                        : _status.StartsWith("실패", StringComparison.Ordinal)
                            ? MessageType.Error
                            : MessageType.None;
                    EditorGUILayout.HelpBox(_status, type);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private bool CanBuild(out string reason)
        {
            reason = string.Empty;
            string path = NormalizeKeystorePath(_keystorePath);
            if (string.IsNullOrWhiteSpace(path))
            {
                reason = "Keystore 경로가 필요합니다.";
                return false;
            }

            if (!File.Exists(path))
            {
                reason = $"Keystore 파일을 찾을 수 없습니다:\n{path}";
                return false;
            }

            if (string.IsNullOrEmpty(_keystorePass))
            {
                reason = "Keystore 비밀번호를 입력하세요.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_keyAlias))
            {
                reason = "Key Alias가 필요합니다.";
                return false;
            }

            if (!_samePasswordForKey && string.IsNullOrEmpty(_keyAliasPass))
            {
                reason = "Key Alias 비밀번호를 입력하세요.";
                return false;
            }

            return true;
        }

        private void ScheduleBuild()
        {
            if (_isBuilding)
            {
                return;
            }

            if (!CanBuild(out string reason))
            {
                // OnGUI 중 DisplayDialog는 EndLayoutGroup 오류를 유발한다.
                string message = reason;
                EditorApplication.delayCall += () =>
                    EditorUtility.DisplayDialog("Release AAB", message, "확인");
                return;
            }

            // OnGUI 밖에서 확인 다이얼로그 → 빌드 실행
            string keystorePath = NormalizeKeystorePath(_keystorePath);
            string keystorePass = _keystorePass;
            string keyAlias = _keyAlias;
            string keyAliasPass = _samePasswordForKey ? _keystorePass : _keyAliasPass;
            bool bump = _bumpVersionCode;
            string confirmMessage = BuildConfirmMessage();

            _isBuilding = true;
            _status = "확인 대기…";
            Repaint();

            EditorApplication.delayCall += () =>
            {
                if (!EditorUtility.DisplayDialog("Release AAB 빌드", confirmMessage, "빌드", "취소"))
                {
                    _isBuilding = false;
                    _status = string.Empty;
                    Repaint();
                    return;
                }

                _status = "빌드 예약… (콘솔 로그를 확인하세요)";
                Repaint();
                RunBuild(keystorePath, keystorePass, keyAlias, keyAliasPass, bump);
            };
        }

        private void RunBuild(
            string keystorePath,
            string keystorePass,
            string keyAlias,
            string keyAliasPass,
            bool bumpVersionCode)
        {
            try
            {
                EditorPrefs.SetString(PrefKeystorePath, keystorePath ?? string.Empty);
                EditorPrefs.SetString(PrefKeyAlias, keyAlias ?? string.Empty);

                _status = "Release 에셋·Player Settings 준비…";
                Repaint();

                ReleaseAssetsBuilder.EnsureReleaseAssets();
                AppReleaseConfig config = ReleaseConfigSync.LoadOrCreateConfig();
                ReleaseConfigSync.ApplyToPlayerSettings(config);
                ApplySigning(keystorePath, keystorePass, keyAlias, keyAliasPass);

                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildAppBundle = true;
                AssetDatabase.SaveAssets();

                ReleaseBuildValidator.Validate(strictRelease: true, throwOnError: true);

                // 실패해도 코드가 미리 올라가지 않도록: 빌드 직전 PlayerSettings 코드만 임시 올렸다가
                // 성공 시 AppReleaseConfig에 반영한다.
                int currentCode = config.AndroidBundleVersionCode;
                int buildCode = bumpVersionCode ? currentCode + 1 : currentCode;
                PlayerSettings.Android.bundleVersionCode = buildCode;

                _status = $"AAB 빌드 중… (code {buildCode})";
                Repaint();

                string outputPath = AndroidReleaseBuildMenu.BuildReleaseAppBundleInternal(buildCode);

                if (bumpVersionCode)
                {
                    // BuildPlayer 이후 ScriptableObject 참조가 무효화될 수 있으므로 재로드한다.
                    PersistBundleVersionCode(buildCode);
                }

                _status = $"완료: {outputPath}\nVersion Code: {buildCode}";
                _keystorePass = string.Empty;
                _keyAliasPass = string.Empty;
                EditorUtility.DisplayDialog(
                    "Build Complete",
                    $"AAB 생성 완료\n\n{outputPath}\n\nVersion Code: {buildCode}\n\n" +
                    "Play Console → 내부 테스트에 업로드하세요.",
                    "확인");
            }
            catch (Exception ex)
            {
                string friendly = FormatFriendlyError(ex);
                _status = "실패: " + friendly;
                Debug.LogError("[AndroidReleaseAabWizard] " + friendly + "\n" + ex);
                EditorUtility.DisplayDialog("Build Failed", friendly, "확인");
            }
            finally
            {
                _isBuilding = false;
                Repaint();
            }
        }

        private static string FormatFriendlyError(Exception ex)
        {
            string text = ex.ToString();
            if (text.IndexOf("keystore password was incorrect", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("password was incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return
                    "키스토어 비밀번호가 올바르지 않습니다.\n\n" +
                    "· Keystore 비밀번호와 Key Alias 비밀번호가 다를 수 있습니다.\n" +
                    "· 'Key Alias 비밀번호 = Keystore 비밀번호' 체크를 해제한 뒤 각각 입력해 보세요.\n" +
                    "· 경로: C:\\Users\\…\\AndroidKeys\\lasttrain-release.keystore";
            }

            if (text.IndexOf("key permanently invalidated", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Cannot recover key", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Key Alias 비밀번호가 올바르지 않거나 Alias 이름이 다릅니다.";
            }

            return string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
        }

        private string BuildConfirmMessage()
        {
            AppReleaseConfig config = ReleaseConfigSync.LoadOrCreateConfig();
            int next = _bumpVersionCode
                ? config.AndroidBundleVersionCode + 1
                : config.AndroidBundleVersionCode;

            return
                $"패키지: {config.AndroidPackageName}\n" +
                $"Version Name: {config.VersionName}\n" +
                $"Bundle Version Code: {next}\n" +
                $"Keystore: {NormalizeKeystorePath(_keystorePath)}\n" +
                $"Alias: {_keyAlias}\n\n" +
                "Release AAB를 빌드할까요?";
        }

        private static void ApplySigning(string keystorePath, string keystorePass, string alias, string aliasPass)
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPass;
        }

        private static void PersistBundleVersionCode(int next)
        {
            AppReleaseConfig config = ReleaseConfigSync.LoadOrCreateConfig();
            if (config == null)
            {
                throw new InvalidOperationException("AppReleaseConfig를 다시 로드하지 못했습니다.");
            }

            WriteBundleVersionCode(config, next);

            AppReleaseConfig resourcesCopy = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(
                "Assets/Resources/AppReleaseConfig.asset");
            if (resourcesCopy != null && resourcesCopy != config)
            {
                WriteBundleVersionCode(resourcesCopy, next);
            }

            PlayerSettings.Android.bundleVersionCode = next;
            AssetDatabase.SaveAssets();
        }

        private static void WriteBundleVersionCode(AppReleaseConfig config, int next)
        {
            if (config == null)
            {
                return;
            }

            var so = new SerializedObject(config);
            SerializedProperty prop = so.FindProperty("androidBundleVersionCode");
            if (prop == null)
            {
                throw new InvalidOperationException("androidBundleVersionCode 필드를 찾지 못했습니다.");
            }

            prop.intValue = next;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static string NormalizeKeystorePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalized = path.Trim().Replace('\\', '/');

            // Unity dedicated path: "{dedicated}: AndroidKeys/foo.keystore"
            const string dedicatedPrefix = "{dedicated}:";
            if (normalized.StartsWith(dedicatedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string relative = normalized.Substring(dedicatedPrefix.Length).TrimStart(' ', '/');
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    .Replace('\\', '/');
                normalized = $"{userProfile}/{relative}";
            }

            return normalized;
        }
    }
}
