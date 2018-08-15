using System;
using System.Runtime.InteropServices;

namespace RelayBoard.Internals
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PulseMetrics
    {
        // Todo: This structure has a big memory overhead and has a real cost when an output wants to retreive.
        // Todo: Memory overhead = 8 * nbInputs * nbOutputs
        // Todo: GetLastTick cost: O(m * n) where m = number of inputs where nad n = number of outputs
        // Todo: Proposal 1:  using bit/mask to locate PulseSource memory offset
        // Todo: Thinking out of the box:
        // Todo:     Give an memory area to IRelayInput where it can store timestamp.
        // Todo:     Remove this structure and access to the memory only with the PulseProbe offset.
        // Todo:     More generic let IRelayInput to request a memory area to store al that it want.
        private readonly PulseSource** _pulseSources;
        private int _length;

        public PulseMetrics(PulseSource** pulseSources)
        {
            _pulseSources = pulseSources;
            _length = 0;
        }

        public long GetLastTicks()
        {
            var max = DateTime.MinValue.Ticks;
            for (var i = 0; i < _length; i++)
                max = Math.Max(max, (*(_pulseSources + i))->LastTicks);
            return max;
        }

        public void AddSender(PulseSource* pulseSource)
        {
            *(_pulseSources + _length) = pulseSource;
            _length++;
        }
    }
}