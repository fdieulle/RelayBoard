using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RelayBoard.Internals;

namespace RelayBoard
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PulseProbe
    {
        // Defines the pointer where flags are stored
        private readonly uint* _flags;
        // Defines the mask to localize the given ouput bit.
        private readonly uint _mask;
        // Defines the memory offset relative to the _flags pointer where metrics data are stored.
        private readonly uint _metricsOffset;

        /// <summary>
        /// Gets if the associated output has been pulsed.
        /// </summary>
        public bool IsFlaged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_mask & *_flags) == _mask;
        }
        /// <summary>
        /// Gets the last pulsed timestamp from all connected inputs.
        /// </summary>
        public DateTime LastTimestamp => new DateTime(Metrics->GetLastTicks());
        /// <summary>
        /// Gets the last pulsed timestamp int ticks from all connected inputs.
        /// </summary>
        public long LastTicks => Metrics->GetLastTicks();

        /// <summary>
        /// Gets the extended state data pointer
        /// </summary>
        private PulseMetrics* Metrics
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (PulseMetrics*)((byte*)_flags + _metricsOffset);
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="flags">The pointer wher flags are stored in memory.</param>
        /// <param name="mask">The mask to localize the given bit into flags</param>
        /// <param name="metricsOffset">Offset memory from flags pointer to retreive metrics data.</param>
        public PulseProbe(byte* flags, int mask, uint metricsOffset)
        {
            _flags = (uint*)flags;
            _mask = (uint)mask;
            _metricsOffset = metricsOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            // Todo: Test both
            *_flags = *_flags & ~_mask;
            //*_flags = (*_flags | _mask) ^ _mask;
        }

        #region Overrides of ValueType

        public override string ToString()
        {
            var m = _mask;
            return $"Flags: {Tools.SerializeBits((byte*)_flags, sizeof(uint))}, Mask: {Tools.SerializeBits((byte*)&m, sizeof(uint))}";
        }

        #endregion
    }
}