using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{

    [DebuggerDisplay("Count = {Count}")]
    internal sealed class DurableTimeWindowBufferCollection<TKey, TValue> :
    IDurableTimeWindowBufferCollection<TKey, TValue>,
    IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>,
    IStateMachine
        where TKey : notnull
    {
        private JournalStreamWriter? _writer;
        private readonly TimeProvider _timeProvider;
        private readonly Dictionary<TKey, TimeWindowBufferProxy> _proxies = [];
        private readonly IDurableTimeWindowBufferCollectionCommandCodec<TKey, TValue> _codec;

        public DurableTimeWindowBufferCollection(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, TimeProvider timeProvider,
            IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _timeProvider = timeProvider;
            _codec = Apulaiset.GetCodec<IDurableTimeWindowBufferCollectionCommandCodec<TKey, TValue>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _proxies.Count;
        public IReadOnlyCollection<TKey> Keys => _proxies.Keys;

        #region IStateMachine

        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We use a push model, and append entries upon modification.
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter) => _codec.WriteSnapshot(_proxies.Count, GetSnapshotData(), snapshotWriter);

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            _proxies.Clear();
            _writer = writer;
        }

        #endregion

        #region IDurableTimeWindowBufferCollectionCommandHandler

        void IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>.ApplySetWindow(TKey key, long windowSeconds) => GetOrCreateProxy(key).Buffer.SetWindow(windowSeconds, _timeProvider.GetUtcNow().ToUnixTimeSeconds());
        void IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>.ApplyEnqueueItem(TKey key, TValue item, long timestamp) => GetOrCreateProxy(key).Buffer.Enqueue(item, timestamp);
        void IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>.ApplyTryDequeueItem(TKey key) => GetOrCreateProxy(key).Buffer.TryDequeue(out _);
        void IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>.ApplyClearBuffer(TKey key) => GetOrCreateProxy(key).Buffer.Clear();
        void IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>.ApplyRemoveBuffer(TKey key) => _proxies.Remove(key);
        void IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>.ApplyClearAll() => _proxies.Clear();
        void IDurableTimeWindowBufferCollectionCommandHandler<TKey, TValue>.Reset() => _proxies.Clear();

        #endregion

        public IDurableTimeWindowBuffer<TValue> EnsureBuffer(TKey key, TimeSpan window)
        {
            TimeWindowBuffer<TValue>.ThrowIfTooShortWindow((long)window.TotalSeconds);

            var proxy = GetOrCreateProxy(key);
            if (proxy.Window != window)
            {
                var windowSeconds = (long)window.TotalSeconds;

                _codec.WriteSetWindow(key, windowSeconds, GetWriter());
                proxy.Buffer.SetWindow(windowSeconds, _timeProvider.GetUtcNow().ToUnixTimeSeconds());
            }

            return proxy;
        }

        public bool Contains(TKey key) => _proxies.ContainsKey(key);

        public bool Remove(TKey key)
        {
            if (!_proxies.ContainsKey(key))
            {
                return false;
            }

            _codec.WriteRemoveBuffer(key, GetWriter());
            _proxies.Remove(key);

            return true;
        }

        public void Clear()
        {
            if (_proxies.Count == 0)
            {
                return;
            }

            _codec.WriteClearAll(GetWriter());
            _proxies.Clear();
        }

        internal void SetBufferWindow(TKey key, long windowSeconds)
        {
            var proxy = GetOrCreateProxy(key);
            if (proxy.Buffer.WindowSeconds == windowSeconds)
            {
                return;
            }

            _codec.WriteSetWindow(key, windowSeconds, GetWriter());
            proxy.Buffer.SetWindow(windowSeconds, _timeProvider.GetUtcNow().ToUnixTimeSeconds());
        }

        internal void EnqueueItem(TKey key, TValue item)
        {
            var timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

            _codec.WriteEnqueueItem(key, item, timestamp, GetWriter());
            GetOrCreateProxy(key).Buffer.Enqueue(item, timestamp);
        }

        internal bool TryDequeueItem(TKey key, [MaybeNullWhen(false)] out TValue item)
        {
            if (!_proxies.TryGetValue(key, out var proxy) || proxy.Buffer.IsEmpty)
            {
                item = default;
                return false;
            }

            _codec.WriteDequeueItem(key, GetWriter());
            return proxy.Buffer.TryDequeue(out item);
        }

        internal void ClearBuffer(TKey key)
        {
            if (!_proxies.TryGetValue(key, out var proxy) || proxy.Buffer.IsEmpty)
            {
                return;
            }

            _codec.WriteClearBuffer(key, GetWriter());
            proxy.Buffer.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TimeWindowBufferProxy GetOrCreateProxy(TKey key)
        {
            if (!_proxies.TryGetValue(key, out var proxy))
            {
                proxy = new TimeWindowBufferProxy(key, this);
                _proxies[key] = proxy;
            }

            return proxy;
        }

        private IEnumerable<(TKey Key, long WindowSeconds, int Count, IEnumerable<(TValue Item, long Timestamp)> Items)> GetSnapshotData()
        {
            foreach (var (key, proxy) in _proxies)
            {
                yield return (key, proxy.Buffer.WindowSeconds, proxy.Buffer.Count, proxy.Buffer.GetEntries());
            }
        }

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }

        internal sealed class TimeWindowBufferProxy(TKey key,
            DurableTimeWindowBufferCollection<TKey, TValue> collection) : IDurableTimeWindowBuffer<TValue>
        {
            public TimeWindowBuffer<TValue> Buffer { get; } = new();

            public int Count => Buffer.Count;
            public bool IsEmpty => Buffer.IsEmpty;
            public TimeSpan Window => TimeSpan.FromSeconds(Buffer.WindowSeconds);

            public bool Contains(TValue item) => Buffer.Contains(item);

            public bool SetWindow(TimeSpan window)
            {
                TimeWindowBuffer<TValue>.ThrowIfTooShortWindow((long)window.TotalSeconds);
                if (Window == window)
                {
                    return false;
                }
                collection.SetBufferWindow(key, (long)window.TotalSeconds);
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
