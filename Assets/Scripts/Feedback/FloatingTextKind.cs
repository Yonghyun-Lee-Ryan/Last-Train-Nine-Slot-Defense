namespace LastTrain.Feedback
{
    /// <summary>플로팅 텍스트 종류. 설정 토글을 서로 독립적으로 적용한다.</summary>
    public enum FloatingTextKind
    {
        /// <summary>적/객차 피해 숫자.</summary>
        Damage = 0,
        /// <summary>코인 획득 숫자.</summary>
        Coin = 1,
        /// <summary>합성 별 등 기타 연출(피해/코인 토글과 무관).</summary>
        Status = 2,
    }
}
