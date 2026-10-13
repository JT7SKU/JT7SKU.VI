namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    /// <summary>
    /// Receives decoded durable cancellation token source commands from a codec implementation.
    /// </summary>
    public interface IDurableCancellationTokenSourceCommandHandler
    {
        /// <summary>Applies a state command.</summary>
        void ApplyState(bool isCanceled, bool isScheduled, long requestTicks, long delayTicks);

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset();
    }
}
