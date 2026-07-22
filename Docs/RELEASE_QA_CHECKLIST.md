# Android Release QA Checklist (Unit 22)

Unity **6000.5.4f1** / 내부 테스트 빌드용 점검 목록입니다.

## 빌드 전 (Editor)

- [ ] `Tools → 막차 생존 → Release → Setup Release Assets` 실행
- [ ] `Tools → 막차 생존 → Release → Sync Release Config to Player Settings` 실행
- [ ] `Tools → 막차 생존 → Release → Validate Release Build` 통과
- [ ] `AppReleaseConfig` 버전명·Bundle Version Code 갱신
- [ ] 개인정보처리방침 URL이 실제 주소로 설정됨
- [ ] AdMob 운영 광고 ID 입력 (Release)
- [ ] Player Settings → Android → Keystore가 **로컬 경로**에 설정됨 (저장소 미포함)

## AAB 생성

- [ ] `Tools → 막차 생존 → Release → Build Android App Bundle (Release)`
- [ ] `Builds/Android/LastTrain.aab` 생성 확인
- [ ] Development Build 메뉴 결과물에 Debug Panel 메뉴가 없음 (Editor 전용)

## 설치·실행

- [ ] 실제 Android 기기에 sideload 또는 Play 내부 테스트 트랙 설치
- [ ] 첫 실행 시 동의 다이얼로그 표시 (Release)
- [ ] 동의 거부 후에도 메인 메뉴·게임 시작 가능
- [ ] 오프라인(비행기 모드)에서 1회차 플레이 가능

## 게임플레이

- [ ] 새 게임 → 전투 → 결과 화면까지 진행
- [ ] 앱 강제 종료 후 **이어하기** 복구
- [ ] 광고 실패/미동의 시에도 리롤·부활 외 핵심 진행 가능
- [ ] 메타 보상·저장 정상

## 설정·법적

- [ ] 설정 → BGM/효과음/진동/알림 토글 저장
- [ ] 설정 → 개인정보처리방침 링크 열림
- [ ] 설정 → 앱 데이터 삭제 후 진행도 초기화
- [ ] 광고·분석 동의 토글 변경 후 재시작 없이 반영

## UI·기기

- [ ] 18:9 / 19.5:9 / 20:9 Safe Area (노치·펀치홀)
- [ ] 세로 고정, 가로 회전 없음
- [ ] 저사양 기기 60 FPS 목표 (발열 시 프레임 드랍 허용 범위 확인)

## Pause / Resume / 네트워크

- [ ] 홈 버튼 → 복귀 시 전투 상태 유지
- [ ] 통화·알림 오버레이 후 복귀
- [ ] Wi-Fi 끊김 중 플레이·저장
- [ ] Firebase/Remote Config 실패 시 로컬 기본값 사용

## Release 전용 제외 확인

- [ ] `DebugCombatSettings` 치트 비활성 (Release)
- [ ] `Tools/막차 생존/Debug/*` 메뉴는 Editor에만 존재
- [ ] MockAdPopup 미표시 (Release, 동의 없음 → NoOp)

## 스토어 제출 준비 (선택)

- [ ] 스크린샷 1080×1920 이상
- [ ] 짧은 설명 / 전체 설명
- [ ] 데이터 안전 섹션 (광고·분석·로컬 저장)
