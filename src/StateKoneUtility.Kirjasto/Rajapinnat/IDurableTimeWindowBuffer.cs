using System.Diagnostics.CodeAnalysis;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTimeWindowBuffer<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Gets the duration that items are retained in the buffer.
        /// </summary>
        TimeSpan Window { get; }

        /// <summary>
        /// Gets a value indicating whether the buffer is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Sets the time window for the buffer.
        /// If the new window is smaller than the current one, older items may be discarded immediately.
        /// </summary>
        /// <param name="window">The desired new time window.</param>
        /// <returns><c>true</c> if the time window is different from the provided <paramref name="window"/>, which in turn becomes the new window; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if time window is zero or negative.</exception>
        bool SetWindow(TimeSpan window);

        /// <summary>
        /// Adds an item to the buffer with the current timestamp. Any items older than the time window are removed.
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
