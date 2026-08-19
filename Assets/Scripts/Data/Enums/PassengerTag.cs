using System;

namespace LastTrain.Data
{
    /// <summary>
    /// 승객 태그. 시너지 판정에 사용한다.
    /// Flags로 여러 태그를 동시에 가질 수 있다.
    /// </summary>
    [Flags]
    public enum PassengerTag
    {
        None = 0,

        /// <summary>직장인 계열 (야근조)</summary>
        OfficeWorker = 1 << 0,

        /// <summary>배달·현장 대응</summary>
        Delivery = 1 << 1,

        /// <summary>체력·방어 (헬스 트레이너)</summary>
        Fitness = 1 << 2,

        /// <summary>의료·회복 (간호사)</summary>
        Medical = 1 << 3,

        /// <summary>기술·소환 (개발자)</summary>
        Tech = 1 << 4,

        /// <summary>학술·확률 딜 (대학원생)</summary>
        Academic = 1 << 5,

        /// <summary>치안·보스 대응 (경찰관)</summary>
        LawEnforcement = 1 << 6,

        /// <summary>행운·특수 (고양이)</summary>
        Lucky = 1 << 7,

        /// <summary>교통·운행 (승무원)</summary>
        Transit = 1 << 8,

        /// <summary>서비스·응대 (바리스타)</summary>
        Service = 1 << 9,

        /// <summary>경비·경계 (경비원)</summary>
        Security = 1 << 10,

        /// <summary>통학·수험 (수험생)</summary>
        Commute = 1 << 11
    }
}
