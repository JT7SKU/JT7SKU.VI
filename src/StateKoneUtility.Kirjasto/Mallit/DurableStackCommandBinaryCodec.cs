using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableStackCommandBinaryCodec<T>(
        IFieldCodec<T> codec, SerializerSessionPool sessionPool) : IDurableStackCommandCodec<T>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            Push = 2,
            Pop = 3
        }

        public void WritePush(T item, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Push);

            codec.WriteField(ref payloadWriter, 0, typeof(T), item);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WritePop(JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Pop);

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

            // Stack enumerates from top-to-bottom, but the restore process pushes items back one-by-one.
            // To reconstruct the original LIFO order, we must reverse the enumeration
            // and write the bottom-most item first, ensuring the final restored state is identical.

            foreach (var item in items.Reverse())
            {
                codec.WriteField(ref payloadWriter, 0, typeof(T), item);
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableStackCommandHandler<T> handler)
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
                case CommandType.Push: handler.ApplyPush(ReadValue(ref reader)); break;
                case CommandType.Pop: handler.ApplyPop(); break;
                case CommandType.Clear: handler.ApplyClear(); break;
                case CommandType.Snapshot:
                    {
                        var count = (int)reader.ReadVarUInt32();

                        handler.Reset(count);

                        for (var i = 0; i < count; i++)
                        {
                            handler.ApplyPush(ReadValue(ref reader));
                        }
                    }
                    break;
                default: Apulaiset.ThrowUnsupportedCommand(command); break;
            }

            Apulaiset.ThrowIfTrailingData(ref reader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T ReadValue<TInput>(ref Reader<TInput> reader)
        {
            var field = reader.ReadFieldHeader();
            return codec.ReadValue(ref reader, field);
        }
    }
}
}
