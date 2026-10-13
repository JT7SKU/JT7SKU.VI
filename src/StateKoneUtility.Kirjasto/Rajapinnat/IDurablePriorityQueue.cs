using System.Diagnostics.CodeAnalysis;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurablePriorityQueue<TElement, TPriority> :
  IEnumerable<(TElement, TPriority)>,
  IReadOnlyCollection<(TElement, TPriority)>
    {
        /// <summary>
        /// Removes all items from the priority queue.
        /// </summary>
        void Clear();

        /// <summary>
        /// Returns the element with the lowest priority from the priority queue without removing it.
        /// </summary>
        /// <returns>The element with the lowest priority in the queue.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
        TElement Peek();

        /// <summary>
        /// Tries to return the element with the lowest priority from the queue without removing it.
        /// </summary>
        /// <param name="element">When this method returns, contains the object with the lowest priority, if the operation was successful; otherwise, the default value of TElement.</param>
        /// <param name="priority">When this method returns, contains the priority of the element, if the operation was successful; otherwise, the default value of TPriority.</param>
        /// <returns><c>true</c> if there was an element to peek; <c>false</c> if the queue was empty.</returns>
        bool TryPeek([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority);

        /// <summary>
        /// Adds the specified element with its associated priority to the priority queue.
        /// </summary>
        /// <param name="element">The element to add to the queue.</param>
        /// <param name="priority">The priority of the element to add.</param>
        void Enqueue(TElement element, TPriority priority);

        /// <summary>
        /// Tries to remove and return the element with the lowest priority from the priority queue.
        /// </summary>
        /// <param name="element">When this method returns, contains the object with the lowest priority that was removed, if the operation was successful; otherwise, the default value of TElement.</param>
        /// <param name="priority">When this method returns, contains the priority of the element that was removed, if the operation was successful; otherwise, the default value of TPriority.</param>
        /// <returns><c>true</c> if an element was successfully removed; <c>false</c> if the queue was empty.</returns>
        bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority);

        /// <summary>
        /// Removes and returns the element with the lowest priority from the priority queue.
        /// </summary>
        /// <returns>The element that was removed from the queue.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
        TElement Dequeue();
    }
}
