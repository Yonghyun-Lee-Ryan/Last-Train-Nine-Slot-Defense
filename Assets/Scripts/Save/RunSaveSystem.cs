using System;
using LastTrain.Core;
using LastTrain.Run;

namespace LastTrain.Save
{
    /// <summary>
    /// RunSaveData 저장/로드/삭제를 담당한다.
    /// </summary>
    public static class RunSaveSystem
    {
        private const string RunSaveFileName = "RunSaveData.json";
        private const string MetaSaveFileName = "MetaSaveData.json";

        private static ISaveService _service;

        private static ISaveService Service
        {
            get
            {
                if (_service != null)
                {
                    return _service;
                }

                string baseDir = AppPathUtil.PersistentDataPath;
                string runPath = System.IO.Path.Combine(baseDir, RunSaveFileName);
                string metaPath = System.IO.Path.Combine(baseDir, MetaSaveFileName);

                _service = new JsonSaveService(runPath, metaPath);
                return _service;
            }
        }

        public static bool TrySavePreparing(GameSession session)
        {
            if (session == null || !session.HasActiveRun || session.RunState == null)
            {
                return false;
            }

            RunState runState = session.RunState;
            if (runState.IsDailyRun)
            {
                return false;
            }

            if (runState.Battle == null
                || !runState.Battle.IsRunActive
                || runState.Battle.CurrentPhase != RunPhase.Preparing)
            {
                return false;
            }

            RunSaveData data = RunSaveMapper.CreateFromRunState(runState);
            return Service.SaveRun(data);
        }

        public static bool TryLoadPreparing(out RunSaveData runSave)
        {
            runSave = null;
            if (!Service.TryLoadRun(out RunSaveData loaded) || loaded == null)
            {
                return false;
            }

            if ((RunPhase)loaded.savedBattlePhase != RunPhase.Preparing)
            {
                return false;
            }

            runSave = loaded;
            return true;
        }

        public static bool DeleteRunSave()
        {
            return Service.DeleteRunSave();
        }

        public static bool TryLoadMeta(out MetaSaveData metaSave)
        {
            return Service.TryLoadMeta(out metaSave);
        }

        public static bool SaveMeta(MetaSaveData metaSave)
        {
            return Service.SaveMeta(metaSave);
        }

        public static bool DeleteMetaSave()
        {
            return Service.DeleteMetaSave();
        }

        /// <summary>EditMode 테스트용. 기본 Service 대신 주입한다.</summary>
        public static void SetServiceForTests(ISaveService service)
        {
            _service = service;
        }
    }

    internal static class AppPathUtil
    {
        public static string PersistentDataPath =>
            UnityEngine.Application.persistentDataPath;
    }
}

