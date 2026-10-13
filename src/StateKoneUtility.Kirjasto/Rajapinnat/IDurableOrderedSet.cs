using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableOrderedSet<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Provides efficient, zero-allocation, read-only access to the items in their original insertion order.
        /// </summary>
        /// <value>A read-only span containing all elements in the set, in order.</value>
        ReadOnlySpan<T> OrderedItems { get; }

        /// <summary>
        /// Determines whether the set contains a specific value.
        /// </summary>
        /// <param name="item">The item to locate in the set.</param>
        /// <returns><c>true</c> if the item is found in the set; otherwise, <c>false</c>.</returns>
        bool Contains(T item);

        /// <summary>
        /// Adds an element to the end of the ordered set.
        /// </summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns><c>true</c> if the element is added to the set; <c>false</c> if the element is already present.</returns>
        bool Add(T item);

        /// <summary>
        /// Removes the specified element from the ordered set.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns><c>true</c> if the element was successfully found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(T item);

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue);

        /// <summary>
        /// Copies the set to an existing array, starting at the specified array index.
        /// The elements copied preserve their original insertion order.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void CopyTo(T[] array, int arrayIndex);

        /// <summary>
        /// Removes all elements from the set.
        /// </summary>
        void Clear();
    }
}
