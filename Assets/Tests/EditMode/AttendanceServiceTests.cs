using System.IO;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class AttendanceServiceTests
    {
        private string _tempDir;
        private string _runPath;
        private string _metaPath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "LastTrainAttendanceTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _runPath = Path.Combine(_tempDir, "RunSaveData.json");
            _metaPath = Path.Combine(_tempDir, "MetaSaveData.json");
            RunSaveSystem.SetServiceForTests(new JsonSaveService(_runPath, _metaPath));
        }

        [TearDown]
        public void TearDown()
        {
            Attendance.AttendanceClock.LocalNowProvider = null;
            RunSaveSystem.SetServiceForTests(null);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Test]
        public void TryClaimBase_SameLocalDay_CannotClaimTwice()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            Attendance.AttendanceClock.LocalNowProvider = () => new System.DateTime(2026, 8, 13, 10, 0, 0);

            Assert.IsTrue(Attendance.AttendanceService.TryClaimBase(meta, out Attendance.AttendanceGrant first));
            int ticketsAfterFirst = meta.ticketFragments;

            Assert.IsFalse(Attendance.AttendanceService.TryClaimBase(meta, out _));
            Assert.AreEqual(ticketsAfterFirst, meta.ticketFragments);
            Assert.AreEqual("2026-08-13", meta.attendanceLastClaimLocalDate);
        }

        [Test]
        public void TryClaimBase_NextLocalDay_AdvancesCycle()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            Attendance.AttendanceClock.LocalNowProvider = () => new System.DateTime(2026, 8, 13, 9, 0, 0);
            Assert.IsTrue(Attendance.AttendanceService.TryClaimBase(meta, out Attendance.AttendanceGrant day1));
            Assert.AreEqual(0, day1.CycleDayIndex);
            Assert.AreEqual(1, meta.attendanceCycleDay);

            Attendance.AttendanceClock.LocalNowProvider = () => new System.DateTime(2026, 8, 14, 9, 0, 0);
            Assert.IsTrue(Attendance.AttendanceService.CanClaimToday(meta));
            Assert.IsTrue(Attendance.AttendanceService.TryClaimBase(meta, out Attendance.AttendanceGrant day2));
            Assert.AreEqual(1, day2.CycleDayIndex);
            Assert.AreEqual(2, meta.attendanceCycleDay);
        }

        [Test]
        public void EnsureDayState_MissedDay_ResetsCycle()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.attendanceCycleDay = 4;
            meta.attendanceLastClaimLocalDate = "2026-08-10";

            Attendance.AttendanceClock.LocalNowProvider = () => new System.DateTime(2026, 8, 13, 12, 0, 0);
            Attendance.AttendanceService.EnsureDayState(meta);

            Assert.AreEqual(0, meta.attendanceCycleDay);
            Assert.IsTrue(Attendance.AttendanceService.CanClaimToday(meta));
        }

        [Test]
        public void TryGrantAdBonus_RequiresBaseClaimToday()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            Attendance.AttendanceClock.LocalNowProvider = () => new System.DateTime(2026, 8, 13, 12, 0, 0);

            Assert.IsFalse(Attendance.AttendanceService.TryGrantAdBonus(meta, out _));

            Assert.IsTrue(Attendance.AttendanceService.TryClaimBase(meta, out _));
            Assert.IsTrue(Attendance.AttendanceService.TryGrantAdBonus(meta, out Attendance.AttendanceGrant bonus));
            Assert.Greater(bonus.TicketFragments, 0);
            Assert.IsFalse(Attendance.AttendanceService.TryGrantAdBonus(meta, out _));
        }

        [Test]
        public void TryClaimBase_Day7_GrantsFreeSummonCharge()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.attendanceCycleDay = 6;
            meta.attendanceLastClaimLocalDate = "2026-08-12";
            Attendance.AttendanceClock.LocalNowProvider = () => new System.DateTime(2026, 8, 13, 8, 0, 0);

            Assert.IsTrue(Attendance.AttendanceService.TryClaimBase(meta, out Attendance.AttendanceGrant grant));
            Assert.AreEqual(6, grant.CycleDayIndex);
            Assert.AreEqual(1, grant.FreeSummonCharges);
            Assert.AreEqual(1, meta.metaPendingFreeSummonCharges);
            Assert.AreEqual(0, meta.attendanceCycleDay);
        }

        [Test]
        public void ApplyPendingRunBonuses_ConsumesFreeSummonCharges()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.metaPendingFreeSummonCharges = 2;
            MetaSaveSystem.Save(meta);

            var runState = new RunState();
            runState.Initialize(RunStartConfig.CreateDefault());

            MetaSaveSystem.ApplyPendingRunBonuses(runState);

            meta = MetaSaveSystem.LoadOrCreate();
            Assert.AreEqual(0, meta.metaPendingFreeSummonCharges);
            Assert.AreEqual(2, runState.ShopTokens.FreeSummonCharges);
        }
    }
}
