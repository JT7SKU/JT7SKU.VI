using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableOrderedSetLookupCommandBinaryCodec<TKey, TValue>(
    IFieldCodec<TKey> keyCodec, IFieldCodec<TValue> valueCodec, SerializerSessionPool sessionPool)
        : IDurableOrderedSetLookupCommandCodec<TKey, TValue>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            Add = 2,
            RemoveKey = 3,
            RemoveItem = 4
        }

        public void WriteAdd(TKey key, TValue value, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Add);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);
            valueCodec.WriteField(ref payloadWriter, 1, typeof(TValue), value);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteRemoveKey(TKey key, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.RemoveKey);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteRemoveItem(TKey key, TValue value, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.RemoveItem);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);
            valueCodec.WriteField(ref payloadWriter, 1, typeof(TValue), value);

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

        public void WriteSnapshot(IReadOnlyCollection<(TKey, IReadOnlyCollection<TValue>)> items, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);
            payloadWriter.WriteVarUInt32((uint)items.Count);

            foreach (var (key, set) in items)
            {
                keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);
                payloadWriter.WriteVarUInt32((uint)set.Count);

                foreach (var value in set)
                {
                    valueCodec.WriteField(ref payloadWriter, 1, typeof(TValue), value);
                }
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableOrderedSetLookupCommandHandler<TKey, TValue> handler)
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
                case CommandType.Add: handler.ApplyAdd(ReadKey(ref reader), ReadValue(ref reader)); break;
                case CommandType.RemoveKey: handler.ApplyRemoveKey(ReadKey(ref reader)); break;
                case CommandType.RemoveItem: handler.ApplyRemoveItem(ReadKey(ref reader), ReadValue(ref reader)); break;
                case CommandType.Clear: handler.ApplyClear(); break;
                case CommandType.Snapshot:
                    {
                        var keyCount = (int)reader.ReadVarUInt32();
                        handler.Reset(keyCount);

                        for (var i = 0; i < keyCount; i++)
                        {
                            var key = ReadKey(ref reader);
                            var valueCount = (int)reader.ReadVarUInt32();

                            for (var j = 0; j < valueCount; j++)
                            {
                                handler.ApplyAdd(key, ReadValue(ref reader));
                            }
                        }
                    }
                    break;
                default: Apulaiset.ThrowUnsupportedCommand(command); break;
            }

            Apulaiset.ThrowIfTrailingData(ref reader);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            TKey ReadKey<TInput>(ref Reader<TInput> reader)
            {
                var field = reader.ReadFieldHeader();
                return keyCodec.ReadValue(ref reader, field);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            TValue ReadValue<TInput>(ref Reader<TInput> reader)
            {
                var field = reader.ReadFieldHeader();
                return valueCodec.ReadValue(ref reader, field);
            }
        }
    }
}
