# Android Release 설정 (Unit 22)

Unity **6000.5.4f1** 기준입니다.

## 1. 중앙 설정

| 항목 | 위치 |
|------|------|
| 앱 표시 이름·패키지·버전 | `Assets/Data/Release/AppReleaseConfig.asset` |
| 런타임 로드 | `Assets/Resources/AppReleaseConfig.asset` (Setup 메뉴가 복사) |

메뉴: **Tools → 막차 생존 → Release → Setup Release Assets**

## 2. Player Settings (자동 동기화)

**Tools → 막차 생존 → Release → Sync Release Config to Player Settings**

| 설정 | 값 |
|------|-----|
| Scripting Backend | IL2CPP |
| Target Architecture | ARM64 |
| Build App Bundle | ON |
| Min SDK | API 26 |

## 3. 서명키

- Keystore는 **저장소에 포함하지 않습니다** (`.gitignore`에 `*.keystore`, `*.jks` 등록)
- Unity: **Edit → Project Settings → Player → Android → Publishing Settings**
- Keystore 경로는 팀 공유 비밀 저장소 또는 CI 시크릿으로 관리

## 4. 빌드

```
Tools → 막차 생존 → Release → Validate Release Build
Tools → 막차 생존 → Release → Build Android App Bundle (Release)
```

출력: `Builds/Android/LastTrain.aab`

Release 빌드는 Validate 실패 시 **빌드가 중단**됩니다.

## 5. Development vs Release

| | Development AAB | Release AAB |
|--|-----------------|-------------|
| 메뉴 | Build Android App Bundle (Development) | Build Android App Bundle (Release) |
| `DEVELOPMENT_BUILD` | ON | OFF |
| Mock 광고 | Editor/Dev에서 Mock | NoOp 또는 AdMob |
| 동의 UI | Editor/Dev 자동 동의 | 최초 실행 다이얼로그 |
| Debug Panel | Editor 메뉴만 | 없음 |

## 6. QA

`Docs/RELEASE_QA_CHECKLIST.md` 참고
