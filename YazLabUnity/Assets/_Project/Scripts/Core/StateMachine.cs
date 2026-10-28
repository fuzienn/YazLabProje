using System;

namespace DungeonEscape.Core
{
    /// <summary>
    /// Genel amacli durum makinesi. Fare, muhafiz ve oyun akisi ayni yapiyi kullanir.
    /// Gecisler yalnizca ChangeState ile yapilir; boylece her gecis tek noktadan izlenebilir.
    /// Bir durumun Enter/Exit'i icinden istenen gecis, mevcut gecis bitince uygulanir.
    /// </summary>
    public class StateMachine
    {
        private bool _transitioning;
        private IState _pending;

        public IState Current { get; private set; }

        /// <summary>(onceki, yeni) durum. Debug ve test kayitlari icin.</summary>
        public event Action<IState, IState> StateChanged;

        public void ChangeState(IState next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (_transitioning)
            {
                _pending = next;
                return;
            }

            _transitioning = true;
            try
            {
                while (next != null)
                {
                    _pending = null;
                    if (!ReferenceEquals(next, Current))
                    {
                        var previous = Current;
                        previous?.Exit();
                        Current = next;
                        Current.Enter();
                        StateChanged?.Invoke(previous, next);
                    }
                    next = _pending;
                }
            }
            finally
            {
                _transitioning = false;
            }
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
