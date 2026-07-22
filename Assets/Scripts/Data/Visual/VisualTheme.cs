using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>공용 UI/환경 스프라이트와 색상 테마.</summary>
    [CreateAssetMenu(fileName = "VisualTheme", menuName = "Last Train/Visual Theme")]
    public class VisualTheme : ScriptableObject
    {
        [Header("Environment")]
        [SerializeField] private Sprite subwayBackground;
        [SerializeField] private Sprite mainMenuBackground;
        [SerializeField] private Sprite spawnLane;
        [SerializeField] private Sprite trainTarget;
        [SerializeField] private Sprite seatFrame;
        [SerializeField] private Sprite seatHighlight;

        [Header("UI Chrome")]
        [SerializeField] private Sprite panel;
        [SerializeField] private Sprite buttonNormal;
        [SerializeField] private Sprite buttonPressed;
        [SerializeField] private Sprite buttonDisabled;
        [SerializeField] private Sprite cardFrame;
        [SerializeField] private Sprite popupDim;
        [SerializeField] private Sprite hpBarFill;
        [SerializeField] private Sprite hpBarBackground;
        [SerializeField] private Sprite bossHpBarFill;

        [Header("Screen Art")]
        [SerializeField] private Sprite mainMenuTitle;
        [SerializeField] private Sprite resultVictoryBanner;
        [SerializeField] private Sprite resultDefeatBanner;

        [Header("Icons")]
        [SerializeField] private Sprite iconCoin;
        [SerializeField] private Sprite iconStation;
        [SerializeField] private Sprite iconWave;
        [SerializeField] private Sprite iconReady;
        [SerializeField] private Sprite iconSpeed;
        [SerializeField] private Sprite iconPause;
        [SerializeField] private Sprite iconSummon;
        [SerializeField] private Sprite iconSell;
        [SerializeField] private Sprite iconReroll;
        [SerializeField] private Sprite iconAd;
        [SerializeField] private Sprite iconAbility;
        [SerializeField] private Sprite iconSynergy;

        [Header("Star Overlays")]
        [SerializeField] private Sprite starFrame1;
        [SerializeField] private Sprite starFrame2;
        [SerializeField] private Sprite starFrame3;

        public Sprite SubwayBackground => subwayBackground;
        public Sprite MainMenuBackground => mainMenuBackground != null ? mainMenuBackground : subwayBackground;
        public Sprite SpawnLane => spawnLane;
        public Sprite TrainTarget => trainTarget;
        public Sprite SeatFrame => seatFrame;
        public Sprite SeatHighlight => seatHighlight;
        public Sprite Panel => panel;
        public Sprite ButtonNormal => buttonNormal;
        public Sprite ButtonPressed => buttonPressed;
        public Sprite ButtonDisabled => buttonDisabled;
        public Sprite CardFrame => cardFrame;
        public Sprite PopupDim => popupDim;
        public Sprite HpBarFill => hpBarFill;
        public Sprite HpBarBackground => hpBarBackground;
        public Sprite BossHpBarFill => bossHpBarFill;
        public Sprite MainMenuTitle => mainMenuTitle;
        public Sprite ResultVictoryBanner => resultVictoryBanner;
        public Sprite ResultDefeatBanner => resultDefeatBanner;
        public Sprite IconCoin => iconCoin;
        public Sprite IconStation => iconStation;
        public Sprite IconWave => iconWave;
        public Sprite IconReady => iconReady;
        public Sprite IconSpeed => iconSpeed;
        public Sprite IconPause => iconPause;
        public Sprite IconSummon => iconSummon;
        public Sprite IconSell => iconSell;
        public Sprite IconReroll => iconReroll;
        public Sprite IconAd => iconAd;
        public Sprite IconAbility => iconAbility;
        public Sprite IconSynergy => iconSynergy;

        public Sprite GetStarFrame(int starLevel)
        {
            return starLevel switch
            {
                3 => starFrame3 != null ? starFrame3 : starFrame1,
                2 => starFrame2 != null ? starFrame2 : starFrame1,
                _ => starFrame1
            };
        }
    }
}
