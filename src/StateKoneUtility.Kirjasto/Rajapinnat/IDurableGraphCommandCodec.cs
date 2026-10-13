using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableGraphCommandCodec<TNode, TEdge>
    {
        /// <summary>Writes an add node command.</summary>
        void WriteAddNode(TNode node, JournalStreamWriter writer);

        /// <summary>Writes a remove node command.</summary>
        void WriteRemoveNode(TNode node, JournalStreamWriter writer);

        /// <summary>Writes an add edge command.</summary>
        void WriteAddEdge(TNode source, TNode destination, TEdge edge, JournalStreamWriter writer);

        /// <summary>Writes an upsert edge command.</summary>
        void WriteUpsertEdge(TNode source, TNode destination, TEdge edge, JournalStreamWriter writer);

        /// <summary>Writes a remove edge command.</summary>
        void WriteRemoveEdge(TNode source, TNode destination, JournalStreamWriter writer);

        /// <summary>Writes a clear command.</summary>
        void WriteClear(JournalStreamWriter writer);

        /// <summary>Writes a snapshot command.</summary>
        void WriteSnapshot(IReadOnlyCollection<TNode> nodes, int edgeCount, IEnumerable<(TNode Source, TNode Destination, TEdge Edge)> edges, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableGraphCommandHandler<TNode, TEdge> handler);
    }
}
