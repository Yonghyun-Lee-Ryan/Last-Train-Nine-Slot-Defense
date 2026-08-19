# Android Release QA Checklist (Unit 22)

Unity **6000.5.4f1** / 내부 테스트 빌드용 점검 목록입니다.

## 빌드 전 (Editor)

- [ ] `Tools → 막차 생존 → Release → Prepare For Play Internal Test` 실행
- [ ] 또는 개별: Setup Release Assets / Sync / Validate 통과
- [ ] `AppReleaseConfig` 버전명·Bundle Version Code 갱신
- [ ] 개인정보처리방침 URL이 실제 주소로 설정됨 (placeholder면 WARN)
- [ ] AdMob 운영 광고 ID 입력 (내부테스트는 테스트 ID로 가능, WARN)
- [ ] Player Settings → Android → Publishing Settings Keystore 로컬 경로 확인

## AAB 생성

- [ ] `Tools → 막차 생존 → Release → Build Android App Bundle (Release)`
- [ ] `Builds/Android/LastTrain.aab` (및 버전 파일명 AAB) 생성 확인
- [ ] Development Build OFF, Debug Panel은 Editor 전용만 존재

## Play Console 내부 테스트

- [ ] `Docs/PLAY_CONSOLE_INTERNAL_TEST.md` 절차로 AAB 업로드
- [ ] 테스터 초대 링크로 설치 확인

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

---

# Soft Launch (Unit 54)

Unity **6000.5.4f1**. 신규 승객 +4 · Quick Run · LiveOps 반영 후 실패율/성능 게이트.

## 밸런스 게이트

- [x] Headless 시나리오에 신규 승객(차장/바리스타/보안/학생) 포함
- [x] Headless 시나리오에 Quick Run 5역 노선 포함
- [x] `Tools → 막차 생존 → 개발 단위 54 Soft Launch QA 게이트` 통과
- [x] `BalanceReports/soft_launch_gate.md` 기록

## 저사양 프레임

- [x] `LowEndFramePolicy` 목표 60 FPS / 프레임 예산 17ms
- [x] 저메모리(<3GB)에서 LowFx 자동 권고 (사용자 설정이 없을 때)
- [x] 설정 패널의 저사양 이펙트 토글 유지
- [ ] 실기기 저사양 60 FPS 체감 (기기 전용 — Soft Launch 트랙에서 확인)

## 핵심 루프 회귀 (Release / SDK 미활성 NoOp)

- [x] EditMode SoftLaunchBalanceGate 시나리오 3종 완료
- [x] `ReleaseBuildValidator` ERROR 0 (AdMob/Firebase WARN 허용)
- [ ] 실기기 1회차 클리어 (Unit 55)

기록일: 2026-08-14. EditMode 게이트는 본 단위에서 통과. 실기기 프레임은 Unit 55 체크리스트에 남긴다.

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
