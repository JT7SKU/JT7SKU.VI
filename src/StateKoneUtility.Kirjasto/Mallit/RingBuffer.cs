using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class RingBuffer<T> : IEnumerable<T>
    {
        private int _head = 0;  // Points to the index where the next item will be written.
        private int _tail = 0;  // Points to the index where the next item will be read.
        private int _count = 0;

        private T[] _buffer = new T[1]; // Users are supposed to set their desired capacity!

        public int Capacity => _buffer.Length;
        public int Count => _count;
        public bool IsEmpty => _count == 0;
        public bool IsFull => _count == Capacity;

        public bool Contains(T item)
        {
            if (IsEmpty)
            {
                return false;
            }

            var span = new ReadOnlySpan<T>(_buffer);

            // Case 1: The items are in a single contiguous block: [_, _, T, T, H, _]
            if (_tail < _head)
            {
                return Exists(span, _tail, _count, item);
            }

            // Case 2: The items are wrapped around the end of the buffer: [T, T, H, _, T]
            // This also handles the case where the buffer is full (_head == _tail).

            // We search the first segment (from the tail to the end of the buffer).
            if (Exists(span, _tail, Capacity - _tail, item))
            {
                return true;
            }

            // Otherwise, we search the second segment (from the start of the buffer up to the head).
            return Exists(span, 0, _head, item);

            static bool Exists(ReadOnlySpan<T> buffer, int index, int segmentLength, T item)
            {
                var comparer = EqualityComparer<T>.Default;
                var end = index + segmentLength;

                for (int i = index; i < end; i++)
                {
                    if (comparer.Equals(buffer[i], item))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Enqueue(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % Capacity;

            if (IsFull)
            {
                _tail = (_tail + 1) % Capacity; // This means that the head has overwritten the tail.
            }
            else
            {
                _count++;
            }
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T item)
        {
            if (IsEmpty)
            {
                item = default;
                return false;
            }

            item = _buffer[_tail];

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _buffer[_tail] = default!; // To avoid a potential memory leak.
            }

            _tail = (_tail + 1) % Capacity;
            _count--;

            return true;
        }

        public bool SetCapacity(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));

            if (capacity == Capacity)
            {
                return false;
            }

            var newBuffer = new T[capacity];
            var itemsToKeep = Math.Min(_count, capacity);

            if (itemsToKeep > 0)
            {
                // We calculate the start index of the newest items by skipping the oldest ones.

                var oldestItemsToSkip = _count - itemsToKeep;
                var startOfNewestItemsIndex = (_tail + oldestItemsToSkip) % Capacity;

                var source = _buffer.AsSpan();
                var destination = newBuffer.AsSpan();

                if (startOfNewestItemsIndex < _head || _head == 0)
                {
                    // The newest items are in a single contiguous block (in the old buffer).
                    source.Slice(startOfNewestItemsIndex, itemsToKeep).CopyTo(destination);
                }
                else
                {
                    // The newest items are split into two segments.
                    // e.g., [4, 5, 1, 2, 3] where tail=2, head=2, count=5. 
                    // If we want to keep 3 items (3 , 4, 5), start index is (2 + 5 - 3) % 5 = 4.
                    // Items are at index 4, and then wraps to 0 and 1.

                    // First segment: from the calculated start to the end of the old buffer.
                    var rightSegmentLength = Capacity - startOfNewestItemsIndex;
                    source.Slice(startOfNewestItemsIndex, rightSegmentLength).CopyTo(destination);

                    // Second segment: from the beginning of the old buffer.
                    var leftSegmentLength = itemsToKeep - rightSegmentLength;
                    source.Slice(0, leftSegmentLength).CopyTo(destination.Slice(rightSegmentLength));
                }
            }

            _buffer = newBuffer;
            _count = itemsToKeep;
            _tail = 0;
            _head = (itemsToKeep == capacity) ? 0 : itemsToKeep;

            return true;
        }

        public int CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            return CopyTo(array.AsSpan(arrayIndex));
        }

        public int CopyTo(Span<T> destination)
        {
            var count = Math.Min(destination.Length, _count);
            if (count == 0)
            {
                return 0;
            }

            var source = new ReadOnlySpan<T>(_buffer);

            // The items can be either in one contiguous or two disjoint segments.
            if (_tail < _head)
            {
                // The items are in a contiguous segment: [_, _, T..H, _, _]
                source.Slice(_tail, count).CopyTo(destination);
            }
            else
            {
                // The items are in two segments: [H.., _, ..T], in other words they are wrapped around the end.
                var rightSegment = source.Slice(_tail, Capacity - _tail);
                var leftSegment = source.Slice(0, _head);

                if (rightSegment.Length >= count)
                {
                    rightSegment.Slice(0, count).CopyTo(destination);
                }
                else
                {
                    rightSegment.CopyTo(destination);
                    leftSegment.Slice(0, count - rightSegment.Length).CopyTo(destination.Slice(rightSegment.Length));
                }
            }
            return count;
        }

        public bool Clear()
        {
            if (_count == 0)
            {
                return false;
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                // To avoid a potential memory leak, we clear the array, which should tell the
                // GC we no longer are clinging to the references of the items we hold in the buffer.
                // This is only valid for reference types or structs holding onto other reference types.

                Array.Clear(_buffer, 0, Capacity);
            }

            _head = 0;
            _tail = 0;
            _count = 0;

            return true;
        }

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator(RingBuffer<T> buffer) : IEnumerator<T>
        {
            private int _index = -1;
            [AllowNull] private T _current = default;

            public readonly T Current => _current;
            readonly object? IEnumerator.Current => Current;

            readonly void IDisposable.Dispose() { }

            public void Reset()
            {
                _index = -1;
                _current = default;
            }

            public bool MoveNext()
            {
                if (buffer._buffer is null || _index >= buffer.Count - 1)
                {
                    _index = buffer.Count;
                    _current = default;

                    return false;
                }

                _index++;

                var actualIndex = (buffer._tail + _index) % buffer.Capacity;

                _current = buffer._buffer[actualIndex];

                return true;
            }
        }
    }
}
