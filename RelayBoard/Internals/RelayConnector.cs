using System;
using System.Collections.Generic;
using RelayBoard.Core;

namespace RelayBoard.Internals
{
    public class RelayConnector : IRelayConnector
    {
        private readonly InputInitializer _input;
        private readonly OutputInitializer _output;
        private readonly LazyInitializer _lazy;
        private readonly Action<RelayConnector> _onDispose;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public string Key { get; }

        public RelayConnector(
            string key,
            InputInitializer input,
            OutputInitializer output,
            LazyInitializer lazy,
            Action<RelayConnector> onDispose)
        {
            Key = key;
            _input = input;
            _output = output;
            _lazy = lazy;
            _onDispose = onDispose;

            _output.AddInput(input);
            _input.AddOutput(output);

            _lazy.ForceInitialize();
        }

        public IDisposable Subscribe(Action<DateTime> callback)
        {
            using (_lazy.Initialize())
            {
                var subscription = _input.Subscribe(callback);
                _subscriptions.Add(subscription);

                return new AnonymousDisposable(() =>
                {
                    using (_lazy.Initialize())
                    {
                        subscription.Dispose();
                        _subscriptions.Remove(subscription);
                    }
                });
            }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            using (_lazy.Initialize())
            {
                _output.RemoveInput(_input);
                _input.RemoveOutput(_output);

                _subscriptions.ForEach(p => p.Dispose());
                _subscriptions.Clear();

                _onDispose(this);
            }
        }

        #endregion
    }
}