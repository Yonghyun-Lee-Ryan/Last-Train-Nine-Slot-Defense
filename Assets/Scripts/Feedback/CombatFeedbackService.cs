using System.Collections;
using LastTrain.Audio;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Release;
using LastTrain.Synergy;
using LastTrain.UI;
using LastTrain.Ux;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Feedback
{
    /// <summary>
    /// 전투 피드백 오케스트레이터. 연출은 전투 로직을 대기하지 않으며,
    /// 이펙트/오디오가 없어도 Null Reference 없이 동작한다.
    /// </summary>
    public sealed class CombatFeedbackService : MonoBehaviour
    {
        [SerializeField] private EffectPool effectPool;
        [SerializeField] private FloatingTextPool floatingTextPool;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private GameBattleBootstrap battleBootstrap;
        [SerializeField] private RectTransform bannerRoot;
        [SerializeField] private Text bannerLabel;

        private SynergyManager _synergyManager;
        private StationManager _stationManager;
        private bool _bound;
        private Coroutine _bannerRoutine;

        public EffectPool Effects => effectPool;
        public FloatingTextPool FloatingTexts => floatingTextPool;

        public void Configure(
            EffectPool effects,
            FloatingTextPool floatingTexts,
            GridManager grid,
            BattleManager battle,
            GameBattleBootstrap bootstrap)
        {
            effectPool = effects;
            floatingTextPool = floatingTexts;
            gridManager = grid;
            battleManager = battle;
            battleBootstrap = bootstrap;
        }

        public void Bind()
        {
            if (_bound)
            {
                Unbind();
            }

            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<GridManager>();
            }

            if (battleManager == null)
            {
                battleManager = FindAnyObjectByType<BattleManager>();
            }

            if (battleBootstrap == null)
            {
                battleBootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            }

            if (battleBootstrap != null)
            {
                _synergyManager = battleBootstrap.SynergyManager;
                _stationManager = battleBootstrap.StationManager;
            }

            if (gridManager != null)
            {
                gridManager.MergeCompleted += HandleMergeCompleted;
            }

            if (battleManager != null)
            {
                battleManager.BossSpawned += HandleBossSpawned;
                battleManager.BossPhaseChanged += HandleBossPhaseChanged;
            }

            if (_synergyManager != null)
            {
                _synergyManager.SynergyActivated += HandleSynergyActivated;
            }

            if (_stationManager != null)
            {
                _stationManager.StationCompleted += HandleStationCompleted;
                _stationManager.RunVictoryRequested += HandleRunVictory;
            }

            CombatVisualEvents.EnemyDamaged += HandleEnemyDamaged;
            CombatVisualEvents.EnemyKilled += HandleEnemyKilled;
            CombatVisualEvents.PassengerAttacked += HandlePassengerAttacked;
            CombatVisualEvents.TrainHealed += HandleTrainHealed;
            CombatVisualEvents.AreaAttack += HandleAreaAttack;
            CombatVisualEvents.KnockbackApplied += HandleKnockback;
            CombatVisualEvents.TrainDamaged += HandleTrainDamaged;

            _bound = true;
        }

        public void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            if (gridManager != null)
            {
                gridManager.MergeCompleted -= HandleMergeCompleted;
            }

            if (battleManager != null)
            {
                battleManager.BossSpawned -= HandleBossSpawned;
                battleManager.BossPhaseChanged -= HandleBossPhaseChanged;
            }

            if (_synergyManager != null)
            {
                _synergyManager.SynergyActivated -= HandleSynergyActivated;
            }

            if (_stationManager != null)
            {
                _stationManager.StationCompleted -= HandleStationCompleted;
                _stationManager.RunVictoryRequested -= HandleRunVictory;
            }

            CombatVisualEvents.EnemyDamaged -= HandleEnemyDamaged;
            CombatVisualEvents.EnemyKilled -= HandleEnemyKilled;
            CombatVisualEvents.PassengerAttacked -= HandlePassengerAttacked;
            CombatVisualEvents.TrainHealed -= HandleTrainHealed;
            CombatVisualEvents.AreaAttack -= HandleAreaAttack;
            CombatVisualEvents.KnockbackApplied -= HandleKnockback;
            CombatVisualEvents.TrainDamaged -= HandleTrainDamaged;

            _bound = false;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void HandleEnemyDamaged(EnemyRuntime enemy, float damage, bool isCrit)
        {
            if (enemy == null)
            {
                return;
            }

            effectPool?.Play(isCrit ? "vfx_crit" : "vfx_hit", ToWorldFromCombatLocal(enemy.Position));

            if (damage > 0.5f)
            {
                Color color = isCrit
                    ? new Color(1f, 0.55f, 0.15f)
                    : new Color(1f, 0.92f, 0.85f);
                string text = isCrit
                    ? $"CRIT {Mathf.RoundToInt(damage)}"
                    : $"-{Mathf.RoundToInt(damage)}";
                SpawnDamageNumber(text, color, ToWorldFromCombatLocal(enemy.Position));
            }

            if (isCrit)
            {
                CameraShakeService.Shake(0.05f, 3f);
            }
        }

        private void HandleEnemyKilled(EnemyRuntime enemy)
        {
            if (enemy == null)
            {
                return;
            }

            effectPool?.Play("vfx_death", ToWorldFromCombatLocal(enemy.Position));
        }

        private void HandlePassengerAttacked(string passengerInstanceId)
        {
            if (gridManager == null)
            {
                return;
            }

            PassengerView view = gridManager.FindViewByInstanceId(passengerInstanceId);
            view?.PlayAttackAnimation();
        }

        private void HandleMergeCompleted(MergeResult result)
        {
            if (gridManager == null)
            {
                return;
            }

            GridSlot slot = gridManager.GetSlot(result.TargetSlot);
            Vector2 pos = slot?.ContentAnchor != null
                ? (Vector2)slot.ContentAnchor.position
                : Vector2.zero;

            effectPool?.Play("vfx_merge", pos);
            PassengerView mergedView = gridManager.FindViewByInstanceId(result.ResultInstanceId);
            mergedView?.PlayMergeAnimation();
            Vector2 starPos = pos + Vector2.up * 70f;
            floatingTextPool?.SpawnWorld(
                $"{result.ResultingStarLevel}★",
                new Color(1f, 0.92f, 0.35f),
                starPos,
                null,
                gridManager.RootCanvas,
                FloatingTextKind.Status);

            FlashSlot(slot);
        }

        private void HandleTrainHealed(Vector2 combatLocalPosition)
        {
            effectPool?.Play("vfx_heal", ToWorldFromCombatLocal(combatLocalPosition));
        }

        private void HandleAreaAttack(Vector2 combatLocalPosition)
        {
            effectPool?.Play("vfx_aoe", ToWorldFromCombatLocal(combatLocalPosition));
        }

        private void HandleKnockback(Vector2 combatLocalPosition)
        {
            effectPool?.Play("vfx_knockback", ToWorldFromCombatLocal(combatLocalPosition));
        }

        private void HandleTrainDamaged(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            Vector2 trainPos = ResolveTrainWorldPosition();
            effectPool?.Play("vfx_debuff_pulse", trainPos);
            CameraShakeService.Shake(0.22f, 28f);
            VibrationService.PlayLight(AppRoot.Instance?.GameSettings);
            floatingTextPool?.SpawnWorld(
                $"-{Mathf.RoundToInt(damage)}",
                new Color(1f, 0.35f, 0.35f),
                trainPos + Vector2.up * 36f,
                null,
                gridManager != null ? gridManager.RootCanvas : null,
                FloatingTextKind.Damage);
        }

        private Vector2 ResolveTrainWorldPosition()
        {
            if (battleManager == null)
            {
                battleManager = FindAnyObjectByType<BattleManager>();
            }

            if (battleManager != null && battleManager.TrainTarget != null)
            {
                return battleManager.TrainTarget.position;
            }

            return Vector2.zero;
        }

        private Vector2 ToWorldFromCombatLocal(Vector2 combatLocal)
        {
            RectTransform space = null;
            if (gridManager != null)
            {
                space = gridManager.transform.parent as RectTransform;
            }

            if (space == null && battleManager != null)
            {
                space = battleManager.transform.parent as RectTransform;
            }

            return BattleCombatSpace.LocalToWorld(space, combatLocal);
        }

        private void HandleBossSpawned(EnemyRuntime boss)
        {
            Vector2 pos = boss != null ? ToWorldFromCombatLocal(boss.Position) : Vector2.zero;
            effectPool?.Play("vfx_boss_portal", pos);
            CameraShakeService.Shake(0.2f, 14f);
            ShowBanner(boss != null && boss.Data != null ? $"BOSS — {boss.Data.DisplayName}" : "BOSS");
        }

        private void HandleBossPhaseChanged(BossPhase previous, BossPhase next)
        {
            if (previous == next)
            {
                return;
            }

            EnemyRuntime boss = battleManager != null ? battleManager.ActiveBoss : null;
            Vector2 pos = boss != null ? ToWorldFromCombatLocal(boss.Position) : Vector2.zero;
            effectPool?.Play("vfx_boss_enrage", pos);
            CameraShakeService.Shake(0.15f, 12f);
            ShowBanner($"PHASE {next}");
        }

        private void HandleSynergyActivated(SynergyData synergy)
        {
            if (synergy == null)
            {
                return;
            }

            effectPool?.Play("vfx_summon", Vector2.zero);
            ShowBanner($"시너지! {synergy.DisplayName}");
            AudioService.PlaySfx(SfxId.Reward);
        }

        private void HandleStationCompleted(StationData station)
        {
            ShowBanner(station != null ? $"{station.DisplayName} 도착" : "역 도착");
        }

        private void HandleRunVictory()
        {
            ShowBanner("종착역 도착!");
            CameraShakeService.Shake(0.18f, 8f);
        }

        private void SpawnDamageNumber(string message, Color color, Vector2 worldPosition)
        {
            if (floatingTextPool == null)
            {
                return;
            }

            Canvas canvas = gridManager != null ? gridManager.RootCanvas : null;
            floatingTextPool.SpawnWorld(
                message,
                color,
                worldPosition,
                Camera.main,
                canvas,
                FloatingTextKind.Damage);
        }

        private void FlashSlot(GridSlot slot)
        {
            if (slot?.ContentAnchor == null)
            {
                return;
            }

            StartCoroutine(FlashRoutine(slot.ContentAnchor));
        }

        private static IEnumerator FlashRoutine(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 original = target.localScale;
            target.localScale = original * 1.25f;
            yield return new WaitForSecondsRealtime(0.08f);
            if (target != null)
            {
                target.localScale = original;
            }
        }

        private void ShowBanner(string message)
        {
            EnsureBanner();
            if (bannerLabel == null)
            {
                return;
            }

            bannerLabel.text = message ?? string.Empty;
            if (_bannerRoutine != null)
            {
                StopCoroutine(_bannerRoutine);
            }

            _bannerRoutine = StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            if (bannerRoot != null)
            {
                bannerRoot.gameObject.SetActive(true);
            }

            float elapsed = 0f;
            const float duration = 1.4f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (bannerLabel != null)
                {
                    Color c = bannerLabel.color;
                    c.a = Mathf.Clamp01(1.2f - elapsed / duration);
                    bannerLabel.color = c;
                }

                yield return null;
            }

            if (bannerRoot != null)
            {
                bannerRoot.gameObject.SetActive(false);
            }

            _bannerRoutine = null;
        }

        private void EnsureBanner()
        {
            if (bannerLabel != null)
            {
                return;
            }

            Canvas canvas = gridManager != null ? gridManager.RootCanvas : FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var root = new GameObject("FeedbackBanner", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.72f);
            rect.anchorMax = new Vector2(0.5f, 0.72f);
            rect.sizeDelta = new Vector2(720f, 80f);
            bannerRoot = rect;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            bannerLabel = labelGo.GetComponent<Text>();
            bannerLabel.alignment = TextAnchor.MiddleCenter;
            bannerLabel.fontSize = 36;
            bannerLabel.color = Color.white;
            bannerLabel.raycastTarget = false;
            if (bannerLabel.font == null)
            {
                bannerLabel.font = LastTrain.UI.GameFontProvider.Get();
            }

            root.SetActive(false);
        }
    }
}
