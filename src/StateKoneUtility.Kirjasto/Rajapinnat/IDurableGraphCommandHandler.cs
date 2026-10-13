namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableGraphCommandHandler<TNode, TEdge>
    {
        /// <summary>Applies an add node command.</summary>
        void ApplyAddNode(TNode node);

        /// <summary>Applies a remove node command.</summary>
        void ApplyRemoveNode(TNode node);

        /// <summary>Applies an add edge command.</summary>
        void ApplyAddEdge(TNode source, TNode destination, TEdge edge);

        /// <summary>Applies an upsert edge command.</summary>
        void ApplyUpsertEdge(TNode source, TNode destination, TEdge edge);

        /// <summary>Applies a remove edge command.</summary>
        void ApplyRemoveEdge(TNode source, TNode destination);

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset();
    }


}
