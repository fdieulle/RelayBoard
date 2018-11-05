using System;
using System.Diagnostics;

namespace RelayBoard.Core
{
    public class AnonymousDisposable : IDisposable
    {
        public static readonly IDisposable Empty = new AnonymousDisposable();
        private Action _onDispose;

        public AnonymousDisposable(Action onDispose = null)
            => _onDispose = onDispose;
        
        #region IDisposable

        [DebuggerStepThrough]
        public void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }

        #endregion
    }
}
