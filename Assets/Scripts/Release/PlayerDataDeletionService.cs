using LastTrain.Integrations;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.Release
{
    /// <summary>로컬 저장 데이터와 동의·설정 PlayerPrefs를 삭제한다.</summary>
    public static class PlayerDataDeletionService
    {
        public static bool DeleteAllLocalData(PrivacyConsentService privacy, GameSettingsService settings)
        {
            bool runDeleted = RunSaveSystem.DeleteRunSave();
            bool metaDeleted = RunSaveSystem.DeleteMetaSave();

            privacy?.RevokeAll();
            settings?.ResetToDefaults();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            return runDeleted || metaDeleted;
        }
    }
}
