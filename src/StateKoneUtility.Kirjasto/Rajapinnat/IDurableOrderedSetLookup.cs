namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableOrderedSetLookup<TKey, TValue> :
    IEnumerable<(TKey, IReadOnlyCollection<TValue>)>,
    IReadOnlyCollection<(TKey, IReadOnlyCollection<TValue>)>
        where TKey : notnull
    {
        /// <summary>
        /// Gets a collection containing all unique keys in the lookup.
        /// </summary>
        IReadOnlyCollection<TKey> Keys { get; }

        /// <summary>
        /// Gets a read-only collection of values associated with the specified key, in the order they were added.
        /// </summary>
        /// <param name="key">The key of the values to get.</param>
        /// <returns>A read-only collection of values for the specified key. If the key is not found, an empty collection is returned.</returns>
        IReadOnlyCollection<TValue> this[TKey key] { get; }

        /// <summary>
        /// Determines whether the lookup contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the lookup.</param>
        /// <returns><c>true</c> if the lookup contains an entry for the specified key; otherwise, <c>false</c>.</returns>
        bool Contains(TKey key);

        /// <summary>
        /// Determines if a specific value exists for the given key.
        /// </summary>
        /// <param name="key">The key to look under.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> if the value is found for the given key; otherwise, <c>false</c>.</returns>
        bool Contains(TKey key, TValue value);

        /// <summary>
        /// Adds the specified value to the ordered set associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the set to add the value to.</param>
        /// <param name="value">The value to add.</param>
        /// <returns><c>true</c> if the value was added to the set for the specified key; <c>false</c> if the value was already present.</returns>
        bool Add(TKey key, TValue value);

        /// <summary>
        /// Removes the specified key and all its associated values from the lookup.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(TKey key);

        /// <summary>
        /// Removes the specified value from the ordered set associated with the specified key.
        /// If the set becomes empty after removal, the key is also removed from the lookup.
        /// </summary>
        /// <param name="key">The key of the set to remove the value from.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if the value was found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(TKey key, TValue value);

        /// <summary>
        /// Removes all keys and values from the lookup.
        /// </summary>
        void Clear();
    }
}
