using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableRingBufferCollectionCommandCodec<TKey, TValue>
    {
        /// <summary>Writes a set capacity command.</summary>
        void WriteSetCapacity(TKey key, int capacity, JournalStreamWriter writer);

        /// <summary>Writes an enqueue item command.</summary>
        void WriteEnqueueItem(TKey key, TValue item, JournalStreamWriter writer);

        /// <summary>Writes a dequeue item command.</summary>
        void WriteDequeueItem(TKey key, JournalStreamWriter writer);

        /// <summary>Writes a clear buffer command.</summary>
        void WriteClearBuffer(TKey key, JournalStreamWriter writer);

        /// <summary>Writes a remove buffer command.</summary>
        void WriteRemoveBuffer(TKey key, JournalStreamWriter writer);

        /// <summary>Writes a clear all command.</summary>
        void WriteClearAll(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command, deriving the item count from <paramref name="buffers"/>.</summary>
        void WriteSnapshot(int bufferCount, IEnumerable<(TKey Key, int Capacity, int Count, IEnumerable<TValue> Items)> buffers, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableRingBufferCollectionCommandHandler<TKey, TValue> handler);
    }
}
