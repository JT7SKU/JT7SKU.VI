using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTreeCommandCodec<T>
    {
        /// <summary>Writes a set root command.</summary>
        void WriteSetRoot(T root, JournalStreamWriter writer);

        /// <summary>Writes an add command.</summary>
        void WriteAdd(T parent, T value, JournalStreamWriter writer);

        /// <summary>Writes a remove command.</summary>
        void WriteRemove(T value, JournalStreamWriter writer);

        /// <summary>Writes a move command.</summary>
        void WriteMove(T value, T parent, JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command.</summary>
        void WriteSnapshot(int count, T root, IEnumerable<(T Child, T Parent)> edges, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableTreeCommandHandler<T> handler);
    }

}
