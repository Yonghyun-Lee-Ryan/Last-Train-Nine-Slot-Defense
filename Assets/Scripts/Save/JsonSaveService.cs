using System;
using System.IO;
using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>Application.persistentDataPath에 JSON으로 저장한다.</summary>
    public sealed class JsonSaveService : ISaveService
    {
        private readonly string _runSavePath;
        private readonly string _metaSavePath;

        private readonly string _runBackupPath;

        public JsonSaveService(string runSavePath, string metaSavePath)
        {
            _runSavePath = runSavePath ?? throw new ArgumentNullException(nameof(runSavePath));
            _metaSavePath = metaSavePath ?? throw new ArgumentNullException(nameof(metaSavePath));
            _runBackupPath = _runSavePath + ".bak";
        }

        public bool TryLoadRun(out RunSaveData runSave)
        {
            runSave = null;
            if (string.IsNullOrWhiteSpace(_runSavePath) || !File.Exists(_runSavePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(_runSavePath);
                runSave = JsonUtility.FromJson<RunSaveData>(json);

                if (runSave == null)
                {
                    return false;
                }

                if (runSave.version != RunSaveData.CurrentVersion)
                {
                    // 버전이 다르면 호환/마이그레이션을 아직 구현하지 않음: 안전하게 무시
                    try
                    {
                        File.Delete(_runSavePath);
                    }
                    catch
                    {
                        // ignore
                    }
                    runSave = null;
                    return false;
                }

                return true;
            }
            catch
            {
                // 손상 파일은 안전하게 무시하고 삭제 시도
                runSave = null;
                try
                {
                    File.Delete(_runSavePath);
                }
                catch
                {
                    // ignore
                }
                return false;
            }
        }

        public bool SaveRun(RunSaveData runSave)
        {
            if (runSave == null)
            {
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(runSave, prettyPrint: false);

                string directory = Path.GetDirectoryName(_runSavePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = _runSavePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(_runSavePath))
                {
                    File.Replace(tempPath, _runSavePath, _runBackupPath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(tempPath, _runSavePath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteRunSave()
        {
            try
            {
                if (File.Exists(_runSavePath))
                {
                    File.Delete(_runSavePath);
                }

                if (File.Exists(_runBackupPath))
                {
                    File.Delete(_runBackupPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryLoadMeta(out MetaSaveData metaSave)
        {
            metaSave = null;
            if (string.IsNullOrWhiteSpace(_metaSavePath) || !File.Exists(_metaSavePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(_metaSavePath);
                metaSave = JsonUtility.FromJson<MetaSaveData>(json);

                if (metaSave == null)
                {
                    return false;
                }

                if (metaSave.version != MetaSaveData.CurrentVersion)
                {
                    try
                    {
                        File.Delete(_metaSavePath);
                    }
                    catch
                    {
                        // ignore
                    }
                    metaSave = null;
                    return false;
                }

                return true;
            }
            catch
            {
                metaSave = null;
                try
                {
                    File.Delete(_metaSavePath);
                }
                catch
                {
                    // ignore
                }
                return false;
            }
        }

        public bool SaveMeta(MetaSaveData metaSave)
        {
            if (metaSave == null)
            {
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(metaSave, prettyPrint: false);

                string directory = Path.GetDirectoryName(_metaSavePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = _metaSavePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(_metaSavePath))
                {
                    File.Delete(_metaSavePath);
                }

                File.Move(tempPath, _metaSavePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteMetaSave()
        {
            try
            {
                if (File.Exists(_metaSavePath))
                {
                    File.Delete(_metaSavePath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

