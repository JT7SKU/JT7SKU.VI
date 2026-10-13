using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurablePriorityQueueCommandCodec<TElement, TPriority>
    {
        /// <summary>Writes an enqueue command.</summary>
        void WriteEnqueue(TElement element, TPriority priority, JournalStreamWriter writer);

        /// <summary>Writes a dequeue command.</summary>
        void WriteDequeue(JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command, deriving the item count from <paramref name="items"/>.</summary>
        void WriteSnapshot(IReadOnlyCollection<(TElement, TPriority)> items, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurablePriorityQueueCommandHandler<TElement, TPriority> handler);
    }
}
