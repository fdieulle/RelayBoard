using System;
using System.Collections.Generic;
using System.Linq;
using RelayBoard.Core;

namespace RelayBoard.Internals
{
    public unsafe class OutputInitializer : IOutputLinks, IDisposable
    {
        private readonly Action<OutputInitializer> _onDisposed;
        private readonly List<Action<DateTime>> _callbacks = new List<Action<DateTime>>();
        private readonly Dictionary<string, IRelayInput> _inputs = new Dictionary<string, IRelayInput>();

        public string Key { get; }
        
        public int Index { get; set; }

        public IEnumerable<Action<DateTime>> Callbacks => _callbacks;

        public OutputInitializer(string key, IRelayOutput output, Action<OutputInitializer> onDisposed)
        {
            Key = key;
            Output = output;
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

        public void AddInput(InputInitializer input)
        {
            if (_inputs.ContainsKey(input.Key)) return;

            _inputs.Add(input.Key, input.Input);
        }

        public void RemoveInput(InputInitializer input)
        {
            _inputs.Remove(input.Key);

            if (_inputs.Count == 0)
                Dispose();
        }

        public void Initialize(PulseProbe* state)
        {
            Output.Inject(state);
            Inputs = _inputs.Values.ToArray();
            IsInitialized = true;
        }

        public void Dispose()
        {
            _callbacks.Clear();
            _inputs.Clear();
            Inputs = null;
            IsInitialized = false;
            _onDisposed(this);
        }

        #region Implementation of IOutputLinks

        public bool IsInitialized { get; private set; }
        public IRelayOutput Output { get; }
        public IRelayInput[] Inputs { get; private set; }

        #endregion
    }
}