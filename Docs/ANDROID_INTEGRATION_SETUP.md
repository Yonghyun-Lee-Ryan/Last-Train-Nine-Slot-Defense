# Android 통합 SDK 설정 (Unit 21)

Unity **6000.5.4f1** 기준으로 Google AdMob, Firebase Analytics, Crashlytics, Remote Config를 연결하는 절차입니다.

코드는 `#if LASTTRAIN_ADMOB` / `#if LASTTRAIN_FIREBASE` 조건부 컴파일로 감싸져 있어, **SDK 미설치 상태에서도 Editor·Release 빌드가 컴파일·실행**됩니다. SDK 연결 전에는 Mock(개발) / NoOp(릴리스·동의 없음)으로 동작합니다.

---

## 1. 사전 준비

| 항목 | 값 |
|------|-----|
| Unity | 6000.5.4f1 |
| Android Min SDK | API 24+ 권장 |
| Scripting Backend | IL2CPP (Unit 22 출시 빌드) |
| 패키지 이름 | `com.yourstudio.lasttrain` (Unit 22에서 확정) |

Firebase Console과 AdMob Console에서 Android 앱을 등록하고 `google-services.json`을 받습니다.

---

## 2. Unity 패키지 설치

### Firebase (Analytics + Crashlytics + Remote Config)

1. [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup)에서 **Unity 6000** 호환 버전을 확인합니다.
2. `FirebaseAnalytics.unitypackage`, `FirebaseCrashlytics.unitypackage`, `FirebaseRemoteConfig.unitypackage`를 Import합니다.
3. `Assets/google-services.json`을 프로젝트 루트 `Assets/`에 배치합니다.
4. **External Dependency Manager**가 Android Resolver를 실행하도록 `Assets → External Dependency Manager → Android Resolver → Resolve`를 실행합니다.

### Firebase (Analytics + Crashlytics + Remote Config) — Unit 36

**현재 Release AAB에는 Firebase SDK / `LASTTRAIN_FIREBASE`를 넣지 않습니다.**  
`google-services.json` 없이 SDK만 넣으면 Gradle 실패 또는 기동 크래시가 납니다. define OFF일 때:

- Analytics: Editor/Dev → `DebugAnalyticsService`, Release → `NoOpAnalyticsService` (동의 없으면 동일)
- Crashlytics: `DebugCrashReporter`
- Remote Config: `RemoteConfigDefaults` ScriptableObject

Soft Launch 직전:

1. [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup)에서 Analytics / Crashlytics / Remote Config Import (또는 `.tgz` UPM)
2. `Assets/google-services.json` 배치
3. External Dependency Manager Resolve
4. **Tools → 막차 생존 → Integration → Enable LASTTRAIN_FIREBASE**

어댑터:

- `FirebaseAppBootstrap` — `CheckAndFixDependenciesAsync`
- `FirebaseAnalyticsService` — `LogEvent` + 파라미터 변환
- `FirebaseCrashReporter` — Log / LogException
- `RemoteConfigService` — Fetch/Activate 실패 시 ScriptableObject 기본값

---

### Google Mobile Ads (AdMob) — Unit 35

**현재 Release AAB에는 `com.google.ads.mobile`을 넣지 않습니다.** App ID·설정 에셋 없이 패키지만 넣으면 Gradle 실패 또는 기동 크래시가 납니다. 코드는 `#if LASTTRAIN_ADMOB`로 가드되어 define 없이 NoOp로 플레이됩니다.

OpenUPM 스코프는 `Packages/manifest.json`에 이미 있습니다. Soft Launch 직전에만 의존성을 추가합니다:

```json
"dependencies": {
  "com.google.ads.mobile": "11.3.0"
}
```

1. Unity가 `com.google.ads.mobile`을 resolve할 때까지 기다립니다.
2. `Assets → External Dependency Manager → Android Resolver → Resolve` 실행.
3. `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`에서 Android App ID 설정
   - 테스트: `ca-app-pub-3940256099942544~3347511713` (Google 샘플 App ID)
   - 운영: AdMob Console App ID
4. **Tools → 막차 생존 → Integration → Enable LASTTRAIN_ADMOB**  
   (패키지 미설치 시 define을 켜지 않음 — 컴파일 보호)

대안: [GitHub Releases](https://github.com/googleads/googleads-mobile-unity/releases) `.unitypackage` Import.

`AdMobAdService`는 Load → Show → Destroy → 재Load, 실패 시 `NotReady`/`Failed`만 반환해 게임 루프를 막지 않습니다.
Editor/Dev는 테스트 광고 단위 ID, Release는 `AdUnitConfig` 운영 ID(비어 있으면 테스트 ID fallback).

---

## 3. Scripting Define Symbols

**Tools → 막차 생존 → Integration → Enable LASTTRAIN_ADMOB** 또는  
**Edit → Project Settings → Player → Android → Scripting Define Symbols**에 추가:

```
LASTTRAIN_ADMOB
LASTTRAIN_FIREBASE
```

SDK 미설치 상태에서 define만 켜면 컴파일이 실패합니다. 패키지 resolve 후 define을 켭니다.

Editor 전용 테스트는 `DEVELOPMENT_BUILD`와 함께 **테스트 광고 단위 ID**를 사용합니다 (`AdServiceFactory.UseTestAdUnitIds`).

보상형 플레이스: Revive / DoubleResult / StationDouble / PassengerReroll / AbilityReroll / FreeSummon / ShopRefresh.  
NoOp·미준비 시 관련 버튼은 숨깁니다.
---

## 4. ScriptableObject 에셋

`Assets/Data/Integration/`에 다음 에셋을 생성합니다.

| 에셋 | 메뉴 |
|------|------|
| `AdUnitConfig` | Create → Last Train → Integration → Ad Unit Config |
| `RemoteConfigDefaults` | Create → Last Train → Integration → Remote Config Defaults |

Bootstrap 씬의 **AppRoot** 컴포넌트에 두 에셋을 할당합니다.

- **Development / Editor**: `GetRewardedUnitId(useTestIds: true)` → Google 공식 테스트 ID
- **Release**: `androidRewardedProductionId`, `androidInterstitialProductionId`에 운영 단위 ID 입력

---

## 5. 개인정보 동의

`PrivacyConsentService`가 PlayerPrefs에 동의 상태를 저장합니다.

- **Editor / Development Build**: 부트 시 자동 동의(테스트 편의)
- **Release**: Unit 22 UI에서 `SetAdsConsent` / `SetAnalyticsConsent` 호출 전까지 **NoOpAdService** 및 Firebase 미수집

동의 없이 광고·분석 SDK를 호출하지 않습니다.

---

## 6. 실제 Android 기기 테스트 절차

### 6.1 Development Build (Mock / 테스트 ID)

1. **File → Build Settings → Android**
2. **Development Build** 체크, **Script Debugging** 선택(선택)
3. `LASTTRAIN_ADMOB` **미설정** 시: MockAdPopup으로 보상형 광고 시뮬레이션
4. `LASTTRAIN_ADMOB` **설정** + SDK 설치 후: Google 테스트 광고 단위 표시

### 6.2 보상형 광고 검증

1. 게임 시작 → 소환 패널 → **광고 리롤** 버튼
2. 광고 완료 후 **리롤 1회만** 적용되는지 확인 (`AdRewardService` RequestId 중복 방지)
3. 회차당 리롤 한도(2회) 소진 후 버튼 비활성 확인

### 6.3 전면 광고 검증

1. Remote Config 기본값: **3회차 완료 후**, 메인 메뉴 복귀 시 전면 광고 후보
2. 전투 중(`Fighting` / `WaveStarting`)에는 표시되지 않음
3. 보상형 광고 직후 **5초** 이내 전면 광고 없음

### 6.4 Firebase / Remote Config 오프라인

1. 기기를 **비행기 모드**로 전환
2. 앱 실행 → 메인 메뉴 → 게임 1회차 플레이 가능 확인
3. Remote Config fetch 실패 시 `RemoteConfigDefaults` ScriptableObject 값 사용

### 6.5 Crashlytics 테스트

1. Development Build에서 Debug Panel 또는 임시 버튼으로 `throw new Exception("Crashlytics test")` 실행
2. Firebase Console → Crashlytics에서 5~15분 내 이벤트 확인
3. SDK 미연결 시 Console에 `[CrashReporter]` 로그만 출력

---

## 7. Remote Config 키 (Firebase Console)

| 키 | 타입 | 설명 |
|----|------|------|
| `interstitial_interval_seconds` | int | 전면 광고 최소 간격(초) |
| `rewarded_daily_limit` | int | 보상형 광고 일일 총 한도 |
| `runs_before_interstitial` | int | 전면 광고 시작 전 최소 완료 회차 |
| `base_summon_cost` | int | 기본 소환 비용 |
| `summon_cost_increase` | int | 소환 비용 증가량 |
| `result_reward_multiplier` | float | 결과 메타 보상 배수 |
| `free_revive_per_run` | int | 회차당 부활 광고 한도 |
| `live_event_enabled` | bool | 라이브 이벤트 플래그 |

로컬 기본값: `Assets/Data/Integration/RemoteConfigDefaults.asset`

---

## 8. 실패 시 폴백 요약

| 상황 | 동작 |
|------|------|
| AdMob 초기화 실패 | `NoOpAdService` — 게임 진행 가능 |
| Firebase Analytics 실패 | `DebugAnalyticsService`(Dev) / `NoOp`(Release) |
| Remote Config fetch 실패 | ScriptableObject 기본값 |
| 광고 동의 없음 | `NoOpAdService` |
| 분석 동의 없음 | Firebase sink 미등록 |

---

## 9. EditMode 테스트

Unity Test Runner → EditMode → `IntegrationServiceTests` 실행:

- 개인정보 동의 게이팅
- Remote Config 로컬 폴백
- 광고 일일 한도 Remote Config 적용
- Composite Analytics fan-out

---

## 10. 알려진 후속 작업 (SDK Import 후)

`AdMobAdService.TryInitialize()`와 `FirebaseAnalyticsService.TryCreate()` 내부에 실제 SDK 초기화 코드를 연결해야 합니다. 현재는 심볼만 정의된 경우 경고 로그 후 NoOp/false를 반환합니다.

```csharp
// 예: AdMob (LASTTRAIN_ADMOB)
MobileAds.Initialize(status => { _initialized = true; });

// 예: Firebase Analytics (LASTTRAIN_FIREBASE)
FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => { ... });
```

SDK 버전별 API 차이는 Import한 패키지 Release Notes를 따릅니다.
