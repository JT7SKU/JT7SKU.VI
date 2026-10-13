using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    /// <summary>
    /// Serializes one durable time window buffer collection command and applies one decoded command.
    /// </summary>
    public interface IDurableTimeWindowBufferCollectionCommandCodec<TKey, TValue>
    {
        /// <summary>Writes a set window command.</summary>
        void WriteSetWindow(TKey key, long windowSeconds, JournalStreamWriter writer);

        /// <summary>Writes an enqueue item command.</summary>
        void WriteEnqueueItem(TKey key, TValue item, long timestamp, JournalStreamWriter writer);

        /// <summary>Writes a dequeue item command.</summary>
        void WriteDequeueItem(TKey key, JournalStreamWriter writer);

        /// <summary>Writes a clear buffer command.</summary>
        void WriteClearBuffer(TKey key, JournalStreamWriter writer);

        /// <summary>Writes a remove buffer command.</summary>
        void WriteRemoveBuffer(TKey key, JournalStreamWriter writer);

        /// <summary>Writes a clear all command.</summary>
        void WriteClearAll(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command, deriving the item count from <paramref name="buffers"/>.</summary>
        void WriteSnapshot(int bufferCount, IEnumerable<(TKey Key, long WindowSeconds, int Count, IEnumerable<(TValue Item, long Timestamp)> Items)> buffers, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue> handler);
    }

}
