using System;
using RelayBoard.Core;

namespace RelayBoard.Internals
{
    public unsafe class InputRuntime
    {
        private readonly PulseSource* _pulseSource;
        private readonly Action<DateTime>[] _callbacks;
        private readonly ArrayEx<Action<DateTime>[]> _callbackQueue;
        private readonly IDisposable _subscription;

        private int _insertIdx;

        public InputRuntime(
            PulseSource* pulseSource,
            IRelayInput input,
            Action<DateTime>[] callbacks,
            ArrayEx<Action<DateTime>[]> callbackQueue)
        {
            _callbacks = callbacks;
            _callbackQueue = callbackQueue;
            _pulseSource = pulseSource;
            _subscription = _callbacks.Length > 0
                ? input.Subscribe(PulseWithCallbacks)
                : input.Subscribe(Pulse);
        }

        private void Pulse()
        {
            _pulseSource->Pulse();
        }

        private void PulseWithCallbacks()
        {
            _pulseSource->Pulse();
            if (_insertIdx >= _callbackQueue.Length || _callbackQueue[_insertIdx] != _callbacks)
            {
                _insertIdx = _callbackQueue.Length;
                _callbackQueue.Add(_callbacks);
            }
        }

        #region IDisposable

        public void Dispose()
        {
            _subscription.Dispose();
        }

        #endregion
    }
}