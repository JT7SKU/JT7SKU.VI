using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTree<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Gets the value of the root node of the tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the tree is empty.</exception>
        T Root { get; }

        /// <summary>
        /// Gets a value indicating whether the tree is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Determines whether the tree contains a node with the specified value.
        /// </summary>
        /// <param name="value">The value to locate in the tree.</param>
        /// <returns><c>true</c> if a node with the specified value is found; otherwise, <c>false</c>.</returns>
        bool Contains(T value);

        /// <summary>
        /// Attempts to get the parent of the node with the specified value.
        /// </summary>
        /// <param name="value">The value of the node whose parent is to be found.</param>
        /// <param name="parent">When this method returns, contains the value of the parent node, if found; otherwise, the default value of T.</param>
        /// <returns><c>true</c> if the specified node has a parent; otherwise, <c>false</c>.</returns>
        bool TryGetParent(T value, [MaybeNullWhen(false)] out T parent);

        /// <summary>
        /// Gets a collection containing the direct children of the node with the specified value.
        /// </summary>
        /// <param name="value">The value of the parent node.</param>
        /// <returns>A read-only collection of the direct children. Returns an empty collection if the node has no children or does not exist.</returns>
        IReadOnlyCollection<T> GetChildren(T value);

        /// <summary>
        /// Gets an enumerable collection of all descendants (children, grandchildren, and so on) of the specified node.
        /// </summary>
        /// <param name="value">The value of the ancestor node.</param>
        /// <returns>An enumerable collection of all descendants.</returns>
        IEnumerable<T> GetDescendants(T value);

        /// <summary>
        /// Sets the root node of the tree. This can only be done when the tree is empty.
        /// </summary>
        /// <param name="value">The value to set as the root.</param>
        /// <exception cref="InvalidOperationException">Thrown if the tree is not empty.</exception>
        /// <exception cref="ArgumentException">Thrown if a node with the same value already exists.</exception>
        void SetRoot(T value);

        /// <summary>
        /// Adds a new node with the specified value as a child of an existing parent node.
        /// </summary>
        /// <param name="parent">The value of the parent node.</param>
        /// <param name="value">The value of the new node to add.</param>
        /// <exception cref="ArgumentException">Thrown if the parent node does not exist or if a node with the new value already exists.</exception>
        void Add(T parent, T value);

        /// <summary>
        /// Moves an existing node to become a child of a different parent node.
        /// </summary>
        /// <param name="value">The value of the node to move.</param>
        /// <param name="parent">The value of the new parent node.</param>
        /// <returns><c>true</c> if the node was successfully moved; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the move creates a cycle or if attempting to move the root node.</exception>
        bool Move(T value, T parent);

        /// <summary>
        /// Removes a node and all of its descendants from the tree.
        /// </summary>
        /// <param name="value">The value of the node to remove.</param>
        /// <returns><c>true</c> if the node was a found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(T value);

        /// <summary>
        /// Removes all nodes from the tree, making it empty.
        /// </summary>
        void Clear();
    }

}
