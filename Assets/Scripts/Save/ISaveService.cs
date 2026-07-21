namespace LastTrain.Save
{
    /// <summary>JSON 저장·복원을 담당한다.</summary>
    public interface ISaveService
    {
        bool TryLoadRun(out RunSaveData runSave);
        bool SaveRun(RunSaveData runSave);
        bool DeleteRunSave();

        bool TryLoadMeta(out MetaSaveData metaSave);
        bool SaveMeta(MetaSaveData metaSave);
        bool DeleteMetaSave();
    }
}

