using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurablePriorityQueueCommandCodec<TElement, TPriority>(
      IFieldCodec<TElement> elementCodec, IFieldCodec<TPriority> priorityCodec, SerializerSessionPool sessionPool)
          : IDurablePriorityQueueCommandCodec<TElement, TPriority>
    {
        private const byte VersionByte = 0;

        private enum CommandType : uint
        {
            Clear = 0,
            Snapshot = 1,
            Enqueue = 2,
            Dequeue = 3,
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

        public void WriteEnqueue(TElement element, TPriority priority, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Enqueue);

            elementCodec.WriteField(ref payloadWriter, 0, typeof(TElement), element);
            priorityCodec.WriteField(ref payloadWriter, 1, typeof(TPriority), priority);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void WriteSnapshot(IReadOnlyCollection<(TElement, TPriority)> items, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteVarUInt32((uint)CommandType.Snapshot);
            payloadWriter.WriteVarUInt32((uint)items.Count);

            // We snapshot the complete state by serializing all element-priority pairs.
            // The order of items is irrelevant, as the restore process reconstructs the heap by re-enqueuing each item individually.
            // This correctly restores the logical state of the queue regardless of the snapshot's internal order.
            // Note that the enumerating over 'items' means enumerating _items.UnorderedItems.GetEnumerator() which is what we want.

            foreach (var (element, priority) in items)
            {
                elementCodec.WriteField(ref payloadWriter, 0, typeof(TElement), element);
                priorityCodec.WriteField(ref payloadWriter, 1, typeof(TPriority), priority);
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurablePriorityQueueCommandHandler<TElement, TPriority> handler)
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
                case CommandType.Enqueue: handler.ApplyEnqueue(ReadElement(ref reader), ReadPriority(ref reader)); break;
                case CommandType.Dequeue: handler.ApplyDequeue(); break;
                case CommandType.Snapshot:
                    {
                        var count = (int)reader.ReadVarUInt32();

                        handler.Reset(count);

                        for (var i = 0; i < count; i++)
                        {
                            handler.ApplyEnqueue(ReadElement(ref reader), ReadPriority(ref reader));
                        }
                    }
                    break;
                default: Apulaiset.ThrowUnsupportedCommand(command); break;
            }

            Apulaiset.ThrowIfTrailingData(ref reader);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            TElement ReadElement<TInput>(ref Reader<TInput> reader)
            {
                var field = reader.ReadFieldHeader();
                return elementCodec.ReadValue(ref reader, field);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            TPriority ReadPriority<TInput>(ref Reader<TInput> reader)
            {
                var field = reader.ReadFieldHeader();
                return priorityCodec.ReadValue(ref reader, field);
            }
        }
    }

}
