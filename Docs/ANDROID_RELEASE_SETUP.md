# Android Release 설정 (Unit 22)

Unity **6000.5.4f1** 기준. Google Play **내부 테스트**에 AAB를 올리기 위한 설정입니다.

## 한 번에 준비 (권장)

Unity 메뉴:

1. **Tools → 막차 생존 → Release → 서명·버전업 후 Release AAB 빌드**
2. Keystore 경로·비밀번호·Alias 입력
3. **Bundle Version Code +1** 확인 후 빌드

출력:

- `Builds/Android/LastTrain-v{version}-b{code}.aab`
- 동일 내용 복사본: `Builds/Android/LastTrain.aab`

업로드 절차는 [`PLAY_CONSOLE_INTERNAL_TEST.md`](PLAY_CONSOLE_INTERNAL_TEST.md)를 따르세요.

> 비밀번호는 에디터 세션에만 적용되며 프로젝트 파일에 저장되지 않습니다. Keystore 경로·Alias만 EditorPrefs에 기억합니다.

---

## 1. 중앙 설정

| 항목 | 위치 |
|------|------|
| 앱 표시 이름·패키지·버전 | `Assets/Data/Release/AppReleaseConfig.asset` |
| 런타임 로드 | `Assets/Resources/AppReleaseConfig.asset` (Setup이 동기화) |
| 광고 단위 ID | `Assets/Data/Integration/AdUnitConfig.asset` |

권장 초기값:

| 필드 | 값 |
|------|-----|
| Display Name | 막차 생존 |
| Package | `com.lasttrain.nineslotdefense` |
| Version Name | `0.1.0` |
| Bundle Version Code | `1` (업로드마다 +1) |

---

## 2. Player Settings (자동 동기화)

**Tools → 막차 생존 → Release → Sync Release Config to Player Settings**

또는 Setup / Prepare 메뉴가 동일 설정을 적용합니다.

| 설정 | 값 |
|------|-----|
| Scripting Backend | IL2CPP |
| Target Architectures | ARM64 only |
| Build App Bundle | ON |
| Split Application Binary | ON |
| Min SDK | API 26 |
| Target SDK | API 35 (Play Console 요구) |
| Orientation | Portrait only (가로 회전 OFF) |
| Splash Unity Logo | OFF |
| App Icon / Splash | Setup이 `app_icon_512` / `splash_portrait` 생성·할당 |

---

## 3. 서명키 (저장소 밖)

- Keystore / JKS는 **절대 커밋하지 않습니다** (`.gitignore`: `*.keystore`, `*.jks`).
- Unity: **Edit → Project Settings → Player → Android → Publishing Settings**
  - Custom Keystore = ON
  - Keystore path / Alias / Passwords 설정
- 프로젝트에 이미 `AndroidKeys/lasttrain-release` dedicated 경로 참조가 있을 수 있습니다.  
  로컬에 파일이 없으면 아래처럼 생성하세요.

### 키스토어 생성 예시 (로컬)

```bash
keytool -genkey -v -keystore "%USERPROFILE%\AndroidKeys\lasttrain-release.keystore" ^
  -alias lasttrain-release -keyalg RSA -keysize 2048 -validity 10000
```

생성 후 Unity Publishing Settings에 동일 경로/alias를 연결합니다.

> Play App Signing을 쓰면 업로드 키와 앱 서명이 분리됩니다. 내부 테스트부터 Google Play App Signing 권장.

---

## 4. 빌드 검증

```
Tools → 막차 생존 → Release → Validate Release Build
```

검증 실패 시 **Release AAB 빌드가 중단**됩니다.

주요 ERROR:

- IL2CPP / ARM64 / AAB 미설정
- Build Settings에 Bootstrap·MainMenu·Game·Result 미포함
- 커스텀 키스토어 미설정
- Development Build가 ON인 채 Release 빌드
- 앱 아이콘 미할당

WARN (업로드는 가능, 보완 권장):

- 개인정보처리방침 URL이 `example.com` placeholder
- AdMob 운영 ID 대신 Google 테스트 ID

---

## 5. Development vs Release

| | Development AAB | Release AAB |
|--|-----------------|-------------|
| 메뉴 | Build … (Development) | Build … (Release) |
| `DEVELOPMENT_BUILD` | ON | OFF |
| Mock 광고 | Editor/Dev Mock | NoOp 또는 AdMob |
| 동의 UI | Editor/Dev 자동 동의 가능 | 최초 실행 다이얼로그 |
| Debug Panel | Editor 메뉴만 | 플레이어에 없음 |

---

## 6. QA

- [`RELEASE_QA_CHECKLIST.md`](RELEASE_QA_CHECKLIST.md)
- [`PLAY_CONSOLE_INTERNAL_TEST.md`](PLAY_CONSOLE_INTERNAL_TEST.md)
