# Google Play Console — 내부 테스트 업로드

Unity에서 만든 **Release AAB**를 Play Console 내부 테스트 트랙에 올리는 절차입니다.  
빌드 설정은 [`ANDROID_RELEASE_SETUP.md`](ANDROID_RELEASE_SETUP.md)를 먼저 완료하세요.

## 사전 조건

- [ ] Google Play Console 앱 등록 (`com.lasttrain.nineslotdefense` 또는 AppReleaseConfig 패키지와 동일)
- [ ] 개발자 계정·앱 생성 완료 (초안 상태도 가능)
- [ ] `Tools → 막차 생존 → Release → Prepare For Play Internal Test` 통과
- [ ] `Builds/Android/LastTrain.aab` (또는 버전 파일명 AAB) 생성
- [ ] 개인정보처리방침 URL 준비 (내부 테스트라도 정책·데이터 안전에 필요할 수 있음)

## 1. AAB 생성

Unity:

1. **Tools → 막차 생존 → Release → 서명·버전업 후 Release AAB 빌드**
2. Keystore 비밀번호·Alias 입력 (Version Code +1 자동)
3. `Builds/Android/LastTrain-v0_1_0-b{N}.aab` 및 `LastTrain.aab` 확인

## 2. Play Console — 앱 생성 (최초 1회)

1. [Play Console](https://play.google.com/console) → 앱 만들기
2. 앱 이름: **막차 생존**
3. 기본 언어: 한국어
4. 앱/게임: **게임**
5. 무료/유료: 무료 (광고 포함 가능)
6. 선언: Play 정책·미국 수출법 등 확인

## 3. 필수 스토어 설정 (내부 테스트 최소)

내부 테스트는 프로덕션보다 요구가 덜하지만, 보통 아래가 막히는 경우가 많습니다.

| 항목 | 위치 | 메모 |
|------|------|------|
| 앱 액세스 | 정책 → 앱 액세스 | 전체 공개 / 제한 중 선택 |
| 광고 선언 | 앱 콘텐츠 → 광고 | 광고 포함 여부 |
| 콘텐츠 등급 | 앱 콘텐츠 | 설문 제출 |
| 타겟층 | 앱 콘텐츠 | 18세 미만 대상 여부 |
| 데이터 안전 | 앱 콘텐츠 | 로컬 저장·분석·광고 SDK (실 SDK 전이면 수집 범위에 맞게) |
| 개인정보처리방침 | 스토어 설정 | 실제 HTTPS URL |

> AdMob/Firebase를 아직 NoOp로 두면 데이터 안전에서 “수집 없음/로컬만”으로 맞춰 두고, SDK 연결 후 수정하세요.

## 4. 내부 테스트 트랙에 AAB 업로드

1. **테스트 → 내부 테스트 → 새 버전 만들기**
2. App Bundle 업로드 → `LastTrain.aab` 선택
3. Play App Signing 안내가 뜨면 **Google 관리 서명** 사용 권장
4. 출시 노트 (예: `0.1.0 내부 테스트 — 핵심 루프·저장·UI`)
5. **검토 → 출시 시작**

버전 코드(`androidBundleVersionCode`)는 **이전 업로드보다 커야** 합니다.  
다음 빌드 전 `AppReleaseConfig`의 Bundle Version Code를 +1 하고 Sync하세요.

## 5. 테스터 등록

1. 내부 테스트 → 테스터 → 이메일 목록 또는 Google 그룹 생성
2. **테스터 초대 링크** 복사
3. 테스터가 링크로 참여한 뒤 Play 스토어에서 앱 설치 (반영까지 수 분~수 시간)

## 6. 설치 확인

테스터 기기에서:

- [ ] Play 스토어에 “내부 테스트” 배지로 설치됨
- [ ] 첫 실행 동의 다이얼로그
- [ ] MainMenu → Game → Result 1회 클리어
- [ ] 강제 종료 후 이어하기
- [ ] 비행기 모드 핵심 플레이

상세는 [`RELEASE_QA_CHECKLIST.md`](RELEASE_QA_CHECKLIST.md).

## 7. 자주 막히는 오류

| 증상 | 조치 |
|------|------|
| Version code already used | Bundle Version Code 증가 후 재빌드 |
| You need to use a different package name | 콘솔 앱의 package와 Unity `applicationId` 불일치 |
| Target API level … | Target SDK 35+ 로 Sync 후 SDK 설치·재빌드 |
| Keystore / upload key mismatch | 최초 업로드에 쓴 업로드 키와 동일한 키 사용 |
| Missing privacy policy | 스토어 설정에 HTTPS 정책 URL 등록 |
| Deobfuscation / symbols | Il2CPP 심볼 업로드는 선택(크래시 분석용) |

## 8. Checkpoint I와의 관계

이 문서는 **Unit 22 빌드·업로드 세팅**까지입니다.

- 실제 AdMob / Firebase / Crashlytics 배선 → **Unit 21 / Checkpoint I** 잔여
- 내부 테스트는 Mock/NoOp로도 플레이 검증 가능
- 실광고를 켠 뒤에는 AdUnitConfig 운영 ID·데이터 안전 설정을 다시 맞추세요
