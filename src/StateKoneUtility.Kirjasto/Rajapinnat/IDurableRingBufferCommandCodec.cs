using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableRingBufferCommandCodec<T>
    {
        /// <summary>Writes a set capacity command.</summary>
        void WriteSetCapacity(int capacity, JournalStreamWriter writer);

        /// <summary>Writes an enqueue command.</summary>
        void WriteEnqueue(T item, JournalStreamWriter writer);

        /// <summary>Writes a dequeue command.</summary>
        void WriteDequeue(JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command.</summary>
        void WriteSnapshot(int capacity, int count, IEnumerable<T> items, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableRingBufferCommandHandler<T> handler);
    }
}
