using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableOrderedSetCommandCodec<T>
    {
        /// <summary>Writes an add command.</summary>
        void WriteAdd(T item, JournalStreamWriter writer);

        /// <summary>Writes a remove command.</summary>
        void WriteRemove(T item, JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command, deriving the item count from <paramref name="items"/>.</summary>
        void WriteSnapshot(IReadOnlyCollection<T> items, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableOrderedSetCommandHandler<T> handler);
    }
}
