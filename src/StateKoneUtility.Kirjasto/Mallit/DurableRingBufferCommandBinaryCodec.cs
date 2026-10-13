using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableRingBufferCommandBinaryCodec<T>(
    IFieldCodec<T> codec, SerializerSessionPool sessionPool) : IDurableRingBufferCommandCodec<T>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            SetCapacity = 2,
            Enqueue = 3,
            Dequeue = 4
        }

        public void WriteSetCapacity(int capacity, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.SetCapacity);

            payloadWriter.WriteVarUInt32((uint)capacity);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteEnqueue(T item, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Enqueue);

            codec.WriteField(ref payloadWriter, 0, typeof(T), item);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteDequeue(JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Dequeue);

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

        public void WriteSnapshot(int capacity, int count, IEnumerable<T> items, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);

            payloadWriter.WriteVarUInt32((uint)count);
            payloadWriter.WriteVarUInt32((uint)capacity);

            foreach (var item in items)
            {
                codec.WriteField(ref payloadWriter, 0, typeof(T), item);
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableRingBufferCommandHandler<T> handler)
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
                case CommandType.Clear: handler.ApplyClear(); break;
                case CommandType.SetCapacity: handler.ApplySetCapacity((int)reader.ReadVarUInt32()); break;
                case CommandType.Enqueue: handler.ApplyEnqueue(ReadValue(ref reader)); break;
                case CommandType.Dequeue: handler.ApplyTryDequeue(); break;
                case CommandType.Snapshot:
                    {
                        var count = (int)reader.ReadVarUInt32();
                        var capacity = (int)reader.ReadVarUInt32();

                        // Since we support a dynamic capacity, we restore the buffer to the capacity it had when the snapshot was taken.
                        handler.Reset(capacity);

                        for (var i = 0; i < count; i++)
                        {
                            handler.ApplyEnqueue(ReadValue(ref reader));
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
