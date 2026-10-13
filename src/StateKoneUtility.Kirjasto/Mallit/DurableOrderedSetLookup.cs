using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{

    internal sealed class DurableOrderedSetLookup<TKey, TValue> :
     IDurableOrderedSetLookup<TKey, TValue>,
     IDurableOrderedSetLookupCommandHandler<TKey, TValue>,
     IStateMachine
         where TKey : notnull
    {
        private JournalStreamWriter? _writer;
        private readonly Dictionary<TKey, OrderedValueSet> _items = [];
        private readonly IDurableOrderedSetLookupCommandCodec<TKey, TValue> _codec;

        public DurableOrderedSetLookup(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableOrderedSetLookupCommandCodec<TKey, TValue>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _items.Count;
        public IReadOnlyCollection<TKey> Keys => _items.Keys;

        public IReadOnlyCollection<TValue> this[TKey key]
        {
            get
            {
                if (!_items.TryGetValue(key, out var set) || set.Count == 0)
                {
                    return ImmutableList<TValue>.Empty;
                }

                if (set.Count == 1)
                {
                    return [set.Single()];
                }

                return set.All();
            }
        }

        #region IStateMachine

       

        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We use a push model, and append entries upon modification.
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter) => _codec.WriteSnapshot(this, snapshotWriter);

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            ApplyClear();
            _writer = writer;
        }

        #endregion

        #region IDurableOrderedSetLookupCommandHandler

        void IDurableOrderedSetLookupCommandHandler<TKey, TValue>.ApplyAdd(TKey key, TValue value) => ApplyAdd(key, value);
        void IDurableOrderedSetLookupCommandHandler<TKey, TValue>.ApplyRemoveKey(TKey key) => ApplyRemoveKey(key);
        void IDurableOrderedSetLookupCommandHandler<TKey, TValue>.ApplyRemoveItem(TKey key, TValue value) => ApplyRemoveItem(key, value);
        void IDurableOrderedSetLookupCommandHandler<TKey, TValue>.ApplyClear() => ApplyClear();
        void IDurableOrderedSetLookupCommandHandler<TKey, TValue>.Reset(int capacityHint)
        {
            ApplyClear();
            _items.EnsureCapacity(capacityHint);
        }

        #endregion

        public bool Contains(TKey key) => _items.ContainsKey(key);

        public bool Contains(TKey key, TValue value) => _items.TryGetValue(key, out var set) && set.Contains(value);

        public bool Add(TKey key, TValue value)
        {
            if (Contains(key, value))
            {
                return false;
            }

            _codec.WriteAdd(key, value, GetWriter());
            ApplyAdd(key, value);

            return true;
        }

        public bool Remove(TKey key)
        {
            if (!Contains(key))
            {
                return false;
            }

            _codec.WriteRemoveKey(key, GetWriter());
            ApplyRemoveKey(key);

            return true;
        }

        public bool Remove(TKey key, TValue value)
        {
            if (!Contains(key, value))
            {
                return false;
            }

            _codec.WriteRemoveItem(key, value, GetWriter());
            ApplyRemoveItem(key, value);

            return true;
        }

        public void Clear()
        {
            _codec.WriteClear(GetWriter());
            ApplyClear();
        }

        private void ApplyAdd(TKey key, TValue value)
        {
            _items.TryGetValue(key, out var set);

            var updated = set.Add(value);

            if (!updated.Equals(set))
            {
                _items[key] = updated;
            }
        }

        private void ApplyRemoveItem(TKey key, TValue value)
        {
            if (!_items.TryGetValue(key, out var set))
            {
                return;
            }

            var updated = set.Remove(value);
            if (updated.Equals(set))
            {
                return;
            }

            if (updated.Count == 0)
            {
                _items.Remove(key);
            }
            else
            {
                _items[key] = updated;
            }
        }

        private void ApplyRemoveKey(TKey key) => _items.Remove(key);

        private void ApplyClear() => _items.Clear();

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }

        public IEnumerator<(TKey, IReadOnlyCollection<TValue>)> GetEnumerator()
        {
            foreach (var (key, _) in _items)
            {
                yield return (key, this[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Initializes a new <see cref="OrderedValueSet"/> holding either a single value or, once more than one
        /// element is added, a tuple containing an <see cref="ImmutableHashSet{TValue}"/> and an <see cref="ImmutableList{TValue}"/>.
        /// This composite structure provides both uniqueness with efficient lookups and preservation of insertion order.
        /// <list type="bullet">
        /// <item>
        /// Every mutation (Add/Remove) returns a fresh instance with structural sharing,
        /// ensuring efficient O(log n) updates without copying the entire collection on each change.
        /// </item>
        /// <item>
        /// The state machine can safely snapshot and replay history without risking out‑of‑band
        /// mutations or shared mutable state.
        /// </item>
        /// <item>
        /// We avoid unnecessary allocations for the very common single‐value case, falling back to the
        /// collection tuple only when a second unique element is added.
        /// </item>
        /// </list>
        /// </summary>
        private readonly struct OrderedValueSet(object? value) : IEnumerable<TValue>
        {
            /// <summary>
            /// Stores either a single value, or a tuple of (<see cref="ImmutableHashSet{TValue}"/>, <see cref="ImmutableList{TValue}"/>).
            /// We avoid allocating a collection for keys that only have one value associated with them, which is very common.
            /// </summary>
            internal readonly object? _value = value;

            private static readonly (ImmutableHashSet<TValue> Set, ImmutableList<TValue> List) Empty =
                (ImmutableHashSet<TValue>.Empty, ImmutableList<TValue>.Empty);

            public int Count
            {
                get
                {
                    if (_value is null)
                    {
                        return 0;
                    }

                    if (_value is not ValueTuple<ImmutableHashSet<TValue>, ImmutableList<TValue>> tuple)
                    {
                        return 1;
                    }

                    Debug.Assert(tuple.Item1.Count == tuple.Item2.Count);
                    return tuple.Item1.Count;
                }
            }

            public bool Contains(TValue value)
            {
                if (_value is null)
                {
                    return false;
                }

                // If we have multiple items, we use the hash set for efficient lookup.
                if (_value is ValueTuple<ImmutableHashSet<TValue>, ImmutableList<TValue>> tuple)
                {
                    return tuple.Item1.Contains(value);
                }

                // Otherwise, we are storing a single item.
                return EqualityComparer<TValue>.Default.Equals((TValue)_value, value);
            }

            public OrderedValueSet Add(TValue value)
            {
                // If the set is empty, we store the new value directly.
                if (_value is null)
                {
                    return new OrderedValueSet(value);
                }

                // If we are already storing multiple items.
                if (_value is ValueTuple<ImmutableHashSet<TValue>, ImmutableList<TValue>> tuple)
                {
                    // First we check for existence. If the value is already present, nothing changes.
                    if (tuple.Item1.Contains(value))
                    {
                        return this;
                    }

                    // Otherwise, we add the new value to both the set and the list.
                    return new OrderedValueSet((tuple.Item1.Add(value), tuple.Item2.Add(value)));
                }

                // If we are storing a single item.
                var singleValue = (TValue)_value;
                if (EqualityComparer<TValue>.Default.Equals(singleValue, value))
                {
                    // And it's the same as the new value, nothing changes.
                    return this;
                }

                // Otherwise, we upgrade from a single value to the collection tuple.

                var newSet = Empty.Set.Add(singleValue).Add(value);
                var newList = Empty.List.Add(singleValue).Add(value);

                return new OrderedValueSet((newSet, newList));
            }

            public OrderedValueSet Remove(TValue value)
            {
                // If the set is empty, there is nothing to remove.
                if (_value is null)
                {
                    return this;
                }

                // If we are storing multiple items.
                if (_value is ValueTuple<ImmutableHashSet<TValue>, ImmutableList<TValue>> tuple)
                {
                    // And the value isn't in the set, nothing changes.
                    if (!tuple.Item1.Contains(value))
                    {
                        return this;
                    }

                    // Otherwise, remove the value from both collections.
                    var newSet = tuple.Item1.Remove(value);
                    var newList = tuple.Item2.Remove(value);

                    // If more than one item remains, we store the updated tuple.
                    if (newList.Count > 1)
                    {
                        return new OrderedValueSet((newSet, newList));
                    }

                    // If exactly one item remains, we downgrade to the single-value optimization.
                    if (newList.Count == 1)
                    {
                        return new OrderedValueSet(newList[0]);
                    }

                    // If the set is now empty, we return the default instance.
                    return default;
                }

                // If we are storing a single item.
                var singleValue = (TValue)_value;
                if (!EqualityComparer<TValue>.Default.Equals(singleValue, value))
                {
                    // And it doesn't match the value to remove, nothing changes.
                    return this;
                }

                // Otherwise, the item matches, so we return an default.
                return default;
            }

            public ImmutableList<TValue> All()
            {
                Debug.Assert(_value != null);
                return ((ValueTuple<ImmutableHashSet<TValue>, ImmutableList<TValue>>)_value).Item2;
            }

            public TValue Single()
            {
                Debug.Assert(_value is TValue);
                return (TValue)_value;
            }

            public Enumerator GetEnumerator() => new(this);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Equals(OrderedValueSet other) => _value == other._value;

            public struct Enumerator : IEnumerator<TValue>
            {
                private int _count;
                [AllowNull] private readonly TValue _value;
                private ImmutableList<TValue>.Enumerator _enumerator;

                object? IEnumerator.Current => Current;
                public TValue Current => _count > 1 ? _enumerator.Current : _value;

                internal Enumerator(OrderedValueSet valueSet)
                {
                    if (valueSet._value is null)
                    {
                        _value = default;
                        _enumerator = default;
                        _count = 0;
                    }
                    else
                    {
                        if (valueSet._value is ValueTuple<ImmutableHashSet<TValue>, ImmutableList<TValue>> tuple)
                        {
                            _value = default;
                            _enumerator = tuple.Item2.GetEnumerator(); // We use the list enumerator since this needs to perserve order.

                            Debug.Assert(tuple.Item1.Count == tuple.Item2.Count);

                            _count = tuple.Item2.Count;

                            Debug.Assert(_count > 1);
                        }
                        else
                        {
                            _value = (TValue?)valueSet._value;
                            _enumerator = default;
                            _count = 1;
                        }
                    }
                }

                readonly void IDisposable.Dispose() { }
                void IEnumerator.Reset() => throw new NotSupportedException();

                public bool MoveNext()
                {
                    switch (_count)
                    {
                        case 0: return false;
                        case 1: _count = 0; return true;
                        default:
                            if (_enumerator.MoveNext())
                            {
                                return true;
                            }
                            _count = 0;
                            return false;
                    }
                }
            }
        }
    }
}
