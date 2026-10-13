using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Text;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableStack<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Removes all objects from the stack.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether an element is in the stack.
        /// </summary>
        /// <param name="item">The object to locate in the stack. The value can be null for reference types.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is found in the stack; otherwise, <c>false</c>.</returns>
        bool Contains(T item);

        /// <summary>
        /// Copies the stack to an existing array, starting at the specified array index.
        /// The elements are copied from top to bottom.
        /// </summary>
        /// <param name="array">The destination  array.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void CopyTo(T[] array, int arrayIndex);

        /// <summary>
        /// Inserts an object at the top of the stack.
        /// </summary>
        /// <param name="item">The object to push onto the stack. The value can be null for reference types.</param>
        void Push(T item);

        /// <summary>
        /// Removes and returns the object at the top of the stack.
        /// </summary>
        /// <returns>The object removed from the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the stack is empty.</exception>
        T Pop();

        /// <summary>
        /// Tries to remove and return the object at the top of the stack.
        /// </summary>
        /// <param name="item">When this method returns, contains the object removed from the top of the stack, if the operation was successful; otherwise, the default value of T.</param>
        /// <returns><c>true</c> if an object was successfully removed and returned; <c>false</c> if the stack was empty.</returns>
        bool TryPop([MaybeNullWhen(false)] out T item);

        /// <summary>
        /// Returns the object at the top of the stack without removing it.
        /// </summary>
        /// <returns>The object at the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the stack is empty.</exception>
        T Peek();

        /// <summary>
        /// Tries to return the object at the top of the stack without removing it.
        /// </summary>
        /// <param name="item">When this method returns, contains the object at the top of the stack, if one exists; otherwise, the default value of T.</param>
        /// <returns><c>true</c> if there was an object at the top of the stack; <c>false</c> if the stack was empty.</returns>
        bool TryPeek([MaybeNullWhen(false)] out T item);
    }
}

