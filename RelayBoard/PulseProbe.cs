using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RelayBoard
{
    /// <summary>
    /// PulseProbe is inject in each <see cref="IRelayOutput"/> instance.
    /// It allows to know if the associated bit is set on or off through <see cref="IsOn"/> readonly property.
    /// Once the <see cref="IRelayOutput"/> wants waiting for another pulse it has to call <see cref="SetOff"/> method
    /// to set its flag to 0 (Off).
    /// 
    /// This structure is encoded into 4 Bytes as a simple <see cref="int"/>.
    /// Memory specifications:
    /// 
    /// Bits    | Description
    /// --------|-----------------------------
    /// 3 bits  | Mask value to apply on flags defines like
    ///         |
    ///         |  bits | value |   mask
    ///         | ------|-------|-----------
    ///         |   000 |     0 | 00000001
    ///         |   001 |     1 | 00000010
    ///         |   010 |     2 | 00000100
    ///         |   011 |     3 | 00001000
    ///         |   100 |     4 | 00010000
    ///         |   101 |     5 | 00100000
    ///         |   110 |     6 | 01000000
    ///         |   111 |     7 | 10000000
    /// --------|-----------------------------
    /// 1 bit   | Memory offset direction from this struct
    ///         | 0 : + Positive offset
    ///         | 1 : - Negative offset
    /// --------|-----------------------------
    /// 28 bits | Offset to retrieve flags memory
    ///         | Max offset value: 2^28 ~= 268 MB
    ///         | 
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public unsafe struct PulseProbe
    {
        private const int SHIFT_MASK = 29;
        private const int SHIFT_SIGN_OFFSET = 28;

        private const uint SIGN_OFFSET_MASK = 0x10000000;
        private const uint OFFSET_MASK =      0x0FFFFFFF;

        [FieldOffset(0)]
        public uint _maskAndOffset;

        /// <summary>
        /// Gets if the associated output has been pulsed.
        /// </summary>
        public bool IsOn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var mask = GetMask();
                return (mask & *GetFlags()) == mask;
            }
        }

        internal void Initialize(byte* flags, int mask)
        {
            switch (mask)
            {
                case 0x1:
                    _maskAndOffset = 0u << SHIFT_MASK;
                    break;
                case 0x2:
                    _maskAndOffset = 1u << SHIFT_MASK;
                    break;
                case 0x4:
                    _maskAndOffset = 2u << SHIFT_MASK;
                    break;
                case 0x8:
                    _maskAndOffset = 3u << SHIFT_MASK;
                    break;
                case 0x10:
                    _maskAndOffset = 4u << SHIFT_MASK;
                    break;
                case 0x20:
                    _maskAndOffset = 5u << SHIFT_MASK;
                    break;
                case 0x40:
                    _maskAndOffset = 6u << SHIFT_MASK;
                    break;
                case 0x80:
                    _maskAndOffset = 7u << SHIFT_MASK;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mask), "The mask should have only one bit set to 1 maximum");
            }

            var offset = flags - (byte*)Unsafe.AsPointer(ref this);
            if (offset >= OFFSET_MASK + sizeof(PulseProbe)) throw new ArgumentOutOfRangeException();

            _maskAndOffset |= (offset < 0 ? 1u : 0u)  << SHIFT_SIGN_OFFSET;
            _maskAndOffset |= (uint)Math.Abs(offset);

            if(mask != GetMask() || flags != GetFlags())
                throw new InvalidDataException("Wrong PulseProbe initialization");
        }

        /// <summary>
        /// Sets the flag off.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOff()
        {
            var flags = (uint*)GetFlags();
            *flags = *flags & ~GetMask();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* GetFlags()
        {
            return (_maskAndOffset & SIGN_OFFSET_MASK) == SIGN_OFFSET_MASK
                ? (byte*)Unsafe.AsPointer(ref this) - (_maskAndOffset & OFFSET_MASK)
                : (byte*)Unsafe.AsPointer(ref this) + (_maskAndOffset & OFFSET_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetMask()
        {
            return 1u << (int)(_maskAndOffset >> SHIFT_MASK);
        }

        #region Overrides of ValueType

        public override string ToString()
        {
            var m = _maskAndOffset >> 21;
            fixed (PulseProbe* p = &this)
            {
                var flags = (uint*)p + (_maskAndOffset & 0x1FFFFFFF);
                return $"Flags: {Tools.SerializeBits((byte*)flags, sizeof(uint))}, Mask: {Tools.SerializeBits((byte*)&m, sizeof(uint))}";
            }
        }

        #endregion
    }
}