using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTimeWindowBufferCommandCodec<T>
    {
        /// <summary>Writes a set window command.</summary>
        void WriteSetWindow(long windowSeconds, JournalStreamWriter writer);

        /// <summary>Writes an enqueue command.</summary>
        void WriteEnqueue(T item, long timestamp, JournalStreamWriter writer);

        /// <summary>Writes a dequeue command.</summary>
        void WriteDequeue(JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command.</summary>
        void WriteSnapshot(long windowSeconds, int count, IEnumerable<(T Item, long Timestamp)> items, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableTimeWindowBufferCommandHandler<T> handler);
    }
}
