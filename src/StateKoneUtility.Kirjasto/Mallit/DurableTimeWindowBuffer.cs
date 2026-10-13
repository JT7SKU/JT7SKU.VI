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

    [DebuggerDisplay("Count = {Count}, IsEmpty = {IsEmpty}, Window = {Window}")]
    internal sealed class DurableTimeWindowBuffer<T> :
    IDurableTimeWindowBuffer<T>,
    IDurableTimeWindowBufferCommandHandler<T>,
    IStateMachine
    {
        private JournalStreamWriter? _writer;
        private readonly TimeWindowBuffer<T> _buffer = new();
        private readonly TimeProvider _timeProvider;
        private readonly IDurableTimeWindowBufferCommandCodec<T> _codec;

        public DurableTimeWindowBuffer(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, TimeProvider timeProvider,
            IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _timeProvider = timeProvider;
            _codec = Apulaiset.GetCodec<IDurableTimeWindowBufferCommandCodec<T>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _buffer.Count;
        public bool IsEmpty => _buffer.IsEmpty;
        public TimeSpan Window => TimeSpan.FromSeconds(_buffer.WindowSeconds);

        #region IStateMachine

        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We use a push model, and append entries upon modification.
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter) => _codec.WriteSnapshot(_buffer.WindowSeconds, Count, _buffer.GetEntries(), snapshotWriter);

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            _buffer.Clear();
            _writer = writer;
        }

        #endregion

        #region IDurableTimeWindowBufferCommandHandler

        void IDurableTimeWindowBufferCommandHandler<T>.ApplySetWindow(long windowSeconds) => _buffer.SetWindow(windowSeconds, _timeProvider.GetUtcNow().ToUnixTimeSeconds());
        void IDurableTimeWindowBufferCommandHandler<T>.ApplyEnqueue(T item, long timestamp) => _buffer.Enqueue(item, timestamp);
        void IDurableTimeWindowBufferCommandHandler<T>.ApplyTryDequeue() => _buffer.TryDequeue(out _);
        void IDurableTimeWindowBufferCommandHandler<T>.ApplyClear() => _buffer.Clear();
        void IDurableTimeWindowBufferCommandHandler<T>.Reset(long windowSeconds)
        {
            _buffer.Clear();
            _buffer.SetWindow(windowSeconds, _timeProvider.GetUtcNow().ToUnixTimeSeconds());
        }

        #endregion

        public bool SetWindow(TimeSpan window)
        {
            var windowSeconds = (long)window.TotalSeconds;
            TimeWindowBuffer<T>.ThrowIfTooShortWindow(windowSeconds);

            if (windowSeconds == _buffer.WindowSeconds)
            {
                return false;
            }

            _codec.WriteSetWindow(windowSeconds, GetWriter());
            _buffer.SetWindow(windowSeconds, _timeProvider.GetUtcNow().ToUnixTimeSeconds());

            return true;
        }

        public void Enqueue(T item)
        {
            var timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

            _codec.WriteEnqueue(item, timestamp, GetWriter());
            _buffer.Enqueue(item, timestamp);
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
