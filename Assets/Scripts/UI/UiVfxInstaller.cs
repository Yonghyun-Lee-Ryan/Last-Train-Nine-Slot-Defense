using LastTrain.Audio;
using LastTrain.Battle;
using LastTrain.Feedback;
using LastTrain.Grid;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>Game Scene에 Effect/FloatingText/CombatFeedback를 보장한다.</summary>
    public sealed class UiVfxInstaller : MonoBehaviour
    {
        [SerializeField] private UiVfxPool vfxPool;
        [SerializeField] private EffectPool effectPool;
        [SerializeField] private FloatingTextPool floatingTextPool;
        [SerializeField] private CombatFeedbackService feedbackService;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private GridManager gridManager;

        private bool _installed;

        public static CombatFeedbackService InstallIfMissing(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            UiVfxInstaller existing = canvas.GetComponentInChildren<UiVfxInstaller>(true);
            if (existing != null)
            {
                existing.Install();
                return existing.feedbackService;
            }

            var root = new GameObject(
                "UiVfxRoot",
                typeof(RectTransform),
                typeof(UiVfxPool),
                typeof(EffectPool),
                typeof(FloatingTextPool),
                typeof(CombatFeedbackService),
                typeof(UiVfxInstaller));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UiVfxInstaller installer = root.GetComponent<UiVfxInstaller>();
            installer.vfxPool = root.GetComponent<UiVfxPool>();
            installer.effectPool = root.GetComponent<EffectPool>();
            installer.floatingTextPool = root.GetComponent<FloatingTextPool>();
            installer.feedbackService = root.GetComponent<CombatFeedbackService>();
            installer.Install();
            return installer.feedbackService;
        }

        public CombatFeedbackService Feedback => feedbackService;
        public FloatingTextPool FloatingTexts => floatingTextPool;
        public EffectPool Effects => effectPool;

        private void Start()
        {
            Install();
        }

        private void OnDestroy()
        {
            feedbackService?.Unbind();
        }

        private void Install()
        {
            if (_installed)
            {
                RebindFeedback();
                return;
            }

            if (vfxPool == null)
            {
                vfxPool = GetComponent<UiVfxPool>();
            }

            if (effectPool == null)
            {
                effectPool = GetComponent<EffectPool>();
                if (effectPool == null)
                {
                    effectPool = gameObject.AddComponent<EffectPool>();
                }
            }

            if (floatingTextPool == null)
            {
                floatingTextPool = GetComponent<FloatingTextPool>();
                if (floatingTextPool == null)
                {
                    floatingTextPool = gameObject.AddComponent<FloatingTextPool>();
                }
            }

            if (feedbackService == null)
            {
                feedbackService = GetComponent<CombatFeedbackService>();
                if (feedbackService == null)
                {
                    feedbackService = gameObject.AddComponent<CombatFeedbackService>();
                }
            }

            if (vfxPool != null)
            {
                vfxPool.Initialize();
            }

            effectPool?.Initialize(vfxPool);
            floatingTextPool?.Initialize(null, transform as RectTransform);

            if (battleManager == null)
            {
                battleManager = FindAnyObjectByType<BattleManager>();
            }

            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<GridManager>();
            }

            GameBattleBootstrap bootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            feedbackService?.Configure(effectPool, floatingTextPool, gridManager, battleManager, bootstrap);
            RebindFeedback();
            AudioService.EnsureInitialized();
            _installed = true;
        }

        public void RebindFeedback()
        {
            GameBattleBootstrap bootstrap = battleBootstrapOrFind();
            feedbackService?.Configure(effectPool, floatingTextPool, gridManager, battleManager, bootstrap);
            feedbackService?.Bind();
        }

        private GameBattleBootstrap battleBootstrapOrFind()
        {
            return FindAnyObjectByType<GameBattleBootstrap>();
        }
    }
}
