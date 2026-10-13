using System.Diagnostics.CodeAnalysis;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableRingBuffer<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Gets the maximum number of elements the buffer can hold.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets a value indicating whether the buffer is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets a value indicating whether the buffer is full.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Sets the total capacity of the buffer. 
        /// If the new capacity is smaller than the current number of items, the oldest items are discarded.
        /// </summary>
        /// <param name="capacity">The desired new capacity.</param>
        /// <returns><c>true</c> if the internal capacity is different from <paramref name="capacity"/>, which in turn becomes the new capacity; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is zero or negative.</exception>
        bool SetCapacity(int capacity);

        /// <summary>
        /// Adds an item to the buffer. If the buffer is full, the oldest item is overwritten.
        /// </summary>
        /// <param name="item">The item to add to the buffer.</param>
        void Enqueue(T item);

        /// <summary>
        /// Tries to remove and return the oldest object in the buffer.
        /// </summary>
        /// <param name="result">
        /// When this method returns, if the operation was successful, <paramref name="result"/> contains the object removed.
        /// If the buffer was empty, <paramref name="result"/> is the default value of <typeparamref name="T"/>.
        /// </param>
        /// <returns><c>true</c> if an element was removed and returned from the buffer successfully; otherwise, <c>false</c>.</returns>
        bool TryDequeue([MaybeNullWhen(false)] out T result);

        /// <summary>
        /// Determines whether the buffer contains a specific value.
        /// <para>
        /// The comparison is performed using <see cref="EqualityComparer{T}.Default"/>.
        /// </para>
        /// </summary>
        /// <param name="item">The object to locate in the buffer. The value can be <c>null</c> for reference types.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is found in the buffer; otherwise, <c>false</c>.</returns>
        bool Contains(T item);

        /// <summary>
        /// Copies the elements of the buffer to an array, starting at a particular array index.
        /// The elements are copied in their logical order (from oldest to newest).
        /// </summary>
        /// <param name="array">The destination array to copy items into.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        /// <returns>The number of items copied to the destination array.</returns>
        /// <exception cref="ArgumentNullException">If the array is <c>null</c></exception>
        int CopyTo(T[] array, int arrayIndex);

        /// <summary>
        /// Copies the elements of the buffer to a span.
        /// The elements are copied in their logical order (from oldest to newest).
        /// </summary>
        /// <param name="destination">The destination span to copy items into.</param>
        /// <returns>The number of items copied to the destination span.</returns>
        int CopyTo(Span<T> destination);

        /// <summary>
        /// Copies all elements to the destination array and then clears the buffer.
        /// The elements are copied in their logical order (from oldest to newest).
        /// </summary>
        /// <param name="array">The destination array to drain items into.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which draining begins.</param>
        /// <returns>The number of items drained to the destination array.</returns>
        /// <exception cref="ArgumentNullException">If the array is <c>null</c></exception>
        int DrainTo(T[] array, int arrayIndex);

        /// <summary>
        /// Copies all elements to the destination span and then clears the buffer.
        /// The elements are copied in their logical order (from oldest to newest).
        /// </summary>
        /// <param name="destination">The destination span to drain items into.</param>
        /// <returns>The number of items drained to the destination span.</returns>
        int DrainTo(Span<T> destination);

        /// <summary>
        /// Removes all items from the buffer.
        /// </summary>
        void Clear();
    }
}
