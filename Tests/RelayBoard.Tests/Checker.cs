using System;
using NUnit.Framework;

namespace RelayBoard.Tests
{
    public static class Checker
    {
        public static void Check(this RelayOutputMock relayOutputMock, bool isInvalidated)
        {
            Assert.AreEqual(isInvalidated, relayOutputMock.IsInvalidated, "IsFlaged");
        }
    }
}
