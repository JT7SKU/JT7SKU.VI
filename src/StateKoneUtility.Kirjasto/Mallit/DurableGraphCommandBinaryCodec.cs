using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableGraphCommandBinaryCodec<TNode, TEdge>(
    IFieldCodec<TNode> nodeCodec, IFieldCodec<TEdge> edgeCodec, SerializerSessionPool sessionPool)
        : IDurableGraphCommandCodec<TNode, TEdge>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            AddNode = 2,
            RemoveNode = 3,
            AddEdge = 4,
            UpsertEdge = 5,
            RemoveEdge = 6
        }

        public void WriteAddNode(TNode node, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.AddNode);

            nodeCodec.WriteField(ref payloadWriter, 0, typeof(TNode), node);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteRemoveNode(TNode node, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.RemoveNode);

            nodeCodec.WriteField(ref payloadWriter, 0, typeof(TNode), node);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteAddEdge(TNode source, TNode destination, TEdge edge, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.AddEdge);

            nodeCodec.WriteField(ref payloadWriter, 0, typeof(TNode), source);
            nodeCodec.WriteField(ref payloadWriter, 1, typeof(TNode), destination);
            edgeCodec.WriteField(ref payloadWriter, 2, typeof(TEdge), edge);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteUpsertEdge(TNode source, TNode destination, TEdge edge, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.UpsertEdge);

            nodeCodec.WriteField(ref payloadWriter, 0, typeof(TNode), source);
            nodeCodec.WriteField(ref payloadWriter, 1, typeof(TNode), destination);
            edgeCodec.WriteField(ref payloadWriter, 2, typeof(TEdge), edge);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteRemoveEdge(TNode source, TNode destination, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.RemoveEdge);

            nodeCodec.WriteField(ref payloadWriter, 0, typeof(TNode), source);
            nodeCodec.WriteField(ref payloadWriter, 1, typeof(TNode), destination);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteClear(JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Clear);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteSnapshot(IReadOnlyCollection<TNode> nodes, int edgeCount, IEnumerable<(TNode Source, TNode Destination, TEdge Edge)> edges, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);

            // The snapshot format is the list of all nodes, followed by the list of all edges.
            // This strategy avoids complex traversal logic for graphs, which can
            // contain cycles or consist of multiple, disconnected components.

            // First, write the count of all nodes, followed by each node's value.
            payloadWriter.WriteVarUInt32((uint)nodes.Count);

            foreach (var node in nodes)
            {
                nodeCodec.WriteField(ref payloadWriter, 0, typeof(TNode), node);
            }

            // Next, calculate and write the total number of edges.
            payloadWriter.WriteVarUInt32((uint)edgeCount);

            // Finally, iterate through every outgoing edge in the graph and write it as a (source, dest, edge) triplet.
            foreach (var (source, dest, edge) in edges)
            {
                nodeCodec.WriteField(ref payloadWriter, 0, typeof(TNode), source);
                nodeCodec.WriteField(ref payloadWriter, 1, typeof(TNode), dest);
                edgeCodec.WriteField(ref payloadWriter, 2, typeof(TEdge), edge);
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableGraphCommandHandler<TNode, TEdge> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            using var slice = input.Peek(input.Length);
            using var session = sessionPool.GetSession();

            var reader = Reader.Create(slice, session);
            var version = reader.ReadByte();

            if (version != VersionByte)
            {
                throw new NotSupportedException($"This command codec supports version {(uint)VersionByte} and not version {(uint)version}.");
            }

            var command = (CommandType)reader.ReadVarUInt32();

            switch (command)
            {
                case CommandType.AddNode: handler.ApplyAddNode(ReadNode(ref reader)); break;
                case CommandType.RemoveNode: handler.ApplyRemoveNode(ReadNode(ref reader)); break;
                case CommandType.AddEdge: handler.ApplyAddEdge(ReadNode(ref reader), ReadNode(ref reader), ReadEdge(ref reader)); break;
                case CommandType.UpsertEdge: handler.ApplyUpsertEdge(ReadNode(ref reader), ReadNode(ref reader), ReadEdge(ref reader)); break;
                case CommandType.RemoveEdge: handler.ApplyRemoveEdge(ReadNode(ref reader), ReadNode(ref reader)); break;
                case CommandType.Clear: handler.ApplyClear(); break;
                case CommandType.Snapshot:
                    {
                        handler.Reset();

                        // First, read the node count and reconstruct all nodes. This ensures that when we
                        // process the edges in the next step, both source and destination nodes will already exist.
                        var nodeCount = (int)reader.ReadVarUInt32();
                        for (var i = 0; i < nodeCount; i++)
                        {
                            handler.ApplyAddNode(ReadNode(ref reader));
                        }

                        // Next, read the edge count and reconstruct all edges,
                        // linking the nodes that were created in the previous step.
                        var edgeCount = (int)reader.ReadVarUInt32();
                        for (var i = 0; i < edgeCount; i++)
                        {
                            var source = ReadNode(ref reader);
                            var destination = ReadNode(ref reader);
                            var edge = ReadEdge(ref reader);

                            handler.ApplyAddEdge(source, destination, edge);
                        }
                    }
                    break;
                default: Apulaiset.ThrowUnsupportedCommand(command); break;
            }

            Apulaiset.ThrowIfTrailingData(ref reader);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            TNode ReadNode<TInput>(ref Reader<TInput> reader)
            {
                var field = reader.ReadFieldHeader();
                return nodeCodec.ReadValue(ref reader, field);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            TEdge ReadEdge<TInput>(ref Reader<TInput> reader)
            {
                var field = reader.ReadFieldHeader();
                return edgeCodec.ReadValue(ref reader, field);
            }
        }
    }
}
