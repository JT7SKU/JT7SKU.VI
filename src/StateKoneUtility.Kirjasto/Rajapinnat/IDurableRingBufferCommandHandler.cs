namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableRingBufferCommandHandler<T>
    {
        /// <summary>Applies a set capacity command.</summary>
        void ApplySetCapacity(int capacity);

        /// <summary>Applies an enqueue command.</summary>
        void ApplyEnqueue(T item);

        /// <summary>Applies a try dequeue command.</summary>
        void ApplyTryDequeue();

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset(int capacity);
    }
}
