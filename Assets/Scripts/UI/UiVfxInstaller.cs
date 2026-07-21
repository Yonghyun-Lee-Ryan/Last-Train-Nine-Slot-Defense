using LastTrain.Battle;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>Game Scene에 UiVfxPool을 보장하고 전투 이벤트에 VFX를 연결한다.</summary>
    public sealed class UiVfxInstaller : MonoBehaviour
    {
        [SerializeField] private UiVfxPool vfxPool;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private GridManager gridManager;

        private bool _installed;

        public static UiVfxPool InstallIfMissing(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            UiVfxInstaller existing = canvas.GetComponentInChildren<UiVfxInstaller>(true);
            if (existing != null && existing.vfxPool != null)
            {
                existing.Install();
                return existing.vfxPool;
            }

            var root = new GameObject("UiVfxRoot", typeof(RectTransform), typeof(UiVfxPool), typeof(UiVfxInstaller));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UiVfxPool pool = root.GetComponent<UiVfxPool>();
            UiVfxInstaller installer = root.GetComponent<UiVfxInstaller>();
            installer.vfxPool = pool;
            installer.Install();
            return pool;
        }

        private void Start()
        {
            Install();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Install()
        {
            if (_installed)
            {
                return;
            }

            if (vfxPool == null)
            {
                vfxPool = GetComponent<UiVfxPool>();
            }

            if (vfxPool != null)
            {
                vfxPool.Initialize();
            }

            if (battleManager == null)
            {
                battleManager = FindAnyObjectByType<BattleManager>();
            }

            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<GridManager>();
            }

            Subscribe();
            _installed = true;
        }

        private void Subscribe()
        {
            if (gridManager != null)
            {
                gridManager.MergeCompleted -= HandleMergeCompleted;
                gridManager.MergeCompleted += HandleMergeCompleted;
            }

            CombatVisualEvents.EnemyDamaged -= HandleEnemyDamaged;
            CombatVisualEvents.EnemyDamaged += HandleEnemyDamaged;
            CombatVisualEvents.EnemyKilled -= HandleEnemyKilled;
            CombatVisualEvents.EnemyKilled += HandleEnemyKilled;
            CombatVisualEvents.PassengerAttacked -= HandlePassengerAttacked;
            CombatVisualEvents.PassengerAttacked += HandlePassengerAttacked;
            CombatVisualEvents.TrainHealed -= HandleTrainHealed;
            CombatVisualEvents.TrainHealed += HandleTrainHealed;
        }

        private void Unsubscribe()
        {
            if (gridManager != null)
            {
                gridManager.MergeCompleted -= HandleMergeCompleted;
            }

            CombatVisualEvents.EnemyDamaged -= HandleEnemyDamaged;
            CombatVisualEvents.EnemyKilled -= HandleEnemyKilled;
            CombatVisualEvents.PassengerAttacked -= HandlePassengerAttacked;
            CombatVisualEvents.TrainHealed -= HandleTrainHealed;
        }

        private void HandleEnemyDamaged(EnemyRuntime enemy, float damage, bool isCrit)
        {
            if (vfxPool == null || enemy == null)
            {
                return;
            }

            vfxPool.Play(isCrit ? "vfx_crit" : "vfx_hit", enemy.Position);
        }

        private void HandleEnemyKilled(EnemyRuntime enemy)
        {
            if (vfxPool == null || enemy == null)
            {
                return;
            }

            vfxPool.Play("vfx_death", enemy.Position);
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
            if (vfxPool == null || gridManager == null)
            {
                return;
            }

            GridSlot slot = gridManager.GetSlot(result.TargetSlot);
            if (slot?.ContentAnchor != null)
            {
                vfxPool.Play("vfx_merge", slot.ContentAnchor.position);
            }
        }

        private void HandleTrainHealed(Vector2 worldPosition)
        {
            vfxPool?.Play("vfx_heal", worldPosition);
        }
    }
}
