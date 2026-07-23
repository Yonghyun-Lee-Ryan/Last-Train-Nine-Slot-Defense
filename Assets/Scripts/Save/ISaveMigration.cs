namespace LastTrain.Save
{
    /// <summary>저장 데이터 버전 단위 마이그레이션.</summary>
    public interface ISaveMigration
    {
        /// <summary>이 마이그레이션이 적용되는 출발 버전.</summary>
        int FromVersion { get; }

        /// <summary>적용 후 버전.</summary>
        int ToVersion { get; }

        /// <summary>JSON 문자열을 변환한다. 실패 시 null.</summary>
        string Migrate(string json);
    }
}
