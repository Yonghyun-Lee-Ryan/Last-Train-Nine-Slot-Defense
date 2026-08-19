using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>버전별 마이그레이션을 From→To 순으로 적용한다.</summary>
    public sealed class SaveMigrationPipeline
    {
        private readonly IReadOnlyList<ISaveMigration> _migrations;
        private readonly int _targetVersion;
        private readonly string _label;

        public SaveMigrationPipeline(string label, int targetVersion, IReadOnlyList<ISaveMigration> migrations)
        {
            _label = label ?? "Save";
            _targetVersion = targetVersion;
            _migrations = migrations ?? Array.Empty<ISaveMigration>();
        }

        public static SaveMigrationPipeline ForRun()
        {
            return new SaveMigrationPipeline(
                "RunSave",
                RunSaveData.CurrentVersion,
                new ISaveMigration[]
                {
                    new RunSaveMigrationV1ToV2(),
                    new RunSaveMigrationV2ToV3(),
                });
        }

        public static SaveMigrationPipeline ForMeta()
        {
            return new SaveMigrationPipeline(
                "MetaSave",
                MetaSaveData.CurrentVersion,
                new ISaveMigration[]
                {
                    new MetaSaveMigrationV1ToV2(),
                    new MetaSaveMigrationV2ToV3(),
                    new MetaSaveMigrationV3ToV4(),
                });
        }

        /// <summary>
        /// JSON을 최신 버전으로 변환한다.
        /// version 필드가 없거나 0이면 1로 간주한다.
        /// </summary>
        public bool TryMigrateToCurrent(string json, out string migratedJson, out int fromVersion, out int toVersion)
        {
            migratedJson = json;
            fromVersion = ReadVersion(json);
            toVersion = fromVersion;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            if (fromVersion < 0)
            {
                Debug.LogWarning($"[{_label}] 손상된 JSON으로 보입니다.");
                return false;
            }

            if (fromVersion <= 0)
            {
                fromVersion = 1;
            }

            if (fromVersion == _targetVersion)
            {
                toVersion = _targetVersion;
                migratedJson = json;
                return true;
            }

            if (fromVersion > _targetVersion)
            {
                Debug.LogWarning($"[{_label}] 미래 버전 저장 파일(v{fromVersion} > v{_targetVersion}). 로드를 거부합니다.");
                return false;
            }

            string current = json;
            int version = fromVersion;
            while (version < _targetVersion)
            {
                ISaveMigration step = FindMigration(version);
                if (step == null)
                {
                    Debug.LogWarning($"[{_label}] v{version} → 다음 버전 마이그레이션이 없습니다.");
                    return false;
                }

                string next = step.Migrate(current);
                if (string.IsNullOrWhiteSpace(next))
                {
                    Debug.LogWarning($"[{_label}] v{step.FromVersion}→v{step.ToVersion} 마이그레이션 실패");
                    return false;
                }

                current = next;
                version = step.ToVersion;
            }

            migratedJson = current;
            toVersion = version;
            return toVersion == _targetVersion;
        }

        private ISaveMigration FindMigration(int fromVersion)
        {
            for (int i = 0; i < _migrations.Count; i++)
            {
                ISaveMigration migration = _migrations[i];
                if (migration != null && migration.FromVersion == fromVersion)
                {
                    return migration;
                }
            }

            return null;
        }

        public static int ReadVersion(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return 0;
            }

            // JsonUtility는 잘못된 JSON에도 예외 없이 기본값을 줄 수 있어 간단 검증을 둔다.
            if (json.IndexOf("\"version\"", System.StringComparison.Ordinal) < 0
                && json.IndexOf("version", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                return -1;
            }

            try
            {
                var envelope = JsonUtility.FromJson<VersionEnvelope>(json);
                return envelope != null ? envelope.version : 0;
            }
            catch
            {
                return -1;
            }
        }

        [Serializable]
        private sealed class VersionEnvelope
        {
            public int version;
        }
    }
}
