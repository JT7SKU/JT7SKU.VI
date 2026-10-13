
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed partial class DurableRingBufferCollection<TKey, TValue> where TKey : notnull
    {
        internal sealed class RingBufferProxy(TKey key, DurableRingBufferCollection<TKey, TValue> collection) : IDurableRingBuffer<TValue>
        {
            public RingBuffer<TValue> Buffer { get; } = new();

            public int Count => Buffer.Count;
            public int Capacity => Buffer.Capacity;
            public bool IsEmpty => Buffer.IsEmpty;
            public bool IsFull => Buffer.IsFull;

            public bool Contains(TValue item) => Buffer.Contains(item);
            public bool SetCapacity(int capacity)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));
                if (Buffer.Capacity == capacity)
                {
                    return false;
                }
                collection.SetBufferCapacity(key, capacity);
                return true;
            }

            public void Enqueue(TValue item) => collection.EnqueueItem(key, item);
            public bool TryDequeue([MaybeNullWhen(false)] out TValue item) => collection.TryDequeueItem(key, out item);
            public void Clear() => collection.ClearBuffer(key);

            public int DrainTo(TValue[] array, int arrayIndex)
            {
                ArgumentNullException.ThrowIfNull(array);
                return DrainTo(array.AsSpan(arrayIndex));
            }

            public int DrainTo(Span<TValue> destination)
            {
                var count = Buffer.CopyTo(destination);
                if (count > 0)
                {
                    Clear();
                }

                return count;
            }

            public int CopyTo(TValue[] array, int arrayIndex) => Buffer.CopyTo(array, arrayIndex);
            public int CopyTo(Span<TValue> destination) => Buffer.CopyTo(destination);

            public IEnumerator<TValue> GetEnumerator() => Buffer.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

}
