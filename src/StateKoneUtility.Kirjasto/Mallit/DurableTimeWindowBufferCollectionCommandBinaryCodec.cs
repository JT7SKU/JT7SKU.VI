using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableTimeWindowBufferCollectionCommandBinaryCodec<TKey, TValue>(
    IFieldCodec<TKey> keyCodec, IFieldCodec<TValue> valueCodec, SerializerSessionPool sessionPool)
        : IDurableTimeWindowBufferCollectionCommandCodec<TKey, TValue>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Snapshot = 0,
            ClearAll = 1,
            ClearBuffer = 2,
            RemoveBuffer = 3,
            SetWindow = 4,
            EnqueueItem = 5,
            DequeueItem = 6
        }

        public void WriteSetWindow(TKey key, long windowSeconds, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.SetWindow);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);
            payloadWriter.WriteVarUInt64((ulong)windowSeconds);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteEnqueueItem(TKey key, TValue item, long timestamp, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.EnqueueItem);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);
            valueCodec.WriteField(ref payloadWriter, 0, typeof(TValue), item);
            payloadWriter.WriteVarUInt64((ulong)timestamp);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteDequeueItem(TKey key, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.DequeueItem);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteClearBuffer(TKey key, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.ClearBuffer);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteRemoveBuffer(TKey key, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.RemoveBuffer);

            keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteClearAll(JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.ClearAll);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteSnapshot(int bufferCount, IEnumerable<(TKey Key, long WindowSeconds, int Count, IEnumerable<(TValue Item, long Timestamp)> Items)> buffers, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);

            payloadWriter.WriteVarUInt32((uint)bufferCount);

            foreach (var (key, windowSeconds, count, items) in buffers)
            {
                // First we write the key and the buffer-specific metadata.
                keyCodec.WriteField(ref payloadWriter, 0, typeof(TKey), key);

                payloadWriter.WriteVarUInt64((ulong)windowSeconds);
                payloadWriter.WriteVarUInt32((uint)count);

                // Then for the current buffer we write all its items.
                foreach (var (item, timestamp) in items)
                {
                    valueCodec.WriteField(ref payloadWriter, 0, typeof(TValue), item);
                    payloadWriter.WriteVarUInt64((ulong)timestamp);
                }
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue> handler)
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
                case CommandType.ClearAll: handler.ApplyClearAll(); break;
                case CommandType.ClearBuffer: handler.ApplyClearBuffer(ReadKey(ref reader)); break;
                case CommandType.RemoveBuffer: handler.ApplyRemoveBuffer(ReadKey(ref reader)); break;
                case CommandType.SetWindow: handler.ApplySetWindow(ReadKey(ref reader), (long)reader.ReadVarUInt64()); break;
                case CommandType.EnqueueItem: handler.ApplyEnqueueItem(ReadKey(ref reader), ReadValue(ref reader), (long)reader.ReadVarUInt64()); break;
                case CommandType.DequeueItem: handler.ApplyTryDequeueItem(ReadKey(ref reader)); break;
                case CommandType.Snapshot:
                    {
                        var bufferCount = (int)reader.ReadVarUInt32();
                        handler.Reset();

                        for (var i = 0; i < bufferCount; i++)
                        {
                            var key = ReadKey(ref reader);
                            var windowSeconds = (long)reader.ReadVarUInt64();
                            var itemCount = (int)reader.ReadVarUInt32();

                            handler.ApplySetWindow(key, windowSeconds);

                            for (var j = 0; j < itemCount; j++)
                            {
                                var value = ReadValue(ref reader);
                                var timestamp = (long)reader.ReadVarUInt64();

                                handler.ApplyEnqueueItem(key, value, timestamp);
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
