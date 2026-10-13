namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableRingBufferCollectionCommandHandler<TKey, TValue>
    {
        /// <summary>Applies a set capacity command.</summary>
        void ApplySetCapacity(TKey key, int capacity);

        /// <summary>Applies an enqueue item command.</summary>
        void ApplyEnqueueItem(TKey key, TValue item);

        /// <summary>Applies a try dequeue item command.</summary>
        void ApplyTryDequeueItem(TKey key);

        /// <summary>Applies a clear buffer command.</summary>
        void ApplyClearBuffer(TKey key);

        /// <summary>Applies a remove buffer command.</summary>
        void ApplyRemoveBuffer(TKey key);

        /// <summary>Applies a clear all command.</summary>
        void ApplyClearAll();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset();
    }
}
