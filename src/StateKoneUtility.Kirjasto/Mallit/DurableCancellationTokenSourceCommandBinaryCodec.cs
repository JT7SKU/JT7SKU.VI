using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableCancellationTokenSourceCommandBinaryCodec(
        SerializerSessionPool sessionPool) : IDurableCancellationTokenSourceCommandCodec
    {
        private const byte VersionByte = 0;

        public void WriteState(bool isCanceled, bool isScheduled, long requestTicks, long delayTicks, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            payloadWriter.WriteByte((byte)(isCanceled ? 1 : 0));
            payloadWriter.WriteByte((byte)(isScheduled ? 1 : 0));

            if (isScheduled)
            {
                payloadWriter.WriteInt64(requestTicks);
                payloadWriter.WriteInt64(delayTicks);
            }

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableCancellationTokenSourceCommandHandler handler)
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

            var isCanceled = reader.ReadByte() == 1;
            var isScheduled = reader.ReadByte() == 1;

            long requestTicks = -1;
            long delayTicks = -1;

            if (isScheduled)
            {
                requestTicks = reader.ReadInt64();
                delayTicks = reader.ReadInt64();
            }

            handler.ApplyState(isCanceled, isScheduled, requestTicks, delayTicks);

            Helpers.ThrowIfTrailingData(ref reader);
        }
    }
}
