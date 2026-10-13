using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{
    [DebuggerDisplay("Count = {Count}")]
    internal sealed partial class DurableRingBufferCollection<TKey, TValue> :
    IDurableRingBufferCollection<TKey, TValue>,
    IDurableRingBufferCollectionCommandHandler<TKey, TValue>,
    IStateMachine
        where TKey : notnull
    {
        private JournalStreamWriter? _writer;
        private readonly Dictionary<TKey, RingBufferProxy> _proxies = [];
        private readonly IDurableRingBufferCollectionCommandCodec<TKey, TValue> _codec;

        public DurableRingBufferCollection(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableRingBufferCollectionCommandCodec<TKey, TValue>>(serviceProvider, options);
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

        #region IDurableRingBufferCollectionCommandHandler

        void IDurableRingBufferCollectionCommandHandler<TKey, TValue>.ApplySetCapacity(TKey key, int capacity) => GetOrCreateProxy(key).Buffer.SetCapacity(capacity);
        void IDurableRingBufferCollectionCommandHandler<TKey, TValue>.ApplyEnqueueItem(TKey key, TValue item) => GetOrCreateProxy(key).Buffer.Enqueue(item);
        void IDurableRingBufferCollectionCommandHandler<TKey, TValue>.ApplyTryDequeueItem(TKey key) => GetOrCreateProxy(key).Buffer.TryDequeue(out _);
        void IDurableRingBufferCollectionCommandHandler<TKey, TValue>.ApplyClearBuffer(TKey key) => GetOrCreateProxy(key).Buffer.Clear();
        void IDurableRingBufferCollectionCommandHandler<TKey, TValue>.ApplyRemoveBuffer(TKey key) => _proxies.Remove(key);
        void IDurableRingBufferCollectionCommandHandler<TKey, TValue>.ApplyClearAll() => _proxies.Clear();
        void IDurableRingBufferCollectionCommandHandler<TKey, TValue>.Reset() => _proxies.Clear();

        #endregion

        public IDurableRingBuffer<TValue> EnsureBuffer(TKey key, int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));

            var proxy = GetOrCreateProxy(key);
            if (proxy.Capacity != capacity)
            {
                _codec.WriteSetCapacity(key, capacity, GetWriter());
                proxy.Buffer.SetCapacity(capacity);
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

        internal void SetBufferCapacity(TKey key, int capacity)
        {
            var proxy = GetOrCreateProxy(key);
            if (proxy.Buffer.Capacity == capacity)
            {
                return;
            }

            _codec.WriteSetCapacity(key, capacity, GetWriter());
            proxy.Buffer.SetCapacity(capacity);
        }

        internal void EnqueueItem(TKey key, TValue item)
        {
            _codec.WriteEnqueueItem(key, item, GetWriter());
            GetOrCreateProxy(key).Buffer.Enqueue(item);
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
        internal RingBufferProxy GetOrCreateProxy(TKey key)
        {
            if (!_proxies.TryGetValue(key, out var proxy))
            {
                proxy = new RingBufferProxy(key, this);
                _proxies[key] = proxy;
            }

            return proxy;
        }

        private IEnumerable<(TKey Key, int Capacity, int Count, IEnumerable<TValue> Items)> GetSnapshotData()
        {
            foreach (var (key, proxy) in _proxies)
            {
                yield return (key, proxy.Buffer.Capacity, proxy.Buffer.Count, proxy.Buffer);
            }
        }

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }
    }

}
