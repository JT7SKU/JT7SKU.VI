using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    /// <summary>
    /// Serializes one durable stack command and applies one decoded command.
    /// </summary>
    public interface IDurableStackCommandCodec<T>
    {
        /// <summary>Writes a push command.</summary>
        void WritePush(T item, JournalStreamWriter writer);

        /// <summary>Writes a pop command.</summary>
        void WritePop(JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command, deriving the item count from <paramref name="items"/>.</summary>
        void WriteSnapshot(IReadOnlyCollection<T> items, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableStackCommandHandler<T> handler);
    }
}
}
