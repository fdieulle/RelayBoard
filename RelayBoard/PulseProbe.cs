using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RelayBoard
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PulseProbe
    {
        // Defines the pointer where flags are stored
        private readonly uint* _flags;
        // Defines the mask to localize the given ouput bit.
        private readonly uint _mask;

        /// <summary>
        /// Gets if the associated output has been pulsed.
        /// </summary>
        public bool IsFlaged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_mask & *_flags) == _mask;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="flags">The pointer wher flags are stored in memory.</param>
        /// <param name="mask">The mask to localize the given bit into flags</param>
        public PulseProbe(byte* flags, int mask)
        {
            _flags = (uint*)flags;
            _mask = (uint)mask;
        }

        /// <summary>
        /// Reset associated flag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            // Todo: Test both performances but i suspect that the non commented is better because there is 1 load less
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