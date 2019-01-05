using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace RelayBoard.Internals
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PulseSource
    {
        private byte* _flags;
        private int _maskOffset;
        private ushort _maskLength;
        private ushort _index;

        public void Initialize(byte* flags, int maskOffset, int maskLength)
        {
            if(maskLength > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maskLength), $"You have too many IRelayOutput linked to input. Max={(int)ushort.MaxValue * Tools.NB_BITS_PER_BYTE}, Current={maskLength * Tools.NB_BITS_PER_BYTE}");

            _flags = flags;
            _maskOffset = maskOffset;
            _maskLength = (ushort)maskLength;
            _index = 0;
        }

        public void UpdateMask(BitArray a)
        {
            var tmp = new int[_maskLength / sizeof(int) + sizeof(int)];
            a.CopyTo(tmp, 0);

            var p = _flags + _maskOffset;
            fixed (int* pt = tmp)
            {
                var t = (byte*)pt;
                for (var i = 0; i < _maskLength; i++)
                    *(p + i) = *(t + i);
            }
        }

        /// <summary>
        /// Set all flags specified into the mask to 1.
        /// </summary>
        public void Pulse()
        {
            // Here is the bigger gain, because we can pulse IRelayOutput 64 by 64
            for (_index = 0; _index < _maskLength - sizeof(ulong); _index += sizeof(ulong))
                *(ulong*)(_flags + _index) |= *(ulong*)(_flags + _maskOffset + _index);

            // Last loops (max 3 times) if the number of bits isn't modulo 64
            for (; _index < _maskLength; _index++)
                *(_flags + _index) |= *(_flags + _maskOffset + _index);
        }

        #region Overrides of ValueType

        public override string ToString() 
            => $"Flags: {Tools.SerializeBits(_flags, _maskLength)}, Mask: {Tools.SerializeBits(_flags + _maskOffset, _maskLength)}";

        #endregion
    }
}