# 「막차 생존: 9칸 디펜스」 Cursor 개발 단위

## 0. 공통 기술 기준

### 개발 환경

* Unity 2D
* C#
* Android 우선
* Portrait 화면
* UGUI 또는 Unity UI Toolkit 중 UGUI 권장
* 싱글플레이 로컬 게임부터 개발
* ScriptableObject 기반 정적 데이터
* 런타임 상태와 원본 데이터 분리
* 핵심 로직은 MonoBehaviour 의존 최소화
* Unity Test Framework 사용
* 실제 광고 SDK는 코어 재미 검증 후 적용

### 권장 폴더 구조

```text
Assets/
├── Art/
│   ├── Characters/
│   ├── Enemies/
│   ├── UI/
│   ├── Backgrounds/
│   └── Effects/
├── Audio/
├── Data/
│   ├── Passengers/
│   ├── Enemies/
│   ├── Stations/
│   ├── Abilities/
│   ├── Synergies/
│   └── Relics/
├── Prefabs/
│   ├── Passengers/
│   ├── Enemies/
│   ├── Projectiles/
│   ├── UI/
│   └── Effects/
├── Scenes/
│   ├── Bootstrap
│   ├── MainMenu
│   ├── Game
│   └── Result
├── Scripts/
│   ├── Core/
│   ├── Data/
│   ├── Battle/
│   ├── Grid/
│   ├── Passenger/
│   ├── Enemy/
│   ├── Wave/
│   ├── Ability/
│   ├── Meta/
│   ├── Save/
│   ├── UI/
│   ├── Ads/
│   ├── Analytics/
│   └── Debug/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

### 핵심 클래스

```text
AppRoot
GameSession
RunState
BattleManager
StationManager
WaveManager
GridManager
GridSlot
PassengerController
PassengerRuntime
PassengerFactory
MergeService
EnemyController
EnemyFactory
TargetingService
DamageService
ProjectileController
AbilityManager
SynergyManager
RewardManager
SaveService
AdService
AnalyticsService
RandomService
```

---

# 개발 단위 1. 프로젝트 기반과 Scene 구성

## 목표

Unity 프로젝트의 공통 구조와 Scene 전환을 만든다.

## 구현 내용

* Android Portrait 설정
* Bootstrap Scene
* MainMenu Scene
* Game Scene
* Result Scene
* AppRoot
* SceneLoader
* Safe Area 대응
* 공통 Canvas 설정
* 기본 폴더 생성

## 완료 기준

* 앱 실행 시 Bootstrap을 거쳐 MainMenu로 이동한다.
* 게임 시작 버튼으로 Game Scene에 진입한다.
* 임시 종료 버튼으로 Result Scene에 이동한다.
* 16:9, 19.5:9 화면에서 UI가 잘리지 않는다.
* AppRoot가 중복 생성되지 않는다.

## Cursor 요청 프롬프트

```text
Unity 2D Android Portrait 게임의 프로젝트 기반 구조를 구현해줘.

게임명은 "막차 생존: 9칸 디펜스"다.

요구사항:
1. Bootstrap, MainMenu, Game, Result Scene을 사용한다.
2. Bootstrap Scene에서 AppRoot를 초기화하고 MainMenu로 이동한다.
3. AppRoot는 중복 생성되지 않아야 한다.
4. SceneLoader가 비동기 Scene 전환을 담당한다.
5. Canvas는 1080x1920 기준 Scale With Screen Size를 사용한다.
6. Android Safe Area를 처리하는 컴포넌트를 작성한다.
7. Portrait 방향만 허용한다.
8. 폴더 구조를 역할별로 생성한다.
9. 임시 MainMenu와 Game, Result UI를 생성할 수 있는 구조를 설명한다.
10. Unity Editor에서 수행해야 할 수동 설정도 단계별로 설명한다.

구현 후 수정하거나 생성한 파일 목록과 테스트 방법을 정리해줘.
```

---

# 개발 단위 2. 정적 데이터 모델

## 목표

승객, 적, 웨이브, 역, 능력 카드 데이터를 정의한다.

## 구현 내용

* PassengerData
* PassengerLevelData
* EnemyData
* WaveData
* WaveSpawnData
* StationData
* AbilityData
* SynergyData
* RelicData
* Enum
* OnValidate 검증

## 완료 기준

* 승객의 기본 능력치와 등급별 성장값을 정의할 수 있다.
* 적의 체력, 속도, 객차 피해를 정의할 수 있다.
* 역에 여러 웨이브를 등록할 수 있다.
* 능력 카드 효과를 데이터로 등록할 수 있다.
* 잘못된 값은 Console 경고로 확인할 수 있다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 ScriptableObject 기반 정적 데이터 모델을 구현해줘.

필요한 데이터 클래스:
- PassengerData
- PassengerStarData
- EnemyData
- WaveData
- WaveSpawnData
- StationData
- AbilityData
- SynergyData
- RelicData

필요한 Enum:
- PassengerRole
- PassengerTag
- EnemyType
- TargetPriority
- DamageType
- AbilityEffectType
- StationType
- Rarity

요구사항:
1. PassengerData에는 ID, 이름, 역할, 태그, 기본 공격력, 공격 간격,
   사거리, 타깃 우선순위, 등급별 데이터가 포함되어야 한다.
2. EnemyData에는 ID, 체력, 이동속도, 객차 피해, 방어력, 적 유형이 포함되어야 한다.
3. StationData에는 여러 WaveData와 완료 보상이 포함되어야 한다.
4. 원본 ScriptableObject가 런타임 중 수정되지 않도록 한다.
5. 모든 데이터에 고유 ID가 있어야 한다.
6. OnValidate를 사용해 음수 값, 중복 ID 가능성, 누락된 참조를 검증한다.
7. 데이터베이스 역할을 하는 GameDatabase ScriptableObject를 추가한다.
8. 샘플 데이터 생성 절차를 설명한다.
```

---

# 개발 단위 3. 런타임 상태와 게임 세션

## 목표

한 회차의 게임 상태를 독립적으로 관리한다.

## 구현 내용

* RunState
* TrainState
* CurrencyState
* PassengerRuntime
* BattleState
* StationProgress
* RunHistory
* 초기화 및 종료

## 완료 기준

* 회차 시작 시 객차 내구도, 코인, 역 번호가 초기화된다.
* ScriptableObject와 런타임 상태가 분리된다.
* 승객별 현재 등급, 공격 쿨타임, 버프를 저장할 수 있다.
* 결과 화면에 전달할 RunResult를 생성할 수 있다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 런타임 상태 시스템을 구현해줘.

필요한 클래스:
- GameSession
- RunState
- TrainState
- CurrencyState
- PassengerRuntime
- BattleState
- StationProgress
- RunHistory
- RunResult

요구사항:
1. ScriptableObject는 정적 데이터로만 사용한다.
2. 한 회차의 변경 가능한 데이터는 RunState에 저장한다.
3. 객차 최대 내구도와 현재 내구도를 관리한다.
4. 코인의 획득과 사용을 관리한다.
5. 현재 역, 처치 수, 합성 수, 최고 승객 등급을 기록한다.
6. 상태 변경 시 C# event를 발행한다.
7. UI 코드는 상태 클래스 내부에 포함하지 않는다.
8. 회차 종료 시 RunResult를 생성한다.
9. EditMode 테스트를 작성한다.
```

---

# 개발 단위 4. 3×3 Grid와 승객 배치

## 목표

9개의 좌석에 승객을 배치하고 이동할 수 있게 한다.

## 구현 내용

* GridManager
* GridSlot
* PassengerView
* 드래그 시작·이동·종료
* 빈 슬롯 배치
* 슬롯 교환
* 유효하지 않은 이동 복귀
* 슬롯 하이라이트

## 완료 기준

* 승객을 빈 슬롯으로 이동할 수 있다.
* 두 승객의 위치를 교환할 수 있다.
* 화면 밖에 놓으면 원래 슬롯으로 돌아온다.
* 드래그 중 가능한 슬롯이 표시된다.
* Grid 로직과 UI 표현이 분리된다.

## Cursor 요청 프롬프트

```text
Unity UGUI 기반 3x3 승객 배치 Grid를 구현해줘.

요구사항:
1. GridManager는 9개의 GridSlot을 관리한다.
2. 각 슬롯은 0~8의 고유 Index를 갖는다.
3. PassengerView를 드래그해서 빈 슬롯으로 이동할 수 있다.
4. 다른 승객이 있는 슬롯에 놓으면 위치를 교환한다.
5. 유효하지 않은 위치에 놓으면 원래 슬롯으로 복귀한다.
6. 드래그 중 이동 가능한 슬롯을 시각적으로 표시한다.
7. 실제 승객 상태는 GridManager가 관리하고 View는 표시만 담당한다.
8. 모바일 터치를 지원하고 에디터에서는 마우스로 테스트할 수 있게 한다.
9. EventSystem 설정과 Prefab 구조를 설명한다.
10. PlayMode 테스트가 가능한 부분은 테스트를 작성한다.
```

---

# 개발 단위 5. 승객 생성과 기본 공격

## 목표

좌석에 배치된 승객이 적을 탐색하고 자동 공격하게 한다.

## 구현 내용

* PassengerFactory
* PassengerController
* TargetingService
* AttackController
* 공격 쿨타임
* 타깃 우선순위
* 사거리
* 기본 발사체

## 완료 기준

* 승객이 사거리 내 적을 자동 탐색한다.
* 공격속도에 따라 공격한다.
* 적이 사망하면 새로운 타깃을 찾는다.
* 가장 가까운 적 또는 빠른 적 우선 등 타깃 규칙을 지원한다.
* 승객 등급에 따라 공격력이 달라진다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 승객 자동 공격 시스템을 구현해줘.

필요한 클래스:
- PassengerFactory
- PassengerController
- PassengerAttackController
- TargetingService
- ProjectileController
- ProjectilePool

요구사항:
1. Grid에 배치된 승객만 공격한다.
2. PassengerData와 PassengerRuntime을 조합해 최종 공격력을 계산한다.
3. 공격 간격에 따라 자동 공격한다.
4. 사거리 내 살아 있는 적만 공격한다.
5. Nearest, Fastest, LowestHealth, BossFirst 타깃 우선순위를 지원한다.
6. 발사체는 Object Pool을 사용한다.
7. 타깃이 사망하면 발사체 처리 방식을 명확하게 정한다.
8. 승객 View와 전투 로직을 분리한다.
9. 공격 애니메이션이 없어도 동작해야 한다.
10. 공격력과 공격 주기에 대한 EditMode 테스트를 작성한다.
```

---

# 개발 단위 6. 적 이동과 객차 피해

## 목표

적이 출입구에서 생성되어 객차로 이동하고 피해를 주게 한다.

## 구현 내용

* EnemyFactory
* EnemyController
* EnemyRuntime
* 이동 경로
* 객차 도달
* 객차 피해
* 적 사망
* 코인 보상
* Object Pool

## 완료 기준

* 적이 지정 위치에서 등장한다.
* 경로를 따라 객차로 이동한다.
* 객차 도달 시 피해를 주고 제거된다.
* 체력이 0이 되면 사망하고 코인을 지급한다.
* 적 생성과 제거에 Object Pool을 사용한다.

## Cursor 요청 프롬프트

```text
적 생성, 이동, 피해, 사망 시스템을 구현해줘.

필요한 클래스:
- EnemyFactory
- EnemyController
- EnemyRuntime
- EnemyPool
- DamageService
- TrainDamageService

요구사항:
1. EnemyData에서 체력, 이동속도, 객차 피해를 가져온다.
2. 적은 SpawnPoint에서 TrainTarget까지 이동한다.
3. 이동 중 피해를 받을 수 있다.
4. 체력 0 이하에서 한 번만 사망 처리한다.
5. 사망 시 코인을 지급하고 처치 기록을 증가시킨다.
6. TrainTarget 도달 시 객차 내구도를 감소시키고 적을 제거한다.
7. 적은 Object Pool로 재사용한다.
8. 사망과 객차 도달이 동시에 중복 처리되지 않도록 한다.
9. 피해량 및 객차 피해 테스트를 작성한다.
```

---

# 개발 단위 7. 웨이브와 역 진행

## 목표

StationData에 따라 적 웨이브를 실행하고 역을 완료한다.

## 구현 내용

* WaveManager
* StationManager
* Spawn sequence
* 웨이브 완료 판정
* 역 완료 판정
* 준비 단계와 전투 단계
* 게임 상태 머신

## 게임 상태

```text
Preparing
→ WaveStarting
→ Fighting
→ WaveCompleted
→ StationCompleted
→ RewardSelecting
→ Preparing
```

## 완료 기준

* StationData에 등록된 순서대로 웨이브가 실행된다.
* 모든 적 생성과 처치가 끝나야 웨이브가 완료된다.
* 마지막 웨이브 종료 후 역 완료 이벤트가 발생한다.
* 준비 단계에서는 전투가 멈춘다.
* 게임오버 시 추가 적 생성이 중단된다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 StationManager와 WaveManager를 구현해줘.

요구사항:
1. StationData의 WaveData를 순서대로 실행한다.
2. WaveSpawnData에 따라 적 종류, 수, 생성 간격을 처리한다.
3. 생성 예약 중인 적과 현재 살아 있는 적 수를 모두 추적한다.
4. 모든 적 생성과 제거가 완료되어야 WaveCompleted가 된다.
5. 마지막 웨이브 완료 후 StationCompleted 이벤트를 발생시킨다.
6. Preparing, Fighting, RewardSelecting 상태를 명확하게 관리한다.
7. 게임오버 시 Coroutine과 생성 작업을 중단한다.
8. 시간 배율 변경에도 안정적으로 동작하도록 한다.
9. Station 진행 로직에 대한 테스트를 작성한다.
```

---

# 개발 단위 8. 승객 합성

## 목표

동일 승객과 동일 등급을 합쳐 상위 등급으로 만든다.

## 구현 내용

* MergeService
* 합성 가능 판정
* 드래그 합성
* 등급 상승
* 최대 등급 처리
* 합성 연출 이벤트
* 합성 이력

## 완료 기준

* 같은 ID와 같은 등급만 합성된다.
* 서로 다른 승객은 기존 위치 교환으로 처리된다.
* 합성 후 한 승객만 남는다.
* 등급에 따라 전투 능력치가 즉시 변경된다.
* 최대 등급 승객은 합성되지 않는다.

## Cursor 요청 프롬프트

```text
3x3 Grid 승객 합성 시스템을 구현해줘.

요구사항:
1. 같은 Passenger ID와 같은 Star Level만 합성할 수 있다.
2. 승객 A를 승객 B 슬롯에 드롭하면 MergeService가 판정한다.
3. 합성 가능하면 A를 제거하고 B의 등급을 1 증가시킨다.
4. 합성 불가능하면 두 승객의 슬롯을 교환한다.
5. 최대 등급은 합성할 수 없다.
6. 합성 후 공격력, 공격 주기, 스킬 수치가 즉시 갱신된다.
7. 합성 횟수를 RunHistory에 기록한다.
8. 합성 시작, 완료 이벤트를 제공해 UI 연출과 분리한다.
9. 합성 가능 조건과 최대 등급 테스트를 작성한다.
```

---

# 개발 단위 9. 코인과 승객 소환

## 목표

코인을 사용해 승객 후보를 생성하고 선택할 수 있게 한다.

## 구현 내용

* CurrencyService
* PassengerOfferService
* 후보 3개 생성
* 승객 선택
* 소환 비용 증가
* 빈 슬롯 검사
* 승객 판매
* 리롤

## 완료 기준

* 코인이 부족하면 소환할 수 없다.
* 소환 시 후보 3명이 표시된다.
* 하나를 선택하면 빈 슬롯에 배치된다.
* 선택 후 소환 비용이 증가한다.
* 빈 슬롯이 없으면 안내가 표시된다.
* 승객을 판매하면 코인을 획득한다.

## Cursor 요청 프롬프트

```text
코인과 승객 소환 시스템을 구현해줘.

필요한 클래스:
- CurrencyService
- PassengerOfferService
- SummonCostCalculator
- PassengerSellService

요구사항:
1. 기본 소환 비용은 설정 데이터에서 가져온다.
2. 소환 횟수에 따라 비용이 증가한다.
3. 소환 요청 시 승객 후보 3명을 생성한다.
4. 동일한 Random Seed에서 같은 후보를 생성할 수 있어야 한다.
5. 잠금 해제된 승객만 후보에 포함한다.
6. 후보 하나를 선택하면 빈 Grid 슬롯에 승객을 생성한다.
7. 빈 슬롯이 없으면 소환을 시작하지 않는다.
8. 리롤 기능을 제공하되 광고 연결은 아직 Mock 처리한다.
9. 승객 판매 시 등급별 코인을 지급한다.
10. 코인 차감과 승객 생성이 원자적으로 처리되게 한다.
11. 후보 생성과 비용 계산 테스트를 작성한다.
```

---

# 개발 단위 10. 전투 HUD와 플레이 UI

## 목표

게임의 전체 플레이 흐름을 UI에서 조작할 수 있게 한다.

## 구현 내용

* 객차 내구도
* 코인
* 현재 역
* 웨이브
* 3×3 Grid
* 소환 버튼
* 준비 완료 버튼
* 승객 후보 팝업
* 승객 상세 팝업
* 피해·코인 텍스트
* 게임 속도 버튼

## 완료 기준

* 런타임 상태가 UI에 실시간 반영된다.
* UI는 상태를 직접 변경하지 않는다.
* 소환 후보에서 하나를 선택할 수 있다.
* 준비 완료 버튼으로 다음 전투를 시작한다.
* 선택 중 중복 입력이 방지된다.

## Cursor 요청 프롬프트

```text
Unity UGUI로 막차 생존 Game Scene의 HUD를 구현해줘.

UI 구성:
- 객차 내구도 게이지
- 현재 코인
- 현재 역과 전체 역
- 현재 웨이브
- 3x3 승객 Grid
- 승객 소환 버튼과 소환 비용
- 준비 완료 버튼
- 승객 후보 3개 팝업
- 승객 상세 및 판매 팝업
- 전투 속도 1x, 2x 버튼
- 일시정지 버튼

요구사항:
1. UI는 GameSession 이벤트를 구독해 갱신한다.
2. UI가 RunState를 직접 수정하지 않게 한다.
3. 모든 버튼은 중복 입력 방지 처리를 한다.
4. 실제 아트가 없어도 Placeholder Sprite로 동작한다.
5. 승객 등급을 별 아이콘 또는 숫자로 표시한다.
6. 능력치 변화와 코인 획득을 간단한 Coroutine 연출로 표시한다.
7. 필요한 Prefab 계층과 Inspector 연결 절차를 설명한다.
```

---

# 개발 단위 11. 능력 카드

## 목표

역 완료 후 능력 카드 3개 중 하나를 선택해 회차에 적용한다.

## 구현 내용

* AbilityManager
* 후보 생성
* 희귀도
* 효과 적용
* 중복 가능 여부
* 카드 리롤
* 능력 목록 UI

## 완료 기준

* 역 완료 후 카드 후보 3개가 표시된다.
* 하나를 선택하면 RunState에 적용된다.
* 공격력·경제·객차 버프가 정상 반영된다.
* 중복 불가 카드는 다시 등장하지 않는다.
* 카드 효과가 전투 중 적용된다.

## Cursor 요청 프롬프트

```text
능력 카드 선택 시스템을 구현해줘.

요구사항:
1. AbilityData는 ScriptableObject로 관리한다.
2. 역 완료 후 후보 3개를 생성한다.
3. 희귀도 기반 가중치 랜덤을 사용한다.
4. 동일한 Random Seed에서 동일한 결과를 생성한다.
5. 카드 효과로 공격력, 공격속도, 객차 최대 내구도,
   처치 코인, 소환 비용 등을 변경할 수 있어야 한다.
6. 중복 가능 여부와 최대 중첩 수를 지원한다.
7. 카드 선택 후 RunState에 영구 적용한다.
8. 카드 UI는 데이터와 분리한다.
9. 광고 리롤을 연결할 수 있는 인터페이스를 제공한다.
10. 효과 계산 테스트를 작성한다.
```

---

# 개발 단위 12. 승객 고유 스킬

## 목표

승객별 차별화된 패시브 또는 액티브 능력을 구현한다.

## MVP 스킬

* 헬스 트레이너: 넉백
* 간호사: 객차 회복
* 개발자: 임시 터렛
* 대학원생: 확률형 광역 치명타

## 완료 기준

* 승객별 스킬을 교체 가능한 구조로 구현한다.
* 승객 컨트롤러에 대형 switch 문을 작성하지 않는다.
* 등급과 능력 카드에 따라 스킬 수치가 변경된다.
* 스킬 이펙트가 없어도 로직이 동작한다.

## Cursor 요청 프롬프트

```text
승객별 고유 스킬 시스템을 전략 패턴 또는 인터페이스 기반으로 구현해줘.

필요한 스킬:
1. KnockbackSkill
2. TrainHealSkill
3. TemporaryTurretSkill
4. CriticalAreaDamageSkill

요구사항:
1. IPassengerSkill 인터페이스를 정의한다.
2. PassengerController가 구체적인 스킬 종류를 직접 판정하지 않게 한다.
3. 등급별 스킬 수치 증가를 지원한다.
4. 능력 카드 버프를 스킬 계산에 반영한다.
5. 스킬 쿨타임과 발동 조건을 지원한다.
6. 넉백은 적 이동 경로를 안전하게 되돌린다.
7. 객차 회복은 최대 내구도를 넘지 않는다.
8. 터렛은 제한 시간 후 자동 제거하고 Object Pool을 사용한다.
9. 광역 피해는 범위 내 적에게 한 번씩만 적용한다.
10. 핵심 스킬별 테스트를 작성한다.
```

---

# 개발 단위 13. 시너지 시스템

## 목표

배치된 승객 조합에 따라 버프를 적용한다.

## 구현 내용

* SynergyManager
* 태그 카운트
* 조건 판정
* 활성·비활성
* 버프 적용 및 제거
* UI 표시

## 완료 기준

* 배치 또는 합성 후 시너지가 다시 계산된다.
* 조건을 만족하면 효과가 적용된다.
* 조건이 해제되면 버프도 제거된다.
* 동일 시너지가 중복 적용되지 않는다.

## Cursor 요청 프롬프트

```text
승객 조합 시너지 시스템을 구현해줘.

요구사항:
1. Grid에 배치된 PassengerRuntime의 태그를 집계한다.
2. SynergyData에 필요한 태그, 필요 수량, 효과를 정의한다.
3. 승객 배치, 이동, 합성, 판매 시 시너지를 다시 계산한다.
4. 조건 충족 시 버프를 적용하고 해제 시 정확하게 제거한다.
5. 동일 버프가 중복 누적되지 않게 한다.
6. 활성 시너지 목록을 UI에 전달하는 이벤트를 제공한다.
7. 공격력, 공격속도, 치명타, 객차 회복량 버프를 지원한다.
8. 시너지 판정 테스트를 작성한다.
```

---

# 개발 단위 14. 보스와 특수 적

## 목표

일반 적과 다른 패턴을 가진 보스를 구현한다.

## 구현 내용

* EnemyAbility
* BossController
* 추가 적 소환
* 승객 공격속도 감소
* 보스 체력 UI
* 보스 단계 전환

## 완료 기준

* 보스가 일반 적과 같은 이동·피해 구조를 재사용한다.
* 일정 시간마다 스킬을 사용한다.
* 체력 구간에 따라 패턴이 변경된다.
* 보스 사망 시 역 완료 처리가 정상 동작한다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 보스 시스템을 구현해줘.

MVP 보스 패턴:
1. 일정 시간마다 일반 적 3마리 소환
2. 일정 시간 동안 모든 승객 공격속도 20% 감소
3. 체력 30% 이하에서 이동속도 증가

요구사항:
1. BossController는 EnemyController를 확장하거나 조합해 기존 이동과 피해 로직을 재사용한다.
2. 보스 스킬은 IEnemyAbility 인터페이스 기반으로 구현한다.
3. 보스 체력 구간별 Phase를 지원한다.
4. 버프와 디버프는 일정 시간 후 정확히 제거한다.
5. 보스 사망 시 남은 소환 Coroutine을 중단한다.
6. 보스 체력 UI 이벤트를 제공한다.
7. 일반 적과 보스의 중복 코드를 최소화한다.
```

---

# 개발 단위 15. 게임오버와 결과 화면

## 목표

객차 내구도 0 또는 최종 보스 처치 시 회차를 종료한다.

## 구현 내용

* 종료 판정
* RunResult
* 게임오버 팝업
* 성공 화면
* 통계 표시
* 재시작
* 메인 메뉴 이동

## 완료 기준

* 객차 내구도 0에서 게임오버된다.
* 최종 역 완료 시 성공 처리된다.
* 종료 후 전투와 입력이 중단된다.
* 결과 화면에 통계가 표시된다.
* 다시 시작 시 상태가 완전히 초기화된다.

## Cursor 요청 프롬프트

```text
회차 종료와 Result Scene을 구현해줘.

요구사항:
1. 객차 내구도 0이면 실패 처리한다.
2. 최종 Station 완료 시 성공 처리한다.
3. 종료 처리는 한 번만 실행되어야 한다.
4. 모든 Wave, 적 생성, 공격, 입력을 중단한다.
5. RunResult에 도달 역, 처치 수, 합성 수, 최고 승객 등급,
   남은 내구도, 획득 코인을 포함한다.
6. Result Scene에서 성공과 실패를 구분해 표시한다.
7. 다시 시작과 메인 메뉴 이동을 지원한다.
8. Scene 재시작 후 이전 회차 상태가 남지 않게 한다.
```

---

# 개발 단위 16. 저장과 이어하기

## 목표

앱 종료 후 현재 회차와 영구 데이터를 복원한다.

## 구현 내용

* RunSaveData
* MetaSaveData
* Grid 저장
* 승객 상태 저장
* 능력 카드 저장
* 현재 역 저장
* JSON 파일
* 저장 버전
* 손상 복구

## 완료 기준

* 준비 단계에서 현재 회차를 저장할 수 있다.
* 앱 재실행 후 동일한 배치와 역에서 이어진다.
* 전투 도중 저장은 MVP에서 제한한다.
* 회차 종료 후 RunSaveData를 삭제한다.
* 손상된 파일은 안전하게 초기화한다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 로컬 저장 시스템을 구현해줘.

요구사항:
1. JSON 파일 기반 ISaveService를 만든다.
2. RunSaveData와 MetaSaveData를 분리한다.
3. RunSaveData에 현재 역, 코인, 객차 내구도,
   9개 슬롯의 승객 ID와 등급, 선택한 능력 카드를 저장한다.
4. Preparing 상태에서만 이어하기 저장을 생성한다.
5. 전투 도중 앱이 종료된 경우 마지막 Preparing 저장으로 복원한다.
6. 저장 데이터에 version 필드를 포함한다.
7. 필드 누락과 파일 손상 시 기본값으로 안전하게 복구한다.
8. 회차 종료 시 RunSaveData만 삭제한다.
9. 앱 Pause와 Quit 시 저장을 시도한다.
10. 저장과 복원 테스트를 작성한다.
```

---

# 개발 단위 17. 메타 성장과 도감

## 목표

반복 플레이를 위한 영구 진행 시스템을 추가한다.

## 구현 내용

* 승차권 조각
* 계정 레벨
* 승객 해금
* 승객 숙련도
* 도감
* 업적
* 결과 보상

## 완료 기준

* 회차 결과에 따라 승차권 조각을 획득한다.
* 특정 조건으로 승객이 해금된다.
* 사용한 승객의 숙련도가 증가한다.
* 처음 발견한 승객과 보스가 도감에 등록된다.
* 중복 보상이 발생하지 않는다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 MetaProgression 시스템을 구현해줘.

기능:
- 승차권 조각
- 계정 레벨
- 승객 해금
- 승객 숙련도
- 승객 도감
- 적 도감
- 업적

요구사항:
1. MetaSaveData에 영구 저장한다.
2. 도달 역, 보스 처치, 처치 수, 새 도감 발견을 기준으로 보상을 계산한다.
3. 기본 승객과 잠금 승객을 구분한다.
4. 잠금된 승객은 소환 후보에 등장하지 않는다.
5. 승객 사용 횟수와 최고 달성 등급으로 숙련도 경험치를 계산한다.
6. 새 해금 이벤트를 UI에 전달한다.
7. 중복 도감과 중복 보상 지급을 방지한다.
8. 메인 메뉴에서 해금 진행률을 표시할 수 있게 한다.
```

---

# 개발 단위 18. 디버그 패널과 자동 시뮬레이션

## 목표

밸런스와 오류를 빠르게 검증한다.

## 구현 내용

* 코인 추가
* 내구도 변경
* 원하는 승객 생성
* 등급 변경
* 특정 역 이동
* 특정 웨이브 실행
* 보스 즉시 호출
* 랜덤 시드 고정
* 피해량 로그
* 자동 전투 통계

## 완료 기준

* 원하는 상태를 에디터에서 즉시 만들 수 있다.
* 100~1,000회 자동 전투 결과를 출력할 수 있다.
* 승객별 평균 피해량과 성공률을 확인할 수 있다.
* Release 빌드에는 포함되지 않는다.

## Cursor 요청 프롬프트

```text
에디터 전용 DebugPanel과 전투 시뮬레이터를 구현해줘.

DebugPanel 기능:
- 코인 설정
- 객차 내구도 설정
- 원하는 승객과 등급을 슬롯에 생성
- 특정 역과 웨이브 시작
- 모든 적 제거
- 보스 호출
- 랜덤 시드 설정
- 게임 즉시 성공 또는 실패
- 게임 속도 변경

시뮬레이터 기능:
1. 지정한 승객 배치로 전투를 여러 번 실행한다.
2. 승률, 객차 남은 내구도, 승객별 피해량, 전투 시간을 집계한다.
3. 결과를 Console과 CSV로 출력할 수 있게 한다.
4. Editor에서만 컴파일되게 한다.
5. Release Build에는 포함되지 않게 한다.
```

---

# 개발 단위 19. 광고 시스템 추상화

## 목표

실제 광고 SDK와 게임 로직을 분리한다.

## 구현 내용

* IAdService
* MockAdService
* RewardedAdPlacement
* 보상 지급
* 회차당 제한
* 광고 실패
* 쿨다운

## 완료 기준

* 에디터에서 Mock 광고가 정상 동작한다.
* 광고 완료 시에만 보상이 지급된다.
* 광고 실패 시 게임이 멈추지 않는다.
* 동일 보상을 중복 수령할 수 없다.
* 실제 SDK를 교체 가능한 구조다.

## Cursor 요청 프롬프트

```text
광고 SDK 독립적인 광고 시스템을 구현해줘.

요구사항:
1. IAdService 인터페이스를 만든다.
2. ShowRewardedAd, ShowInterstitial, IsRewardedReady를 제공한다.
3. Editor용 MockAdService를 구현한다.
4. RewardedAdPlacement Enum에 PassengerReroll, AbilityReroll,
   Revive, DoubleResultReward, FreeSummon을 포함한다.
5. 광고 완료 Callback 이후에만 보상을 지급한다.
6. 회차당 리롤과 부활 횟수를 제한한다.
7. 광고 로딩 실패와 취소를 별도로 처리한다.
8. 광고가 없어도 게임 진행이 가능해야 한다.
9. 구체적인 광고 SDK에 GameManager가 직접 의존하지 않게 한다.
```

---

# 개발 단위 20. 분석 이벤트

## 목표

이탈 구간과 승객·광고 사용 패턴을 측정한다.

## 구현 내용

* IAnalyticsService
* DebugAnalyticsService
* 전투 이벤트
* 배치·합성 이벤트
* 광고 이벤트
* 회차 결과
* 세션 식별자

## 완료 기준

* Editor Console에서 이벤트가 확인된다.
* 승객 선택과 합성 데이터가 기록된다.
* 역별 실패율을 계산할 수 있다.
* 광고 제안과 완료를 구분한다.
* 개인 식별 정보는 수집하지 않는다.

## Cursor 요청 프롬프트

```text
막차 생존 게임의 분석 이벤트 시스템을 구현해줘.

요구사항:
1. IAnalyticsService 인터페이스를 만든다.
2. Editor용 DebugAnalyticsService를 구현한다.
3. 이벤트 이름은 snake_case를 사용한다.
4. 다음 이벤트를 지원한다:
   run_started, station_started, station_completed,
   passenger_selected, passenger_merged, passenger_sold,
   ability_selected, boss_started, boss_defeated,
   run_failed, run_completed,
   rewarded_ad_offered, rewarded_ad_completed
5. 파라미터는 Dictionary<string, object>로 전달한다.
6. Run ID와 Station Index를 공통 파라미터로 관리한다.
7. 구체적인 Firebase SDK를 게임 로직이 알지 못하게 한다.
8. 이메일, 이름, 광고 ID 같은 개인 식별 정보는 기록하지 않는다.
```

---

# 개발 단위 21. 실제 광고·Firebase·Remote Config

## 시작 조건

다음 조건을 충족한 뒤 진행한다.

* 광고 없이 5개 역을 정상 플레이할 수 있다.
* 승객 배치와 합성이 재미있다는 테스트 의견이 있다.
* 최소 20명의 테스트 결과를 확보했다.
* 크래시 없이 여러 회차를 완료할 수 있다.
* 저장과 복원이 안정적이다.

## 구현 내용

* 실제 보상형 광고
* 전면 광고
* Firebase Analytics
* Crashlytics
* Remote Config
* 광고 빈도 원격 제어
* 밸런스 값 원격 제어

## 완료 기준

* 테스트 광고가 실제 기기에서 출력된다.
* 광고 보상이 한 번만 지급된다.
* Remote Config 실패 시 로컬 기본값을 사용한다.
* 광고 빈도와 일부 경제 수치를 앱 업데이트 없이 변경할 수 있다.

---

# 개발 단위 22. 출시 준비

## 목표

Google Play 내부 테스트용 AAB를 제작한다.

## 구현 내용

* 앱 아이콘
* 시작 화면
* 사운드·진동 설정
* 개인정보처리방침
* 광고 동의
* 앱 버전
* 서명키
* Release 빌드
* 실제 기기 테스트
* 스토어 이미지

## 완료 기준

* Android App Bundle이 생성된다.
* 실제 기기에서 전체 회차를 완료할 수 있다.
* 오프라인에서 핵심 게임이 실행된다.
* 광고 실패 시 오류가 발생하지 않는다.
* 앱 강제 종료 후 이어하기가 정상 동작한다.
* 개인정보처리방침에 접근할 수 있다.

---

# 권장 개발 순서

## 1차: 코어 프로토타입

```text
1. 프로젝트 기반
2. 데이터 모델
3. 런타임 상태
4. 3×3 Grid
5. 승객 기본 공격
6. 적 이동과 피해
7. 웨이브
8. 합성
```

이 단계의 완료 기준:

> 승객을 배치하고 합쳐서 적 웨이브를 막을 수 있다.

---

## 2차: 플레이 가능한 MVP

```text
9. 소환과 코인
10. 전투 UI
11. 능력 카드
12. 고유 스킬
14. 보스
15. 결과 화면
16. 저장
```

이 단계의 완료 기준:

> 5개의 역과 보스가 포함된 한 회차를 처음부터 끝까지 플레이할 수 있다.

---

## 3차: 반복 플레이 구조

```text
13. 시너지
17. 메타 성장
18. 디버그와 시뮬레이션
```

이 단계의 완료 기준:

> 여러 회차를 반복하면서 새로운 승객과 능력을 해금할 수 있다.

---

## 4차: 수익화와 출시

```text
19. 광고 추상화
20. 분석 이벤트
21. 실제 광고와 Firebase
22. 출시 준비
```

이 단계의 완료 기준:

> Google Play 내부 테스트에서 실제 광고와 분석이 동작한다.

---

# Cursor 공통 작업 규칙

각 개발 단위 프롬프트 마지막에 다음 내용을 붙인다.

```text
공통 작업 규칙:

1. 현재 프로젝트의 기존 코드를 먼저 분석하고 중복 클래스를 만들지 마라.
2. 요청한 개발 단위 외의 다음 기능을 임의로 구현하지 마라.
3. 하나의 클래스가 여러 책임을 갖지 않게 하라.
4. 게임 로직, 데이터, View, UI를 분리하라.
5. public field보다 [SerializeField] private를 우선 사용하라.
6. FindObjectOfType와 GameObject.Find 사용을 피하라.
7. 구체적인 Manager Singleton을 무분별하게 추가하지 마라.
8. null 참조 가능성을 검사하고 오류 메시지를 명확하게 작성하라.
9. Object Pool이 필요한 객체는 반복 Instantiate/Destroy하지 마라.
10. 수정하거나 생성한 파일 목록을 응답 마지막에 정리하라.
11. Unity Editor에서 해야 하는 수동 설정을 단계별로 설명하라.
12. 구현 후 테스트 시나리오와 예상 결과를 제공하라.
13. 가능한 핵심 로직에는 EditMode 또는 PlayMode 테스트를 작성하라.
14. 기존 기능에 영향을 주는 변경이라면 영향 범위를 먼저 설명하라.
15. 컴파일 오류가 발생하지 않는 완전한 코드를 작성하라.
```

---

# 개발 체크포인트

## 체크포인트 A: 전투 원형

* 3×3 좌석이 표시된다.
* 승객을 이동할 수 있다.
* 승객이 자동 공격한다.
* 적이 이동한다.
* 적이 객차에 도달하면 내구도가 감소한다.

개발 단위 1~7 완료 시점이다.

## 체크포인트 B: 합성 재미 검증

* 승객을 소환한다.
* 같은 승객을 합친다.
* 상위 등급이 더 강해진다.
* 코인을 어디에 쓸지 선택한다.
* 5개 역을 진행한다.

개발 단위 8~10 완료 시점이다.

## 체크포인트 C: 완성된 MVP

* 능력 카드를 고른다.
* 승객별 스킬이 다르다.
* 보스가 등장한다.
* 게임오버와 성공이 존재한다.
* 저장하고 이어할 수 있다.

개발 단위 11~16 완료 시점이다.

## 체크포인트 D: 알파 테스트

* 시너지가 존재한다.
* 승객이 해금된다.
* 여러 회차를 반복할 이유가 있다.
* 자동 시뮬레이션으로 밸런스를 확인한다.

개발 단위 17~18 완료 시점이다.

## 체크포인트 E: 출시 후보

* 보상형 광고가 정상 동작한다.
* 분석 이벤트가 기록된다.
* Firebase 오류가 게임을 중단시키지 않는다.
* AAB를 만들어 실제 기기에서 검증한다.

개발 단위 19~22 완료 시점이다.
