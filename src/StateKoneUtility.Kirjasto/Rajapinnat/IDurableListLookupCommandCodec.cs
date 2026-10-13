using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableListLookupCommandCodec<TKey, TValue>
    {
        /// <summary>Writes an add command.</summary>
        void WriteAdd(TKey key, TValue value, JournalStreamWriter writer);

        /// <summary>Writes a remove key command.</summary>
        void WriteRemoveKey(TKey key, JournalStreamWriter writer);

        /// <summary>Writes a remove item command.</summary>
        void WriteRemoveItem(TKey key, TValue value, JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command, deriving the item count from <paramref name="items"/>.</summary>
        void WriteSnapshot(IReadOnlyCollection<(TKey, IReadOnlyCollection<TValue>)> items, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableListLookupCommandHandler<TKey, TValue> handler);
    }
}
