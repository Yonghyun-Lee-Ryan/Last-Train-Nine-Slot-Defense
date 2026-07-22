using System;

namespace LastTrain.Run
{
    /// <summary>객차 내구도 런타임 상태.</summary>
    public sealed class TrainState
    {
        public event Action<int, int> HpChanged;
        public event Action Destroyed;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsDestroyed => CurrentHp <= 0;

        public TrainState(int maxHp, int currentHp)
        {
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "maxHp는 0보다 커야 합니다.");
            }

            MaxHp = maxHp;
            CurrentHp = Math.Clamp(currentHp, 0, maxHp);
        }

        public void SetMaxHp(int maxHp, bool healToFull = false)
        {
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "maxHp는 0보다 커야 합니다.");
            }

            MaxHp = maxHp;
            if (healToFull)
            {
                CurrentHp = maxHp;
            }
            else
            {
                CurrentHp = Math.Min(CurrentHp, maxHp);
            }

            NotifyHpChanged();
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDestroyed)
            {
                return;
            }

            if (LastTrain.DebugTools.DebugCombatSettings.Invulnerable)
            {
                return;
            }

            int previous = CurrentHp;
            CurrentHp = Math.Max(0, CurrentHp - amount);
            NotifyHpChanged();

            if (previous > 0 && CurrentHp <= 0)
            {
                Destroyed?.Invoke();
            }
        }

        /// <summary>디버그/시뮬용 현재 체력 직접 설정.</summary>
        public void SetCurrentHp(int currentHp)
        {
            CurrentHp = Math.Clamp(currentHp, 0, MaxHp);
            NotifyHpChanged();
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
            NotifyHpChanged();
        }

        public void RestoreFull()
        {
            CurrentHp = MaxHp;
            NotifyHpChanged();
        }

        private void NotifyHpChanged()
        {
            HpChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}
