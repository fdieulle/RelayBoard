using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RelayBoard.Core;
using RelayBoard.Internals;

namespace RelayBoard
{
    public unsafe class RelayBoard : IRelayBoard
    {
        private readonly Dictionary<string, InputInitializer> _inputInitializers = new Dictionary<string, InputInitializer>();
        private readonly Dictionary<string, OutputInitializer> _outputInitializers = new Dictionary<string, OutputInitializer>();
        private readonly Dictionary<string, RelayConnector> _connectors = new Dictionary<string, RelayConnector>();
        private readonly LazyInitializer _lazyInitializer;
        private readonly ArrayEx<Action<DateTime>[]> _queue = new ArrayEx<Action<DateTime>[]>();
        private readonly List<InputRuntime> _runtimes = new List<InputRuntime>();
        private readonly StringBuilder _reports = new StringBuilder();
        private bool _isIninitialized;
        private IntPtr _globalMemory;

        public RelayBoard()
        {
            _lazyInitializer = new LazyInitializer(LazyInitialize);
        }

        public IRelayConnector Connect(IRelayInput input, IRelayOutput output)
        {
            if (input == null || output == null) return null;

            var iKey = input.Name ?? string.Empty;
            var oKey = output.Name ?? string.Empty;

            if (!_inputInitializers.TryGetValue(iKey, out var inputInitializer))
                _inputInitializers.Add(iKey, inputInitializer = new InputInitializer(iKey, input, _queue, RemoveInputInitializer));

            if (!_outputInitializers.TryGetValue(oKey, out var outputInitializer))
                _outputInitializers.Add(oKey, outputInitializer = new OutputInitializer(oKey, output, RemoveOutputInitializer));

            var key = $"{iKey}-{oKey}";
            if (!_connectors.TryGetValue(key, out var connector))
                _connectors.Add(key, connector = new RelayConnector(key, inputInitializer, outputInitializer, _lazyInitializer, RemoveConnector));

            return connector;
        }

        private void RemoveInputInitializer(InputInitializer input)
        {
            _inputInitializers.Remove(input.Key);
        }

        private void RemoveOutputInitializer(OutputInitializer output)
        {
            _outputInitializers.Remove(output.Key);
        }

        private void RemoveConnector(RelayConnector connector)
        {
            _outputInitializers.Remove(connector.Key);
        }

        #region Initialize part

        public void Initialize()
        {
            if (_isIninitialized)
                DisposeRuntime();
            _isIninitialized = true;

            // Check limits
            if (_outputInitializers.Count > ushort.MaxValue * Tools.NB_BITS_PER_BYTE) // To increase this limit change ushort type of maskLength and index into PulseSource
                throw new OverflowException($"Max ouput capacity reached: Max={sizeof(ushort) * Tools.NB_BITS_PER_BYTE} NbOutputs={_outputInitializers.Count}");

            // First prepare indices
            PrepareIndices(_outputInitializers, _inputInitializers);

            // Compute the memory length.

            // 1. We use 1 bit to flag a subscribe
            var flagsSize = Ceiling(_outputInitializers.Count / Tools.NB_BITS_PER_BYTE, sizeof(uint));

            // 2. For suscribers
            // 2.1 state size
            var pulseProbeSize = sizeof(PulseProbe) * _outputInitializers.Count;

            // 3. For producer
            // 3.1 structure layout size
            var pulseSourceSize = sizeof(PulseSource) * _inputInitializers.Count;
            // 3.2 pImpulseMask size
            var pulseMasksSizes = _inputInitializers.Sum(p => p.Value.MaskLength);

            var totalSize = flagsSize + pulseProbeSize + pulseSourceSize + pulseMasksSizes;

            // Alocate the memory
            _globalMemory = Marshal.AllocHGlobal(totalSize);

            // Prepare memory
            var ptr = (byte*)_globalMemory;

            //var pFlags = ptr;
            //var pPulseProbe = (PulseProbe*)(ptr + coilSize);
            //var pPulseMetrics = (PulseMetrics*)(ptr + coilSize + galvanometersSize);
            //var pPulseSource = (PulseSource*)(ptr + coilSize + galvanometersSize + galvanometerStatesSize);
            //var pPulseMask = ptr + coilSize + galvanometersSize + galvanometerStatesSize + currentFlowsSize;

            var pPulseSource = (PulseSource*)ptr;
            var pFlags = ptr + pulseSourceSize;
            var pPulseMask = ptr + pulseSourceSize + flagsSize;
            var pPulseProbe = (PulseProbe*)(ptr + pulseSourceSize + flagsSize + pulseMasksSizes);

            InstallFlags(pFlags, flagsSize);
            InstallPulseProbs(pPulseProbe, pFlags, _outputInitializers);
            InstallPulseSources(pPulseSource, pFlags, pPulseMask, _inputInitializers);

            _runtimes.AddRange(_inputInitializers.Select(p => p.Value.CreateRuntime()));
            _queue.SetCapacity(_runtimes.Count);

            #region Build Report
            _reports.Clear();
            _reports.AppendLine("=== Initialize");
            _reports.AppendLine($"Flags         Size: {flagsSize / 1000.0} Ko");
            _reports.AppendLine($"PulseProbe    Size: {pulseProbeSize / 1000.0} Ko");
            _reports.AppendLine($"PulseSource   Size: {pulseSourceSize / 1000.0} Ko");
            _reports.AppendLine($"PulseMasks    Size: {pulseMasksSizes / 1000.0} Ko");
            _reports.AppendLine("--------------------------------");
            _reports.AppendLine($"Total         Size: {totalSize / 1000.0} Ko");
            _reports.AppendLine("===");
            #endregion
        }

        private static void PrepareIndices(
            Dictionary<string, OutputInitializer> outputs,
            Dictionary<string, InputInitializer> initializers)
        {
            var idx = 0;
            foreach (var handler in outputs.Values)
                handler.Index = idx++;

            foreach (var source in initializers.Values)
                source.PrepareIndices();
        }

        private static void InstallFlags(byte* pFlags, int length)
        {
            for (var i = 0; i < length; i++)
                *(pFlags + i) = 0;
        }

        private static void InstallPulseProbs(
            PulseProbe* pPulseProbe,
            byte* pFlags,
            Dictionary<string, OutputInitializer> outputs)
        {
            // Todo: I should order memory to optimize dependecies calls with a contiguous memory and avoid to seek on it.
            // Todo For that and to be generic manage UpToBottom calls or BottomToUp.
            
            // Todo For now it's a simple implementation
            var ppp = pPulseProbe;
            foreach (var output in outputs.Values)
            {
                var idx = output.Index;
                var offset = idx / Tools.NB_BITS_PER_BYTE;
                var p = pFlags + offset;
                var mask = 1 << (idx % Tools.NB_BITS_PER_BYTE);

                ppp->Initialize(p, mask);
                output.Initialize(ppp);
                ppp++;
            }

        }

        private static void InstallPulseSources(
            PulseSource* pPulseSource,
            byte* pFlags,
            byte* pImpulseMask,
            Dictionary<string, InputInitializer> initializers)
        {
            var p = pPulseSource;
            var maskOffset = 0;
            foreach (var pair in initializers)
            {
                var initializer = pair.Value;
                p->Intialize(
                    pFlags + initializer.FlagsOffset,
                    (int)(pImpulseMask - pFlags - initializer.FlagsOffset + maskOffset),
                    initializer.MaskLength);
                maskOffset += initializer.MaskLength;

                initializer.Initialize(p);

                p++;
            }
        }

        private void LazyInitialize()
        {
            if (_isIninitialized)
                Initialize();
        }

        public string Report()
        {
            return _reports.ToString();
        }

        #endregion

        public IInputLinks GetInputLinks(IRelayInput input)
        {
            if (input == null) return null;

            var iKey = input.Name ?? string.Empty;
            _inputInitializers.TryGetValue(iKey, out var initializer);
            return initializer;
        }

        public IOutputLinks GetOutputLinks(IRelayOutput output)
        {
            if (output == null) return null;

            var oKey = output.Name ?? string.Empty;
            _outputInitializers.TryGetValue(oKey, out var initializer);
            return initializer;
        }

        public bool HasCallback(IRelayInput input)
        {
            if (input == null) return false;

            var iKey = input.Name ?? string.Empty;
            return _inputInitializers.TryGetValue(input.Name, out var initializer) && initializer.HasCallbacks;
        }

        public void Poll(DateTime now)
        {
            if (_queue.Length <= 0) return;

            // Process enqueued callbacks
            for (var i = 0; i < _queue.Length; i++)
                for (var j = 0; j < _queue[i].Length; j++)
                    _queue[i][j](now);
            _queue.Reset();
        }

        private static int Ceiling(int value, int qo)
        {
            return value / qo * qo + qo;
        }

        #region IDisposable

        public void Dispose()
        {
            _inputInitializers.Values.ToList().ForEach(p => p.Dispose());
            _inputInitializers.Clear();
            _outputInitializers.Values.ToList().ForEach(p => p.Dispose());
            _outputInitializers.Clear();
            _connectors.Values.ToList().ForEach(p => p.Dispose());
            _connectors.Clear();

            DisposeRuntime();
        }

        private void DisposeRuntime()
        {
            _isIninitialized = false;
            _runtimes.ForEach(p => p.Dispose());
            _runtimes.Clear();

            if (_globalMemory != IntPtr.Zero)
                Marshal.FreeHGlobal(_globalMemory);
        }

        #endregion
    }
}