using System;
using NUnit.Framework;

namespace RelayBoard.Tests
{
    public static class Checker
    {
        public static void Check(this RelayOutputMock relayOutputMock, bool isInvalidated, DateTime lastUpdateTimestamp)
        {
            Assert.AreEqual(isInvalidated, relayOutputMock.IsInvalidated, "IsFlaged");
            Assert.AreEqual(lastUpdateTimestamp, relayOutputMock.LastUpdateTimestamp, "LastUpdateTimestamp");
        }
    }
}
