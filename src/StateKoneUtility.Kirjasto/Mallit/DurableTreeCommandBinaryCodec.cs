using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableTreeCommandBinaryCodec<T>(
    IFieldCodec<T> codec, SerializerSessionPool sessionPool) : IDurableTreeCommandCodec<T>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            Add = 2,
            Remove = 3,
            Move = 4,
            SetRoot = 5,
        }

        public void WriteSetRoot(T root, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.SetRoot);

            codec.WriteField(ref payloadWriter, 0, typeof(T), root);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteAdd(T parent, T value, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Add);

            codec.WriteField(ref payloadWriter, 0, typeof(T), parent);
            codec.WriteField(ref payloadWriter, 1, typeof(T), value);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteRemove(T value, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Remove);

            codec.WriteField(ref payloadWriter, 0, typeof(T), value);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteMove(T value, T parent, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Move);

            codec.WriteField(ref payloadWriter, 0, typeof(T), value);
            codec.WriteField(ref payloadWriter, 1, typeof(T), parent);

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

        public void WriteSnapshot(int count, T root, IEnumerable<(T Child, T Parent)> edges, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);

            payloadWriter.WriteVarUInt32((uint)count);

            if (count > 0)
            {
                // The root is a special node, so we always write it first.
                codec.WriteField(ref payloadWriter, 0, typeof(T), root);

                foreach (var (child, parent) in edges)
                {
                    // For each child found, we write the (child, parent) pair.
                    // The 'parent' is the 'current' node from the queue.

                    codec.WriteField(ref payloadWriter, 0, typeof(T), child);
                    codec.WriteField(ref payloadWriter, 1, typeof(T), parent);
                }
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableTreeCommandHandler<T> handler)
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
                case CommandType.SetRoot: handler.ApplySetRoot(ReadValue(ref reader)); break;
                case CommandType.Add: handler.ApplyAdd(ReadValue(ref reader), ReadValue(ref reader)); break;
                case CommandType.Remove: handler.ApplyRemove(ReadValue(ref reader)); break;
                case CommandType.Move: handler.ApplyMove(ReadValue(ref reader), ReadValue(ref reader)); break;
                case CommandType.Clear: handler.ApplyClear(); break;
                case CommandType.Snapshot:
                    {
                        var count = (int)reader.ReadVarUInt32();

                        handler.Reset();

                        if (count == 0)
                        {
                            break;
                        }

                        // Because of the level-order guarantee (Breadth-First Search),
                        // the very first item in the snapshot is always the root node.

                        var root = ReadValue(ref reader);
                        handler.ApplySetRoot(root);

                        for (var i = 1; i < count; i++)
                        {
                            // Parent can not be null here, because we are 100% certain the parent node was
                            // processed in a previous iteration (or was the root) and already exists in _nodes.

                            var child = ReadValue(ref reader);
                            var parent = ReadValue(ref reader);

                            handler.ApplyAdd(parent, child);
                        }
                    }
                    break;
                default: Apulaiset.ThrowUnsupportedCommand(command); break;
            }

            Apulaiset.ThrowIfTrailingData(ref reader);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            T ReadValue<TInput>(ref Reader<TInput> reader)
            {
                var field = reader.ReadFieldHeader();
                return codec.ReadValue(ref reader, field);
            }
        }
    }

}
