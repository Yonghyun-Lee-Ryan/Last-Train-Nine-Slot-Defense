using System;
using System.IO;
using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>임시 파일 작성 → 원본 교체 + 백업. 실패해도 예외를 밖으로 던지지 않는다.</summary>
    public static class AtomicSaveIO
    {
        public static bool TryWriteAtomic(string path, string backupPath, string contents, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || contents == null)
            {
                LogFail(label, "경로 또는 내용이 비어 있습니다.");
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, contents);

                if (File.Exists(path))
                {
                    if (!string.IsNullOrWhiteSpace(backupPath))
                    {
                        File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Delete(path);
                        File.Move(tempPath, path);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogFail(label, ex.Message);
                TryDeleteQuiet(path + ".tmp");
                return false;
            }
        }

        public static bool TryReadText(string path, out string text)
        {
            text = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                text = File.ReadAllText(path);
                return !string.IsNullOrWhiteSpace(text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AtomicSaveIO] 읽기 실패: {path} — {ex.Message}");
                text = null;
                return false;
            }
        }

        public static void TryDeleteQuiet(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }

        public static void LogFail(string label, string message)
        {
            Debug.LogWarning($"[Save:{label}] 저장/복원 실패(게임 진행은 계속): {message}");
        }
    }
}
