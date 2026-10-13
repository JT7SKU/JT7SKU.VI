using System;
using System.Collections.Generic;
using System.Text;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableListLookup<TKey, TValue> :
     IEnumerable<(TKey, IReadOnlyCollection<TValue>)>,
     IReadOnlyCollection<(TKey, IReadOnlyCollection<TValue>)>
         where TKey : notnull
    {
        /// <summary>
        /// Gets a collection containing all unique keys in the lookup.
        /// </summary>
        IReadOnlyCollection<TKey> Keys { get; }

        /// <summary>
        /// Gets a read-only collection of values associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the values to get.</param>
        /// <returns>A read-only collection of values for the specified key, preserving the order of insertion. If the key is not found, an empty collection is returned.</returns>
        IReadOnlyCollection<TValue> this[TKey key] { get; }

        /// <summary>
        /// Determines whether the lookup contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the lookup.</param>
        /// <returns><c>true</c> if the lookup contains a key that matches the specified key; otherwise, <c>false</c>.</returns>
        bool Contains(TKey key);

        /// <summary>
        /// Determines if a specific value exists for the given key.
        /// </summary>
        /// <param name="key">The key to look under.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> if the value is found for the given key; otherwise, <c>false</c>.</returns>
        bool Contains(TKey key, TValue value);

        /// <summary>
        /// Adds the specified value to the list associated with the specified key.
        /// Duplicate values are allowed.
        /// </summary>
        /// <param name="key">The key of the list to add the value to.</param>
        /// <param name="value">The value to add.</param>
        void Add(TKey key, TValue value);

        /// <summary>
        /// Adds a collection of values to the list associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the list to add the values to.</param>
        /// <param name="values">The collection of values to add.</param>
        void AddRange(TKey key, IEnumerable<TValue> values);

        /// <summary>
        /// Removes the specified key and all its associated values from the lookup.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(TKey key);

        /// <summary>
        /// Removes the first occurrence of the specified value from the list associated with the specified key.
        /// If the list becomes empty after removal, the key is also removed from the lookup.
        /// </summary>
        /// <param name="key">The key of the list to remove the value from.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if the value was found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(TKey key, TValue value);

        /// <summary>
        /// Removes all keys and values from the lookup.
        /// </summary>
        void Clear();
    }
}
