using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{

    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, IsEmpty = {IsEmpty}, IsFull = {IsFull}")]
    internal sealed class DurableRingBuffer<T> :
     IDurableRingBuffer<T>,
     IDurableRingBufferCommandHandler<T>,
     IStateMachine
    {
        private JournalStreamWriter? _writer;
        private readonly RingBuffer<T> _buffer = new();
        private readonly IDurableRingBufferCommandCodec<T> _codec;

        public DurableRingBuffer(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableRingBufferCommandCodec<T>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _buffer.Count;
        public int Capacity => _buffer.Capacity;
        public bool IsEmpty => _buffer.IsEmpty;
        public bool IsFull => _buffer.IsFull;

        #region IStateMachine


        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We use a push model, and append entries upon modification.
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter) => _codec.WriteSnapshot(Capacity, Count, _buffer, snapshotWriter);

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            _buffer.Clear();
            _writer = writer;
        }

        #endregion

        #region IDurableRingBufferCommandHandler

        void IDurableRingBufferCommandHandler<T>.ApplySetCapacity(int capacity) => _buffer.SetCapacity(capacity);
        void IDurableRingBufferCommandHandler<T>.ApplyEnqueue(T item) => _buffer.Enqueue(item);
        void IDurableRingBufferCommandHandler<T>.ApplyTryDequeue() => _buffer.TryDequeue(out _);
        void IDurableRingBufferCommandHandler<T>.ApplyClear() => _buffer.Clear();
        void IDurableRingBufferCommandHandler<T>.Reset(int capacity)
        {
            _buffer.Clear();
            _buffer.SetCapacity(capacity);
        }

        #endregion

        public bool SetCapacity(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));

            if (capacity == _buffer.Capacity)
            {
                return false;
            }

            _codec.WriteSetCapacity(capacity, GetWriter());
            _buffer.SetCapacity(capacity);

            return true;
        }

        public void Enqueue(T item)
        {
            _codec.WriteEnqueue(item, GetWriter());
            _buffer.Enqueue(item);
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T item)
        {
            if (_buffer.IsEmpty)
            {
                item = default;
                return false;
            }

            _codec.WriteDequeue(GetWriter());
            return _buffer.TryDequeue(out item);
        }

        public void Clear()
        {
            if (_buffer.IsEmpty)
            {
                return;
            }

            _codec.WriteClear(GetWriter());
            _buffer.Clear();
        }

        public bool Contains(T item) => _buffer.Contains(item);
        public int CopyTo(T[] array, int arrayIndex) => _buffer.CopyTo(array, arrayIndex);
        public int CopyTo(Span<T> destination) => _buffer.CopyTo(destination);

        public int DrainTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            return DrainTo(array.AsSpan(arrayIndex));
        }

        public int DrainTo(Span<T> destination)
        {
            var count = CopyTo(destination);
            if (count > 0)
            {
                Clear(); // We durably log this by means of clearing the buffer.
            }

            return count;
        }

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => _buffer.GetEnumerator();
    }
}
