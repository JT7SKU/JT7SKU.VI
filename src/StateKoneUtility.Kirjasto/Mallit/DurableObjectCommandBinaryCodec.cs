using Orleans.Journaling;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using StateKoneUtility.Kirjasto.Rajapinnat;


namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableObjectCommandBinaryCodec<T>(
    IFieldCodec<T> codec, SerializerSessionPool sessionPool) : IDurableObjectCommandCodec<T>
    {
        private const byte VersionByte = 0;

        public void WriteState(T value, JournalStreamWriter writer)
        {
            using var entry = writer.BeginEntry();
            using var session = sessionPool.GetSession();

            var payloadWriter = Writer.Create(entry.Writer, session);

            payloadWriter.WriteByte(VersionByte);
            codec.WriteField(ref payloadWriter, 0, typeof(T), value);

            payloadWriter.Commit();
            entry.Commit();
        }

        public void Apply(JournalBufferReader input, IDurableObjectCommandHandler<T> handler)
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

            var field = reader.ReadFieldHeader();
            handler.ApplyState(codec.ReadValue(ref reader, field));

            Apulaiset.ThrowIfTrailingData(ref reader);
        }
    }
}
