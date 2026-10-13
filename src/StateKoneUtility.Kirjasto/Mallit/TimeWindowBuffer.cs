using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class TimeWindowBuffer<T> : IEnumerable<T>
    {
        private readonly Queue<(T Item, long Timestamp)> _buffer = new();

        public int Count => _buffer.Count;
        public bool IsEmpty => _buffer.Count == 0;
        public long WindowSeconds { get; private set; } = 60 * 60; // We default to 1 hour

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfTooShortWindow(long windowSeconds)
        {
            if (windowSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(windowSeconds), "Window must be at least 1 second.");
            }
        }

        public bool Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            foreach (var (current, _) in _buffer)
            {
                if (comparer.Equals(current, item))
                {
                    return true;
                }
            }

            return false;
        }

        public void Enqueue(T item, long timestamp)
        {
            _buffer.Enqueue((item, timestamp));
            PurgeOldItems(timestamp);
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T item)
        {
            if (_buffer.TryDequeue(out var entry))
            {
                item = entry.Item;
                return true;
            }

            item = default;
            return false;
        }

        public bool SetWindow(long windowSeconds, long currentTimestamp)
        {
            ThrowIfTooShortWindow(windowSeconds);

            if (windowSeconds == WindowSeconds)
            {
                return false;
            }

            WindowSeconds = windowSeconds;
            PurgeOldItems(currentTimestamp);

            return true;
        }

        public int CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            return CopyTo(array.AsSpan(arrayIndex));
        }

        public int CopyTo(Span<T> destination)
        {
            var count = Math.Min(destination.Length, _buffer.Count);
            if (count == 0)
            {
                return 0;
            }

            int i = 0;

            foreach (var (item, _) in _buffer)
            {
                if (i >= count)
                {
                    break;
                }

                destination[i++] = item;
            }

            return count;
        }

        public bool Clear()
        {
            if (_buffer.Count == 0)
            {
                return false;
            }

            _buffer.Clear();

            return true;
        }

        private void PurgeOldItems(long currentTimestamp)
        {
            var evictionThreshold = currentTimestamp - WindowSeconds;

            while (_buffer.TryPeek(out var entry) && entry.Timestamp < evictionThreshold)
            {
                _buffer.Dequeue();
            }
        }

        /// <summary>
        /// Returns an enumerable of the raw entries, including timestamps.
        /// </summary>
        /// <remarks>Used for snapshotting.</remarks>
        public IEnumerable<(T Item, long TimestampSeconds)> GetEntries() => _buffer;

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var (item, _) in _buffer)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
