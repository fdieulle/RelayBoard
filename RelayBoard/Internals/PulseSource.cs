﻿using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace RelayBoard.Internals
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PulseSource
    {
        private readonly byte* _flags;
        private readonly int _maskOffset;
        private readonly ushort _maskLength;
        private ushort _index;
        private long _lastTicks;

        public long LastTicks => _lastTicks;

        public PulseSource(byte* flags, int maskOffset, int maskLength)
        {
            _flags = flags;
            _maskOffset = maskOffset;
            _maskLength = (ushort)maskLength;
            _index = 0;
            _lastTicks = DateTime.MinValue.Ticks;
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
        /// <param name="timestamp">Current timestamp</param>
        public void Pulse(DateTime timestamp)
        {
            // Here is the bigger gain, because we can pulse IRelayOutput 64 by 64
            for (_index = 0; _index < _maskLength - sizeof(ulong); _index += sizeof(ulong))
                *(ulong*)(_flags + _index) |= *(ulong*)(_flags + _maskOffset + _index);

            // Last loops (max 3 times) if the number of bits isn't modulo 64
            for (; _index < _maskLength; _index++)
                *(_flags + _index) |= *(_flags + _maskOffset + _index);

            // Store the timestamp.
            _lastTicks = timestamp.Ticks;
        }

        #region Overrides of ValueType

        public override string ToString()
        {
            return $"Flags: {Tools.SerializeBits(_flags, _maskLength)}, Mask: {Tools.SerializeBits(_flags + _maskOffset, _maskLength)}";
        }

        #endregion
    }
}