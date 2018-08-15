using System;

namespace RelayBoard
{
    /// <inheritdoc />
    /// <summary>
    /// Relay board connect an input with an output instance.
    /// Pulse notifications are done asynchronously through a state which indicate the output if at least a pulse is reecived or not.
    /// </summary>
    public interface IRelayBoard : IDisposable
    {
        /// <summary>
        /// Connect an <see cref="IRelayInput"/> with an <see cref="IRelayOutput"/>.
        /// </summary>
        /// <param name="input">Input to connect</param>
        /// <param name="output">Output to connect</param>
        /// <returns>Return the connection result to enrich it, if you want. see <see cref="IRelayConnector"/> for more details.</returns>
        IRelayConnector Connect(IRelayInput input, IRelayOutput output);

        /// <summary>
        /// Initialize all conections.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Dequeue call backs ad raise them.
        /// </summary>
        /// <param name="now">Timestamp to deque callbacks</param>
        void Poll(DateTime now);
    }
}