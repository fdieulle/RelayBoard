namespace RelayBoard
{
    /// <summary>
    /// Defines all <see cref="IRelayOutput"/> links from an <see cref="IRelayInput"/>.
    /// Pay attention because the links are built after <see cref="IRelayBoard"/> initialization.
    /// </summary>
    public interface IInputLinks
    {
        /// <summary>
        /// Gets if links are built.
        /// </summary>
        bool IsInitialized { get; }
        /// <summary>
        /// Gets <see cref="IRelayInput"/> instance.
        /// </summary>
        IRelayInput Input { get; }
        /// <summary>
        /// Gets all linked <see cref="IRelayOutput"/> instances.
        /// </summary>
        IRelayOutput[] Outputs { get; }
    }
}