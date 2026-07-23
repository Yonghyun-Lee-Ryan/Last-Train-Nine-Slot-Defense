using System.IO;
using LastTrain.Difficulty;
using LastTrain.Save;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class SaveStabilityTests
    {
        private string _dir;
        private string _runPath;
        private string _metaPath;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Application.temporaryCachePath, "LastTrainSaveStability_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _runPath = Path.Combine(_dir, "run.json");
            _metaPath = Path.Combine(_dir, "meta.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
        }

        [Test]
        public void RunMigration_V1Json_LoadsAsCurrentVersion()
        {
            string v1 = JsonUtility.ToJson(new RunSaveData
            {
                version = 1,
                stationIndex = 3,
                trainHp = 40,
                trainMaxHp = 100,
                difficultyId = DifficultyIds.Normal,
            });

            File.WriteAllText(_runPath, v1);
            var service = new JsonSaveService(_runPath, _metaPath);
            Assert.IsTrue(service.TryLoadRun(out RunSaveData loaded));
            Assert.AreEqual(RunSaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(3, loaded.stationIndex);
            Assert.AreEqual(40, loaded.trainHp);
        }

        [Test]
        public void MetaMigration_V1Json_LoadsAsCurrentVersion()
        {
            string v1 = "{\"version\":1,\"ticketFragments\":7,\"accountLevel\":2}";
            File.WriteAllText(_metaPath, v1);
            var service = new JsonSaveService(_runPath, _metaPath);
            Assert.IsTrue(service.TryLoadMeta(out MetaSaveData loaded));
            Assert.AreEqual(MetaSaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(7, loaded.ticketFragments);
            Assert.AreEqual(2, loaded.accountLevel);
        }

        [Test]
        public void CorruptPrimary_RestoresFromBackup()
        {
            var service = new JsonSaveService(_runPath, _metaPath);
            var good = new RunSaveData
            {
                version = RunSaveData.CurrentVersion,
                stationIndex = 5,
                trainHp = 70,
                trainMaxHp = 100,
                difficultyId = DifficultyIds.Express,
            };
            Assert.IsTrue(service.SaveRun(good));

            // 백업 생성 후 원본 손상
            Assert.IsTrue(File.Exists(_runPath + ".bak") || File.Exists(_runPath));
            service.SaveRun(good); // 두 번째 저장으로 .bak 확보
            File.WriteAllText(_runPath, "{ not-json");

            Assert.IsTrue(service.TryLoadRun(out RunSaveData restored));
            Assert.AreEqual(5, restored.stationIndex);
            Assert.AreEqual(70, restored.trainHp);
        }

        [Test]
        public void SaveFailure_DoesNotThrow()
        {
            // 잘못된 경로여도 false만 반환
            var service = new JsonSaveService(
                Path.Combine(_dir, "no_such", "nested", "run.json"),
                Path.Combine(_dir, "no_such", "nested", "meta.json"));

            Assert.DoesNotThrow(() =>
            {
                bool ok = service.SaveRun(new RunSaveData { version = RunSaveData.CurrentVersion });
                // 디렉터리 생성에 성공하면 true일 수 있음 — 예외만 없으면 됨
                _ = ok;
            });
        }

        [Test]
        public void Pipeline_MigratesSequentially_FromVersion1()
        {
            var pipeline = SaveMigrationPipeline.ForRun();
            string v1 = JsonUtility.ToJson(new RunSaveData { version = 1, stationIndex = 2 });
            Assert.IsTrue(pipeline.TryMigrateToCurrent(v1, out string migrated, out int from, out int to));
            Assert.AreEqual(1, from);
            Assert.AreEqual(RunSaveData.CurrentVersion, to);
            Assert.AreEqual(RunSaveData.CurrentVersion, SaveMigrationPipeline.ReadVersion(migrated));
        }
    }
}
