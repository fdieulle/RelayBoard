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

        private void Pulse(DateTime timestamp)
        {
            _pulseSource->Pulse(timestamp);
        }

        private void PulseWithCallbacks(DateTime timestamp)
        {
            _pulseSource->Pulse(timestamp);
            _callbackQueue.Add(_callbacks);
        }

        #region IDisposable

        public void Dispose()
        {
            _subscription.Dispose();
        }

        #endregion
    }
}