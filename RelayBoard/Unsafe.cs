using System.Runtime.CompilerServices;
using InlineIL;
using static InlineIL.IL.Emit;

namespace RelayBoard
{
    public static unsafe class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T value)
        {
            Ldarg(nameof(value));
            Conv_U();
            Ret();
            throw IL.Unreachable();
        }
    }
}
