using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    /// <summary>
    /// Serializes one durable cancellation token source command and applies one decoded command.
    /// </summary>
    public interface IDurableCancellationTokenSourceCommandCodec
    {
        /// <summary>Writes a state command.</summary>
        void WriteState(bool isCanceled, bool isScheduled, long requestTicks, long delayTicks, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableCancellationTokenSourceCommandHandler handler);
    }
}
