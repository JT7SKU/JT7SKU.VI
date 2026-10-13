using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableOrderedSetCommandBinaryCodec<T>(
    IFieldCodec<T> codec, SerializerSessionPool sessionPool) : IDurableOrderedSetCommandCodec<T>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            Add = 2,
            Remove = 3
        }

        public void WriteAdd(T item, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Add);

            codec.WriteField(ref payloadWriter, 0, typeof(T), item);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteRemove(T item, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Remove);

            codec.WriteField(ref payloadWriter, 0, typeof(T), item);

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

        public void WriteSnapshot(IReadOnlyCollection<T> items, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);
            payloadWriter.WriteVarUInt32((uint)items.Count);

            // We iterate the list to preserve order.
            foreach (var item in items)
            {
                codec.WriteField(ref payloadWriter, 0, typeof(T), item);
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableOrderedSetCommandHandler<T> handler)
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
                case CommandType.Add: handler.ApplyAdd(ReadValue(ref reader)); break;
                case CommandType.Remove: handler.ApplyRemove(ReadValue(ref reader)); break;
                case CommandType.Clear: handler.ApplyClear(); break;
                case CommandType.Snapshot:
                    {
                        var count = (int)reader.ReadVarUInt32();
                        handler.Reset(count);
                        for (var i = 0; i < count; i++)
                        {
                            handler.ApplyAdd(ReadValue(ref reader));
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
