using System.Text;
using UnityEngine;

namespace LastTrain.Performance
{
    /// <summary>
    /// Unit 32: Update 사용 MonoBehaviour 목록과 Profiler 점검 항목.
    /// Editor 메뉴 또는 테스트에서 문자열로 확인한다.
    /// </summary>
    public static class PerformanceChecklist
    {
        public static readonly string[] UpdateMonoBehaviours =
        {
            "BattleManager.Update — 전투 틱 허브 (유지)",
            "ProjectileController.Update — 활성 발사체 이동 (풀 재사용)",
            "GameBattleBootstrap.Update — StationManager.Tick",
            "PassengerView.Update — 스케일 보간 (합성 연출)",
            "UiSpriteAnimator.Update — VFX/애니 프레임",
            "SafeAreaFitter.Update — 화면 크기 변경 감지",
            "NonCombatPanelController.Update — 상점/이벤트 UI 폴링",
            "BattleHudController — CameraShake SafeArea 바인딩",
            "ScreenShakeDriver.LateUpdate — CameraShake Tick",
        };

        public static readonly string[] ProfilerMarkers =
        {
            "CPU Usage: Scripts — BattleManager / Passenger Tick",
            "GC Alloc — BattleManager 전투 중 프레임당 할당 (목표: 안정화)",
            "Memory: Texture / AudioClip — Scene 전환 후 잔류",
            "Rendering: Batches — Sprite Atlas / UI Canvas rebuild",
            "Physics — 사용 최소화 (2D UI 전투)",
        };

        public static readonly string[] TestProcedure =
        {
            "1. Development Build + Autoconnect Profiler로 Android/Editor 실행",
            "2. 일반 전투 웨이브에서 Deep Profile 없이 GC Alloc 곡선 확인",
            "3. 적 30+ 동시 출현 구간에서 프레임 유지 여부 확인",
            "4. 무한 모드 50역 EditMode 테스트(EndlessPerformanceTests) 통과",
            "5. 저장 중 강제 종료 후 .bak 복원 시나리오(SaveStabilityTests) 통과",
            "6. LowFx / 피해숫자 OFF 시 FloatingText·VFX 할당 감소 확인",
            "7. Unit 54: 저사양 60 FPS 목표, LowEndFramePolicy 권고, SoftLaunchBalanceGate 통과",
        };

        public static string BuildReport()
        {
            var sb = new StringBuilder(1024);
            sb.AppendLine("=== Unit 32 Performance Checklist ===");
            sb.AppendLine("-- Update MonoBehaviours --");
            for (int i = 0; i < UpdateMonoBehaviours.Length; i++)
            {
                sb.AppendLine(UpdateMonoBehaviours[i]);
            }

            sb.AppendLine("-- Profiler Markers --");
            for (int i = 0; i < ProfilerMarkers.Length; i++)
            {
                sb.AppendLine(ProfilerMarkers[i]);
            }

            sb.AppendLine("-- Test Procedure --");
            for (int i = 0; i < TestProcedure.Length; i++)
            {
                sb.AppendLine(TestProcedure[i]);
            }

            return sb.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Warmup()
        {
            // no-op: 도메인 리로드 시 static 초기화 보장
        }
    }
}
