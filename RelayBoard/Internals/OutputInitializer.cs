using System;
using System.Collections.Generic;
using RelayBoard.Core;

namespace RelayBoard.Internals
{
    public unsafe class OutputInitializer : IDisposable
    {
        private readonly IRelayOutput _output;
        private readonly Action<OutputInitializer> _onDisposed;
        private readonly List<Action<DateTime>> _callbacks = new List<Action<DateTime>>();
        private readonly HashSet<string> _inputs = new HashSet<string>();

        public string Key { get; }

        public int Index { get; set; }

        public IEnumerable<Action<DateTime>> Callbacks => _callbacks;

        public int PulseMetricsSize => sizeof(PulseMetrics) + _inputs.Count * sizeof(PulseSource*);

        public OutputInitializer(string key, IRelayOutput output, Action<OutputInitializer> onDisposed)
        {
            Key = key;
            _output = output;
            _onDisposed = onDisposed;
        }

        public IDisposable Subscribe(Action<DateTime> callback)
        {
            if (callback == null) return AnonymousDisposable.Empty;

            _callbacks.Add(callback);
            return new AnonymousDisposable(() =>
            {
                _callbacks.Remove(callback);
            });
        }

        public void AddInput(string name)
        {
            if (_inputs.Contains(name)) return;

            _inputs.Add(name);
        }

        public bool HasInput(string source)
        {
            return _inputs.Contains(source);
        }

        public void RemoveInput(string name)
        {
            _inputs.Remove(name);

            if (_inputs.Count == 0)
                Dispose();
        }

        public void Inject(PulseProbe* state)
        {
            _output.Inject(state);
        }

        public void Dispose()
        {
            _callbacks.Clear();
            _inputs.Clear();
            _onDisposed(this);
        }
    }
}