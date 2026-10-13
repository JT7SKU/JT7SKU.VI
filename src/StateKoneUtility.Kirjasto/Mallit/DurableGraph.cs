using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{

    [DebuggerDisplay("Nodes = {Count}")]
    internal sealed class DurableGraph<TNode, TEdge> :
    IDurableGraph<TNode, TEdge>,
    IDurableGraphCommandHandler<TNode, TEdge>,
    IStateMachine
        where TNode : notnull
    {
        private JournalStreamWriter? _writer;
        private readonly Dictionary<TNode, NodeInfo> _nodes = [];
        private readonly IDurableGraphCommandCodec<TNode, TEdge> _codec;

        public DurableGraph(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableGraphCommandCodec<TNode, TEdge>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _nodes.Count;
        public IReadOnlyCollection<TNode> Nodes => _nodes.Keys;

        #region IStateMachine

        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We use a push model, and append entries upon modification.
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter)
        {
            var edgeCount = _nodes.Values.Sum(n => n.Outgoing.Count);
            _codec.WriteSnapshot(Nodes, edgeCount, GetEdges(), snapshotWriter);
        }

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            ApplyClear();
            _writer = writer;
        }

        #endregion

        #region IDurableGraphCommandHandler

        void IDurableGraphCommandHandler<TNode, TEdge>.ApplyAddNode(TNode node) => ApplyAddNode(node);
        void IDurableGraphCommandHandler<TNode, TEdge>.ApplyRemoveNode(TNode node) => ApplyRemoveNode(node);
        void IDurableGraphCommandHandler<TNode, TEdge>.ApplyAddEdge(TNode source, TNode destination, TEdge edge) => ApplyAddEdge(source, destination, edge);
        void IDurableGraphCommandHandler<TNode, TEdge>.ApplyUpsertEdge(TNode source, TNode destination, TEdge edge) => ApplyUpsertEdge(source, destination, edge);
        void IDurableGraphCommandHandler<TNode, TEdge>.ApplyRemoveEdge(TNode source, TNode destination) => ApplyRemoveEdge(source, destination);
        void IDurableGraphCommandHandler<TNode, TEdge>.ApplyClear() => ApplyClear();
        void IDurableGraphCommandHandler<TNode, TEdge>.Reset() => ApplyClear();

        #endregion

        public bool AddNode(TNode node)
        {
            if (_nodes.ContainsKey(node))
            {
                return false;
            }

            _codec.WriteAddNode(node, GetWriter());
            ApplyAddNode(node);

            return true;
        }

        public bool RemoveNode(TNode node)
        {
            if (!_nodes.ContainsKey(node))
            {
                return false;
            }

            _codec.WriteRemoveNode(node, GetWriter());
            ApplyRemoveNode(node);

            return true;
        }

        public bool AddEdge(TNode source, TNode destination, TEdge edge)
        {
            if (!_nodes.ContainsKey(source))
            {
                throw new ArgumentException("The source node does not exist.", nameof(source));
            }

            if (!_nodes.ContainsKey(destination))
            {
                throw new ArgumentException("The destination node does not exist.", nameof(destination));
            }

            if (_nodes[source].Outgoing.ContainsKey(destination))
            {
                return false;
            }

            _codec.WriteAddEdge(source, destination, edge, GetWriter());
            ApplyAddEdge(source, destination, edge);

            return true;
        }

        public void UpsertEdge(TNode source, TNode destination, TEdge edge)
        {
            if (!_nodes.ContainsKey(source))
            {
                throw new ArgumentException("The source node does not exist.", nameof(source));
            }

            if (!_nodes.ContainsKey(destination))
            {
                throw new ArgumentException("The destination node does not exist.", nameof(destination));
            }

            // Since validations passed, the upsert will always succeed:
            // - If the edge does not exist, it is created.
            // - If the edge exists with a different value, it is updated.
            // - If the edge exists with the exact same value, it is overwritten with that same value.

            _codec.WriteUpsertEdge(source, destination, edge, GetWriter());
            ApplyUpsertEdge(source, destination, edge);
        }

        public bool RemoveEdge(TNode source, TNode destination)
        {
            if (!_nodes.TryGetValue(source, out var sourceInfo) || !sourceInfo.Outgoing.ContainsKey(destination))
            {
                return false;
            }

            _codec.WriteRemoveEdge(source, destination, GetWriter());
            ApplyRemoveEdge(source, destination);

            return true;
        }

        public void Clear()
        {
            if (_nodes.Count == 0)
            {
                return;
            }

            _codec.WriteClear(GetWriter());
            ApplyClear();
        }

        public bool ContainsNode(TNode node) => _nodes.ContainsKey(node);

        public bool TryGetEdge(TNode source, TNode destination, [MaybeNullWhen(false)] out TEdge edge)
        {
            if (_nodes.TryGetValue(source, out var sourceInfo) &&
                sourceInfo.Outgoing.TryGetValue(destination, out edge))
            {
                return true;
            }

            edge = default;

            return false;
        }

        public IReadOnlyCollection<TNode> GetNeighbors(TNode node)
        {
            if (!_nodes.TryGetValue(node, out var nodeInfo))
            {
                return [];
            }

            var neighbors = new HashSet<TNode>(nodeInfo.Outgoing.Keys);
            neighbors.UnionWith(nodeInfo.Incoming);

            return neighbors;
        }

        public IReadOnlyCollection<(TNode Node, TEdge Edge)> GetOutgoing(TNode node)
        {
            if (!_nodes.TryGetValue(node, out var nodeInfo))
            {
                return [];
            }

            return nodeInfo.Outgoing.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }

        public IReadOnlyCollection<(TNode Node, TEdge Edge)> GetIncoming(TNode node)
        {
            if (!_nodes.TryGetValue(node, out var nodeInfo))
            {
                return [];
            }

            var result = new List<(TNode Node, TEdge Edge)>();

            foreach (var sourceNode in nodeInfo.Incoming)
            {
                if (_nodes.TryGetValue(sourceNode, out var sourceInfo) &&
                    sourceInfo.Outgoing.TryGetValue(node, out var edge))
                {
                    result.Add((sourceNode, edge));
                }
            }

            return result;
        }

        private void ApplyAddNode(TNode node) => _nodes.TryAdd(node, new NodeInfo());

        private void ApplyRemoveNode(TNode node)
        {
            if (!_nodes.Remove(node, out var nodeInfo))
            {
                return;
            }

            foreach (var destination in nodeInfo.Outgoing.Keys)
            {
                if (_nodes.TryGetValue(destination, out var destinationInfo))
                {
                    destinationInfo.Incoming.Remove(node);
                }
            }

            foreach (var source in nodeInfo.Incoming)
            {
                if (_nodes.TryGetValue(source, out var sourceInfo))
                {
                    sourceInfo.Outgoing.Remove(node);
                }
            }
        }

        private void ApplyAddEdge(TNode source, TNode destination, TEdge edge)
        {
            if (_nodes.TryGetValue(source, out var sourceInfo) &&
                _nodes.TryGetValue(destination, out var destinationInfo))
            {
                if (sourceInfo.Outgoing.TryAdd(destination, edge))
                {
                    destinationInfo.Incoming.Add(source);
                }
            }
        }

        private void ApplyUpsertEdge(TNode source, TNode destination, TEdge edge)
        {
            _nodes[source].Outgoing[destination] = edge;
            _nodes[destination].Incoming.Add(source);
        }

        private void ApplyRemoveEdge(TNode source, TNode destination)
        {
            if (_nodes.TryGetValue(source, out var sourceInfo) &&
                _nodes.TryGetValue(destination, out var destinationInfo))
            {
                if (sourceInfo.Outgoing.Remove(destination))
                {
                    destinationInfo.Incoming.Remove(source);
                }
            }
        }

        private void ApplyClear() => _nodes.Clear();

        private IEnumerable<(TNode Source, TNode Destination, TEdge Edge)> GetEdges()
        {
            foreach (var (source, nodeInfo) in _nodes)
            {
                foreach (var (destination, edge) in nodeInfo.Outgoing)
                {
                    yield return (source, destination, edge);
                }
            }
        }

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<(TNode Node, IReadOnlyCollection<(TNode Destination, TEdge Edge)> OutgoingEdges)> GetEnumerator() =>
            _nodes.Select(kvp => (kvp.Key, (IReadOnlyCollection<(TNode, TEdge)>)
                kvp.Value.Outgoing.Select(e => (e.Key, e.Value)).ToList())).GetEnumerator();

        private sealed class NodeInfo
        {
            /// <summary>
            /// The Key is the destination node (TNode), and the Value is the edge data (TEdge).
            /// </summary>
            public Dictionary<TNode, TEdge> Outgoing { get; } = [];
            /// <summary>
            /// Stores a unique list of all the source nodes that have an edge leading to this one.
            /// </summary>
            /// <remarks>
            /// The edge data (TEdge) is considered to be "owned" by the source node and is stored in its Outgoing list.
            /// This way we avoid duplicating the edge data by using a HashSet for incoming edges.</remarks>
            public HashSet<TNode> Incoming { get; } = [];
        }
    }
}
