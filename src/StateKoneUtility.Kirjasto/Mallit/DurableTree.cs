using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{
    [DebuggerDisplay("Count = {Count}, IsEmpty = {IsEmpty}")]
    internal sealed class DurableTree<T>: IDurableTree<T> IDurableTreeCommandHandler<T>, IStateMachine where T : notnull
    {
        private JournalStreamWriter? _writer;
        private T? _root;
        private readonly Dictionary<T, Node> _nodes = [];
        private readonly IDurableTreeCommandCodec<T> _codec;

        public DurableTree(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableTreeCommandCodec<T>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _nodes.Count;
        public bool IsEmpty => _root is null;
        public T Root => _root ?? throw new InvalidOperationException("The tree is empty.");

        #region IStateMachine

        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We use a push model, and append entries upon modification.
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter)
        {
            if (_nodes.Count == 0)
            {
                _codec.WriteSnapshot(0, default!, [], snapshotWriter);
            }
            else
            {
                _codec.WriteSnapshot(Count, Root, GetEdges(), snapshotWriter);
            }
        }

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            ApplyClear();
            _writer = writer;
        }

        #endregion

        #region IDurableTreeCommandHandler

        void IDurableTreeCommandHandler<T>.ApplySetRoot(T root) => ApplySetRoot(root);
        void IDurableTreeCommandHandler<T>.ApplyAdd(T parent, T value) => ApplyAdd(parent, value);
        void IDurableTreeCommandHandler<T>.ApplyRemove(T value) => ApplyRemove(value);
        void IDurableTreeCommandHandler<T>.ApplyMove(T value, T parent) => ApplyMove(value, parent);
        void IDurableTreeCommandHandler<T>.ApplyClear() => ApplyClear();
        void IDurableTreeCommandHandler<T>.Reset() => ApplyClear();

        #endregion

        public void SetRoot(T value)
        {
            if (!EqualityComparer<T>.Default.Equals(_root, default) || _nodes.Count > 0)
            {
                throw new InvalidOperationException("A root node has already been set.");
            }

            if (_nodes.ContainsKey(value))
            {
                throw new ArgumentException("A node with the same value already exists.", nameof(value));
            }

            _codec.WriteSetRoot(value, GetWriter());
            ApplySetRoot(value);
        }

        public void Add(T parent, T value)
        {
            if (!_nodes.ContainsKey(parent))
            {
                throw new ArgumentException("The parent node does not exist.", nameof(parent));
            }

            if (_nodes.ContainsKey(value))
            {
                throw new ArgumentException("A node with the same value already exists.", nameof(value));
            }

            _codec.WriteAdd(parent, value, GetWriter());
            ApplyAdd(parent, value);
        }

        public bool Remove(T value)
        {
            if (!_nodes.ContainsKey(value))
            {
                return false;
            }

            _codec.WriteRemove(value, GetWriter());
            ApplyRemove(value);

            return true;
        }

        public bool Move(T value, T parent)
        {
            if (!Contains(value) || !Contains(parent) || EqualityComparer<T>.Default.Equals(value, parent))
            {
                return false;
            }

            if (IsDescendantOf(parent, value))
            {
                throw new InvalidOperationException("Cannot move a node to be a child of one of its own descendants.");
            }

            if (EqualityComparer<T>.Default.Equals(Root, value) && Count > 1)
            {
                throw new InvalidOperationException("Cannot move the root node.");
            }

            if (!_nodes.TryGetValue(value, out var nodeToMoveData) || nodeToMoveData.Parent is null)
            {
                return false;
            }

            var oldParent = nodeToMoveData.Parent;
            if (EqualityComparer<T>.Default.Equals(oldParent, parent))
            {
                return false;
            }

            _codec.WriteMove(value, parent, GetWriter());
            ApplyMove(value, parent);

            return true;
        }

        public void Clear()
        {
            if (_nodes.Count == 0)
            {
                return;
            }

            _codec.WriteClear(GetWriter());
            ApplyClear();
        }

        public bool Contains(T value) => _nodes.ContainsKey(value);

        public bool TryGetParent(T value, [MaybeNullWhen(false)] out T parent)
        {
            if (_nodes.TryGetValue(value, out var node) && node.Parent is not null)
            {
                parent = node.Parent;
                return true;
            }

            parent = default;
            return false;
        }

        public IReadOnlyCollection<T> GetChildren(T value)
        {
            if (_nodes.TryGetValue(value, out var node))
            {
                return node.Children;
            }

            return [];
        }

        // We return a lazy evaluated enumerable here, because we have to traverse the tree to find the descendants.
        public IEnumerable<T> GetDescendants(T value)
        {
            if (!_nodes.TryGetValue(value, out Node? node))
            {
                yield break;
            }

            using var queue = new ValueQueue();

            foreach (var child in node.Children)
            {
                queue.Enqueue(child);
            }

            while (queue.TryDequeue(out var current))
            {
                yield return current;

                foreach (var grandChild in _nodes[current].Children)
                {
                    queue.Enqueue(grandChild);
                }
            }
        }

        private void ApplySetRoot(T root)
        {
            _root = root;
            _nodes[root] = new Node(default);
        }

        private void ApplyAdd(T parentValue, T newValue)
        {
            _nodes[parentValue].Children.Add(newValue);
            _nodes[newValue] = new Node(parentValue);
        }

        private void ApplyRemove(T value)
        {
            if (!_nodes.TryGetValue(value, out var nodeToRemove))
            {
                return;
            }

            // Remove node from its parent's children list.
            if (nodeToRemove.Parent is T parent)
            {
                _nodes[parent].Children.Remove(value);
            }

            // Collect all descendants and remove them.
            using var queue = new ValueQueue();

            queue.Enqueue(value);

            while (queue.TryDequeue(out var current))
            {
                if (_nodes.TryGetValue(current, out var data))
                {
                    foreach (var child in data.Children)
                    {
                        queue.Enqueue(child);
                    }

                    _nodes.Remove(current);
                }
            }

            // If the root was removed, clear it.
            if (EqualityComparer<T>.Default.Equals(_root, value))
            {
                _root = default;
            }
        }

        private void ApplyMove(T valueToMove, T newParentValue)
        {
            if (!_nodes.TryGetValue(valueToMove, out var nodeToMoveData) || nodeToMoveData.Parent is null)
            {
                return; // Cannot move root or non just an non-existent node.
            }

            var oldParent = nodeToMoveData.Parent;
            if (EqualityComparer<T>.Default.Equals(oldParent, newParentValue))
            {
                return; // No change, so skip.
            }

            // Unlink from old parent
            _nodes[oldParent].Children.Remove(valueToMove);

            // Link to new parent
            nodeToMoveData.Parent = newParentValue;
            _nodes[newParentValue].Children.Add(valueToMove);
        }

        private void ApplyClear()
        {
            _root = default;
            _nodes.Clear();
        }

        private bool IsDescendantOf(T potentialDescendant, T potentialAncestor)
        {
            using var queue = new ValueQueue();

            queue.Enqueue(potentialAncestor);

            var comparer = EqualityComparer<T>.Default;

            while (queue.TryDequeue(out var current))
            {
                if (comparer.Equals(current, potentialDescendant))
                {
                    return true;
                }

                if (_nodes.TryGetValue(current, out var data))
                {
                    foreach (var child in data.Children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return false;
        }

        private IEnumerable<(T Child, T Parent)> GetEdges()
        {
            // We use BFS (Breadth-First Search) to serialize all other nodes, guaranteeing parent-before-child order.
            // We need to serialize the tree into a flat, linear stream of bytes. To rebuild it correctly, a parent node
            // must always be processed before its children. If we process a child first, the ApplySnapshot method will fail
            // when it tries to link that child to a parent that does not exist yet in the reconstructed tree.

            using var queue = new ValueQueue();

            queue.Enqueue(Root);

            while (queue.TryDequeue(out var current))
            {
                var node = _nodes[current];

                foreach (var child in node.Children)
                {
                    yield return (child, current);
                    queue.Enqueue(child);
                }
            }
        }

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }

        public IEnumerator<T> GetEnumerator() => _nodes.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Node(T? parent)
        {
            public T? Parent { get; set; } = parent;
            public List<T> Children { get; } = [];
        }

        /// <summary>
        /// A high-performance, minimal-allocation queue implemented as a struct.
        /// It is designed for short-lived, high-frequency operations like the
        /// breadth-first traversals within this class.
        /// </summary>
        /// <remarks>Must be disposed to return the array to the pool.</remarks>
        private struct ValueQueue : IDisposable
        {
            private int _head;
            private int _tail;
            private int _count;
            private T[] _array;

            public ValueQueue()
            {
                _array = ArrayPool<T>.Shared.Rent(4);
            }

            public void Enqueue(T item)
            {
                if (_count == _array.Length)
                {
                    Grow();
                }

                _array[_tail] = item;
                _tail = (_tail + 1) % _array.Length;
                _count++;
            }

            public bool TryDequeue([MaybeNullWhen(false)] out T item)
            {
                if (_count == 0)
                {
                    item = default;
                    return false;
                }

                item = _array[_head];

                _array[_head] = default!; // Clear the slot to allow the GC to collect the object if it is a reference type.
                _head = (_head + 1) % _array.Length;
                _count--;

                return true;
            }

            private void Grow()
            {
                var newArray = ArrayPool<T>.Shared.Rent(_array.Length * 2);

                if (_count > 0)
                {
                    // The buffer can be in two states:
                    // 1. Contiguous: [_ _, H, I, J, K, T, _, _] where H=head, T=tail.
                    // 2. Wrapped:   [J, K, T, _, _, H, I, _,] where the items wrap around the end.
                    if (_head < _tail)
                    {
                        // The items are in a contiguous block.
                        Array.Copy(_array, _head, newArray, 0, _count);
                    }
                    else
                    {

                        int segment = _array.Length - _head;

                        // The items are in two segments. We copy both to the new array.
                        Array.Copy(_array, _head, newArray, 0, segment);
                        Array.Copy(_array, 0, newArray, segment, _tail);
                    }
                }

                ArrayPool<T>.Shared.Return(_array, clearArray: true);

                _head = 0;
                _tail = _count; // After copying, the new tail is simply the number of items.
                _array = newArray;
            }

            public void Dispose()
            {
                if (_array != null)
                {
                    ArrayPool<T>.Shared.Return(_array, clearArray: true);
                    _array = null!; // To make extra sure no reference is kept held.
                }
            }
        }
    }

}


