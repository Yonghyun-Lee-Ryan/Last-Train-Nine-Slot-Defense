namespace LastTrain.Data
{
    /// <summary>
    /// 고유 ID를 가진 정적 데이터 에셋 공통 계약.
    /// GameDatabase 조회와 중복 검증에 사용한다.
    /// </summary>
    public interface IDataWithId
    {
        string Id { get; }
    }
}
