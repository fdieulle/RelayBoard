namespace RelayBoard
{
    /// <summary>
    /// Defines the relay output. The <see cref="IRelayBoard"/> will use it to connect with many input.
    /// And inject it a <see cref="PulseProbe"/> which allow the output implementation to know if at least one pulse
    /// was sent to him. And aloso allows the output to reset pulse observations.
    /// </summary>
    public unsafe interface IRelayOutput
    {
        /// <summary>
        /// Gets name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The <see cref="IRelayBoard"/> inject it a <see cref="PulseProbe"/> which allow to know
        /// if at least one pulse was sent to him. And aloso allows the output to reset pulse observations.
        /// </summary>
        /// <param name="state">PulseProbe pointer</param>
        void Inject(PulseProbe* state);
    }
}