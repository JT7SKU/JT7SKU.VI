using Orleans.Journaling;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableObjectCommandCodec<T>
    {
        /// <summary>Writes a state command.</summary>
        void WriteState(T value, JournalStreamWriter writer);

        /// <summary>Reads one encoded command and applies it to <paramref name="handler"/>.</summary>
        void Apply(JournalBufferReader input, IDurableObjectCommandHandler<T> handler);
    }
}
