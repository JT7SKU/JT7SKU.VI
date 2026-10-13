
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Diagnostics;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableGraphDebugView<TNode, TEdge>(IDurableGraph<TNode, TEdge> graph)
    where TNode : notnull
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DebugViewItem[] Items => graph.Nodes.Select(n => new DebugViewItem(n, graph.GetOutgoing(n))).ToArray();

        [DebuggerDisplay("{Node}, Outgoing = {Outgoing.Length}")]
        internal readonly struct DebugViewItem(TNode node, IReadOnlyCollection<(TNode Node, TEdge Edge)> outgoing)
        {
            public TNode Node { get; } = node;
            public (TNode Node, TEdge Edge)[] Outgoing { get; } = [.. outgoing];
        }
    }
}
