using System;

namespace RelayBoard
{
    /// <inheritdoc />
    /// <summary>
    /// Result of a connection between <see cref="T:RelayBoard.IRelayInput" /> and <see cref="T:RelayBoard.IRelayOutput" />.
    /// This interface allows you to add callbacks on this connection.
    /// If you dispose it the connection will terminate.
    /// </summary>
    public interface IRelayConnector : IDisposable
    {
        /// <summary>
        /// Add a callback on connection.
        /// </summary>
        /// <param name="callback">Callback delegate to add</param>
        /// <returns>Returns the subscription result. Dispose it to end subscription.</returns>
        IDisposable Subscribe(Action<DateTime> callback);
    }
}