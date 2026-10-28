using System;

namespace DungeonEscape.Core
{
    /// <summary>
    /// Genel amacli durum makinesi. Fare, muhafiz ve oyun akisi ayni yapiyi kullanir.
    /// Gecisler yalnizca ChangeState ile yapilir; boylece her gecis tek noktadan izlenebilir.
    /// </summary>
    public class StateMachine
    {
        public IState Current { get; private set; }

        /// <summary>(onceki, yeni) durum. Debug ve test kayitlari icin.</summary>
        public event Action<IState, IState> StateChanged;

        public void ChangeState(IState next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (ReferenceEquals(next, Current)) return;

            var previous = Current;
            previous?.Exit();
            Current = next;
            Current.Enter();
            StateChanged?.Invoke(previous, Current);
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
