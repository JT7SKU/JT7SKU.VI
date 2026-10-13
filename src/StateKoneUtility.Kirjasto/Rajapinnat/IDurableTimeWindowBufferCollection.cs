namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTimeWindowBufferCollection<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Gets the number of window buffers in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a read-only collection containing the keys of the window buffers.
        /// </summary>
        IReadOnlyCollection<TKey> Keys { get; }

        /// <summary>
        /// Ensures that a window buffer associated with the specified key exists and is configured with the given time window.
        /// </summary>
        /// <param name="key">The key of the window buffer to ensure.</param>
        /// <param name="window">The desired time window for the buffer.</param>
        /// <returns>A durable proxy to the window buffer, which will have the specified window after this call.</returns>
        /// <remarks>
        /// <para>
        /// This method provides a convenient way to get a buffer and set its time window in a single, atomic operation.
        /// If a buffer for the given <paramref name="key"/> does not exist, it will be created with the specified <paramref name="window"/>.
        /// If the buffer already exists, its time window will be overwritten with the new value (if different).
        /// </para>
        /// <para><strong>Decreasing the time window on an existing buffer may result in data loss, if items fall outside the new (smaller) window.</strong></para>
        /// </remarks>
        IDurableTimeWindowBuffer<TValue> EnsureBuffer(TKey key, TimeSpan window);

        /// <summary>
        /// Determines whether the collection contains a window buffer with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> if the collection contains a buffer with the key; otherwise, <c>false</c>.</returns>
        bool Contains(TKey key);

        /// <summary>
        /// Removes the window buffer associated with the specified key from the collection.
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
