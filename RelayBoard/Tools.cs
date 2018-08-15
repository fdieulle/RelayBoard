using System.Collections;
using System.Linq;
using System.Text;

namespace RelayBoard
{
    public static unsafe class Tools
    {
        public const int NB_BITS_PER_BYTE = 8;

        public static string SerializeBits(byte* p, int length)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                sb.Append((*p & 0x01) == 0x01 ? '1' : '0');
                sb.Append((*p & 0x02) == 0x02 ? '1' : '0');
                sb.Append((*p & 0x04) == 0x04 ? '1' : '0');
                sb.Append((*p & 0x08) == 0x08 ? '1' : '0');
                sb.Append((*p & 0x10) == 0x10 ? '1' : '0');
                sb.Append((*p & 0x20) == 0x20 ? '1' : '0');
                sb.Append((*p & 0x80) == 0x80 ? '1' : '0');
                p++;
            }
            return new string(sb.ToString().Reverse().ToArray());
        }

        public static string SerializeBits(BitArray a)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < a.Count; i++)
                sb.Append(a[i] ? '1' : '0');
            return sb.ToString();
        }

    }
}
