namespace RelayBoard
{
    /// <summary>
    /// Defines all <see cref="IRelayInput"/> links from an <see cref="IRelayOutput"/>.
    /// Pay attention because the links are built after <see cref="IRelayBoard"/> initialization.
    /// </summary>
    public interface IOutputLinks
    {
        /// <summary>
        /// Gets if links are built.
        /// </summary>
        bool IsInitialized { get; }
        /// <summary>
        /// Gets <see cref="IRelayOutput"/> instance.
        /// </summary>
        IRelayOutput Output { get; }
        /// <summary>
        /// Gets all linked <see cref="IRelayInput"/> instances.
        /// </summary>
        IRelayInput[] Inputs { get; }
    }
}