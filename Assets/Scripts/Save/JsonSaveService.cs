using System;
using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>Application.persistentDataPath에 JSON으로 저장한다. 원자적 교체·백업 복원·버전 마이그레이션을 지원한다.</summary>
    public sealed class JsonSaveService : ISaveService
    {
        private readonly string _runSavePath;
        private readonly string _metaSavePath;
        private readonly string _runBackupPath;
        private readonly string _metaBackupPath;
        private readonly SaveMigrationPipeline _runMigrations;
        private readonly SaveMigrationPipeline _metaMigrations;

        public JsonSaveService(string runSavePath, string metaSavePath)
        {
            _runSavePath = runSavePath ?? throw new ArgumentNullException(nameof(runSavePath));
            _metaSavePath = metaSavePath ?? throw new ArgumentNullException(nameof(metaSavePath));
            _runBackupPath = _runSavePath + ".bak";
            _metaBackupPath = _metaSavePath + ".bak";
            _runMigrations = SaveMigrationPipeline.ForRun();
            _metaMigrations = SaveMigrationPipeline.ForMeta();
        }

        public bool TryLoadRun(out RunSaveData runSave)
        {
            return TryLoad(
                _runSavePath,
                _runBackupPath,
                _runMigrations,
                "Run",
                ParseRun,
                out runSave);
        }

        public bool SaveRun(RunSaveData runSave)
        {
            if (runSave == null)
            {
                return false;
            }

            runSave.version = RunSaveData.CurrentVersion;
            string json = JsonUtility.ToJson(runSave, prettyPrint: false);
            return AtomicSaveIO.TryWriteAtomic(_runSavePath, _runBackupPath, json, "Run");
        }

        public bool DeleteRunSave()
        {
            AtomicSaveIO.TryDeleteQuiet(_runSavePath);
            AtomicSaveIO.TryDeleteQuiet(_runBackupPath);
            AtomicSaveIO.TryDeleteQuiet(_runSavePath + ".tmp");
            return true;
        }

        public bool TryLoadMeta(out MetaSaveData metaSave)
        {
            bool loaded = TryLoad(
                _metaSavePath,
                _metaBackupPath,
                _metaMigrations,
                "Meta",
                ParseMeta,
                out metaSave);
            if (loaded && metaSave != null)
            {
                metaSave.EnsureDefaults();
                metaSave.version = MetaSaveData.CurrentVersion;
            }

            return loaded;
        }

        public bool SaveMeta(MetaSaveData metaSave)
        {
            if (metaSave == null)
            {
                return false;
            }

            metaSave.EnsureDefaults();
            metaSave.version = MetaSaveData.CurrentVersion;
            string json = JsonUtility.ToJson(metaSave, prettyPrint: false);
            return AtomicSaveIO.TryWriteAtomic(_metaSavePath, _metaBackupPath, json, "Meta");
        }

        public bool DeleteMetaSave()
        {
            AtomicSaveIO.TryDeleteQuiet(_metaSavePath);
            AtomicSaveIO.TryDeleteQuiet(_metaBackupPath);
            AtomicSaveIO.TryDeleteQuiet(_metaSavePath + ".tmp");
            return true;
        }

        private static bool TryLoad<T>(
            string path,
            string backupPath,
            SaveMigrationPipeline pipeline,
            string label,
            Func<string, T> parse,
            out T data)
            where T : class
        {
            data = null;
            if (TryLoadFromPath(path, backupPath, pipeline, label, parse, out data))
            {
                return data != null;
            }

            if (TryLoadFromPath(backupPath, null, pipeline, label + ".bak", parse, out data))
            {
                Debug.LogWarning($"[Save:{label}] 원본 손상/실패 → 백업에서 복원했습니다.");
                // 성공한 백업을 원본으로 다시 기록 시도(실패해도 로드는 성공)
                if (data != null)
                {
                    string json = JsonUtility.ToJson(data, prettyPrint: false);
                    AtomicSaveIO.TryWriteAtomic(path, backupPath, json, label + ".restore");
                }

                return data != null;
            }

            return false;
        }

        private static bool TryLoadFromPath<T>(
            string path,
            string unusedBackup,
            SaveMigrationPipeline pipeline,
            string label,
            Func<string, T> parse,
            out T data)
            where T : class
        {
            data = null;
            _ = unusedBackup;
            if (!AtomicSaveIO.TryReadText(path, out string json))
            {
                return false;
            }

            try
            {
                if (!pipeline.TryMigrateToCurrent(json, out string migrated, out int from, out int to))
                {
                    AtomicSaveIO.LogFail(label, $"마이그레이션 실패 (v{from}→current)");
                    return false;
                }

                if (from != to)
                {
                    Debug.Log($"[Save:{label}] v{from} → v{to} 마이그레이션 완료");
                }

                data = parse(migrated);
                return data != null;
            }
            catch (Exception ex)
            {
                AtomicSaveIO.LogFail(label, ex.Message);
                data = null;
                return false;
            }
        }

        private static RunSaveData ParseRun(string json)
        {
            RunSaveData data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null || data.version != RunSaveData.CurrentVersion)
            {
                return null;
            }

            return data;
        }

        private static MetaSaveData ParseMeta(string json)
        {
            MetaSaveData data = JsonUtility.FromJson<MetaSaveData>(json);
            if (data == null || data.version != MetaSaveData.CurrentVersion)
            {
                return null;
            }

            return data;
        }
    }
}
