using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableTimeWindowBufferCommandBinaryCodec<T>(
    IFieldCodec<T> codec, SerializerSessionPool sessionPool) : IDurableTimeWindowBufferCommandCodec<T>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            SetWindow = 2,
            Enqueue = 3,
            Dequeue = 4
        }

        public void WriteSetWindow(long windowSeconds, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.SetWindow);

            payloadWriter.WriteVarUInt64((ulong)windowSeconds);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteEnqueue(T item, long timestamp, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Enqueue);

            codec.WriteField(ref payloadWriter, 0, typeof(T), item);
            payloadWriter.WriteVarUInt64((ulong)timestamp);

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

        public void WriteSnapshot(long windowSeconds, int count, IEnumerable<(T Item, long Timestamp)> items, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);

            payloadWriter.WriteVarUInt64((ulong)windowSeconds);
            payloadWriter.WriteVarUInt32((uint)count);

            foreach (var (item, timestamp) in items)
            {
                codec.WriteField(ref payloadWriter, 0, typeof(T), item);
                payloadWriter.WriteVarUInt64((ulong)timestamp);
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableTimeWindowBufferCommandHandler<T> handler)
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
                case CommandType.SetWindow: handler.ApplySetWindow((long)reader.ReadVarUInt64()); break;
                case CommandType.Enqueue: handler.ApplyEnqueue(ReadValue(ref reader), (long)reader.ReadVarUInt64()); break;
                case CommandType.Dequeue: handler.ApplyTryDequeue(); break;
                case CommandType.Snapshot:
                    {
                        var windowSeconds = (long)reader.ReadVarUInt64();
                        var count = (int)reader.ReadVarUInt32();

                        handler.Reset(windowSeconds);

                        for (var i = 0; i < count; i++)
                        {
                            var item = ReadValue(ref reader);
                            var timestamp = (long)reader.ReadVarUInt64();

                            handler.ApplyEnqueue(item, timestamp);
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
