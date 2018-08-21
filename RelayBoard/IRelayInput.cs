using System;

namespace RelayBoard
{
    /// <summary>
    /// Defines the relay input. The <see cref="IRelayBoard"/> will subscribe on it to dispatch the event.
    /// </summary>
    public interface IRelayInput
    {
        /// <summary>
        /// Gets name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Subscribes on the relay input.
        /// </summary>
        /// <param name="onTick">Callback call when the relay is pulsed.</param>
        /// <returns>Returns the subscription result. Dispose it to end subscription.</returns>
        IDisposable Subscribe(Action onTick);
    }
}
