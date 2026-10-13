namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTimeWindowBufferCommandHandler<T>
    {
        /// <summary>Applies a set window command.</summary>
        void ApplySetWindow(long windowSeconds);

        /// <summary>Applies an enqueue command.</summary>
        void ApplyEnqueue(T item, long timestamp);

        /// <summary>Applies a try dequeue command.</summary>
        void ApplyTryDequeue();

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset(long windowSeconds);
    }

}
