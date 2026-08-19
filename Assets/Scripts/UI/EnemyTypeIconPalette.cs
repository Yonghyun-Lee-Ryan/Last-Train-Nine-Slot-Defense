using LastTrain.Data;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>적 유형 색·실루엣과 HUD에 쓰는 짧은 이름.</summary>
    public static class EnemyTypeIconPalette
    {
        public static string DisplayNameFor(EnemyType type)
        {
            return type switch
            {
                EnemyType.Fast => "빠름",
                EnemyType.Tank => "탱커",
                EnemyType.Split => "분열",
                EnemyType.Elite => "정예",
                EnemyType.Boss => "보스",
                _ => "일반",
            };
        }

        public static Color ColorFor(EnemyType type)
        {
            return type switch
            {
                EnemyType.Fast => new Color(0.28f, 0.86f, 1f, 1f),
                EnemyType.Tank => new Color(1f, 0.55f, 0.18f, 1f),
                EnemyType.Split => new Color(0.42f, 0.9f, 0.38f, 1f),
                EnemyType.Elite => new Color(0.78f, 0.38f, 1f, 1f),
                EnemyType.Boss => new Color(0.95f, 0.22f, 0.28f, 1f),
                _ => new Color(0.82f, 0.84f, 0.88f, 1f),
            };
        }

        public static Sprite SpriteFor(EnemyType type)
        {
            return type switch
            {
                EnemyType.Fast => UiProceduralSprites.Chevron(),
                EnemyType.Tank => UiProceduralSprites.RoundedSquare(),
                EnemyType.Split => UiProceduralSprites.SplitDuo(),
                EnemyType.Elite => UiProceduralSprites.Diamond(),
                EnemyType.Boss => UiProceduralSprites.FilledCircle(),
                _ => UiProceduralSprites.FilledCircle(),
            };
        }

        public static Color PhaseColor(Enemy.BossPhase phase)
        {
            return phase switch
            {
                Enemy.BossPhase.DoorOpen => new Color(1f, 0.78f, 0.22f, 1f),
                Enemy.BossPhase.Enraged => new Color(0.95f, 0.22f, 0.28f, 1f),
                _ => new Color(0.28f, 0.78f, 0.55f, 1f),
            };
        }
    }
}
