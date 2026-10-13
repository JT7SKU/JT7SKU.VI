namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableRingBufferCollection<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Gets the number of ring buffers in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a reado-only collection containing the keys of the ring buffers.
        /// </summary>
        IReadOnlyCollection<TKey> Keys { get; }

        /// <summary>
        /// Ensures that a ring buffer associated with the specified key exists and is configured with the given capacity.
        /// </summary>
        /// <param name="key">The key of the ring buffer to ensure.</param>
        /// <param name="capacity">The desired capacity for the ring buffer.</param>
        /// <returns>A durable proxy to the ring buffer, which will have the specified capacity after this call.</returns>
        /// <remarks>
        /// <para>
        /// This method provides a convenient way to get a buffer and set its capacity in a single, atomic operation.
        /// If a buffer for the given <paramref name="key"/> does not exist, it will be created with the specified <paramref name="capacity"/>.
        /// If the buffer already exists, its capacity will be overwritten with the new value (if different).
        /// </para>
        /// <para><strong>Decreasing the capacity on an existing buffer may result in data loss, if the number of items currently in the buffer exceeds the new capacity.</strong></para>
        /// </remarks>
        IDurableRingBuffer<TValue> EnsureBuffer(TKey key, int capacity);

        /// <summary>
        /// Determines whether the collection contains a ring buffer with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> if the collection contains a buffer with the key; otherwise, <c>false</c>.</returns>
        bool Contains(TKey key);

        /// <summary>
        /// Removes the ring buffer associated with the specified key from the collection.
        /// </summary>
        /// <param name="key">The key of the buffer to remove.</param>
        /// <returns><c>true</c> if the buffer was successfully found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(TKey key);

        /// <summary>
        /// Removes all buffers from the collection.
        /// </summary>
        void Clear();
    }
}
