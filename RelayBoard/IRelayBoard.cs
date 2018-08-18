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
        /// Gets an interface which describes all <see cref="IRelayOutput"/> linked instances on a given <see cref="IRelayInput"/>.
        /// </summary>
        /// <param name="input">Input instance to get links</param>
        /// <returns>Returns the linked list.</returns>
        IInputLinks GetInputLinks(IRelayInput input);

        /// <summary>
        /// Gets an interface which describes all <see cref="IRelayInput"/> linked instances on a given <see cref="IRelayOutput"/>.
        /// </summary>
        /// <param name="output">Ouput instance to get links</param>
        /// <returns>Returns the linked list.</returns>
        IOutputLinks GetOutputLinks(IRelayOutput output);

        /// <summary>
        /// Dequeue callbacks and call them.
        /// </summary>
        /// <param name="now">Timestamp to deque callbacks</param>
        void Poll(DateTime now);
    }
}