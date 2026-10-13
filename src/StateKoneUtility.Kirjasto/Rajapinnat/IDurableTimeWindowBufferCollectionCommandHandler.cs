namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>
    {
        /// <summary>Applies a set window command.</summary>
        void ApplySetWindow(TKey key, long windowSeconds);

        /// <summary>Applies an enqueue item command.</summary>
        void ApplyEnqueueItem(TKey key, TValue item, long timestamp);

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
