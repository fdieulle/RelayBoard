using System;

namespace RelayBoard.Core
{
    public class AnonymousDisposable : IDisposable
    {
        public static readonly IDisposable Empty = new AnonymousDisposable();
        private Action _onDispose;

        public AnonymousDisposable(Action onDispose = null)
        {
            _onDispose = onDispose;
        }

        #region IDisposable

        public void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }

        #endregion
    }
}
