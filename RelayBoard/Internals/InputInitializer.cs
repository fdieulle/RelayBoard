using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RelayBoard.Core;

namespace RelayBoard.Internals
{
    public unsafe class InputInitializer : IInputLinks, IDisposable
    {
        private readonly ArrayEx<Action<DateTime>[]> _queue;
        private readonly Action<InputInitializer> _onDispose;
        private readonly Dictionary<string, OutputInitializer> _outputs = new Dictionary<string, OutputInitializer>();
        private readonly List<Action<DateTime>> _callbacks = new List<Action<DateTime>>();
        private readonly BitArray _mask = new BitArray(0);

        private PulseSource* _pulseSource;

        private bool IsMaskBuilt => _mask.Length > 0;

        public string Key { get; }

        public int MaskLength { get; private set; }

        public int FlagsOffset { get; private set; }

        public InputInitializer(string key, IRelayInput input, ArrayEx<Action<DateTime>[]> queue, Action<InputInitializer> onDispose)
        {
            Key = key;
            Input = input;
            _queue = queue;
            _onDispose = onDispose;
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

        public void AddOutput(OutputInitializer output)
        {
            if (!_outputs.ContainsKey(output.Key))
                _outputs.Add(output.Key, output);
        }

        public void RemoveOutput(OutputInitializer output)
        {
            _outputs.Remove(output.Key);

            if (IsMaskBuilt) // Todo: Do we need to keep this code in case of initlalize is call for each new connectons
            {
                var mask = new BitArray(_mask.Length, true);
                mask.Set(output.Index, false);
                _mask.And(mask);
                _pulseSource->UpdateMask(_mask);
            }

            if (_outputs.Count == 0)
                Dispose();
        }

        public void PrepareIndices()
        {
            MaskLength = 0;
            FlagsOffset = 0;
            if (_outputs.Count == 0) return;

            var min = int.MaxValue;
            var max = int.MinValue;
            foreach (var receiver in _outputs.Values)
            {
                min = Math.Min(receiver.Index, min);
                max = Math.Max(receiver.Index, max);
            }

            var minInByte = min / Tools.NB_BITS_PER_BYTE;
            var maxInByte = max / Tools.NB_BITS_PER_BYTE + 1;

            FlagsOffset = minInByte;
            MaskLength = Math.Max(0, maxInByte - minInByte);

            var floorInBits = minInByte * Tools.NB_BITS_PER_BYTE;
            // Prepare producer mask
            _mask.Length = Math.Max(0, MaskLength * Tools.NB_BITS_PER_BYTE);
            _mask.SetAll(false);
            foreach (var dependency in _outputs.Values)
                _mask.Set(dependency.Index - floorInBits, true);
        }

        public void Initialize(PulseSource* producer)
        {
            producer->UpdateMask(_mask);

            // Keep producer pointer here to notify when the input trigger
            _pulseSource = producer;

            // Build links and callbacks
            Outputs = _outputs.Select(p => p.Value.Output).ToArray();

            IsInitialized = true;
        }

        public InputRuntime CreateRuntime()
        {
            var compactedCallbacks = _callbacks
                .Distinct()
                .OrderBy(p => p.Target)
                .ToArray();
            
            return new InputRuntime(_pulseSource, Input, compactedCallbacks, _queue);
        }

        #region Implementation of IInputLinks

        public bool IsInitialized { get; private set; }
        public IRelayInput Input { get; }
        public IRelayOutput[] Outputs { get; private set; }
        public bool HasCallbacks => _callbacks.Count > 0;

        #endregion

        public void Dispose()
        {
            _mask.Length = 0;
            _outputs.Clear();
            _callbacks.Clear();
            _queue.Reset();
            Outputs = null;
            IsInitialized = false;
            _onDispose(this);
        }
    }
}