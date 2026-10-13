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

    internal sealed class DurableSetLookup<TKey, TValue> :
    IDurableSetLookup<TKey, TValue>,
    IDurableSetLookupCommandHandler<TKey, TValue>,
    IStateMachine
        where TKey : notnull
    {
        private JournalStreamWriter? _writer;
        private readonly Dictionary<TKey, ValueSet> _items = [];
        private readonly IDurableSetLookupCommandCodec<TKey, TValue> _codec;

        public DurableSetLookup(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableSetLookupCommandCodec<TKey, TValue>>(serviceProvider, options);
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
                    return ImmutableArray<TValue>.Empty;
                }

                if (set.Count == 1)
                {
                    return [set.Single()];
                }

                return [.. set];
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

        #region IDurableSetLookupCommandHandler

        void IDurableSetLookupCommandHandler<TKey, TValue>.ApplyAdd(TKey key, TValue value) => ApplyAdd(key, value);
        void IDurableSetLookupCommandHandler<TKey, TValue>.ApplyRemoveKey(TKey key) => ApplyRemoveKey(key);
        void IDurableSetLookupCommandHandler<TKey, TValue>.ApplyRemoveItem(TKey key, TValue value) => ApplyRemoveItem(key, value);
        void IDurableSetLookupCommandHandler<TKey, TValue>.ApplyClear() => ApplyClear();
        void IDurableSetLookupCommandHandler<TKey, TValue>.Reset(int capacityHint)
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
            if (_items.TryGetValue(key, out var set))
            {
                _items[key] = set.Add(value);
            }
            else
            {
                _items[key] = new ValueSet(value);
            }
        }

        private void ApplyRemoveItem(TKey key, TValue value)
        {
            if (!_items.TryGetValue(key, out ValueSet set))
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
        /// Initializes a new <see cref="ValueSet"/> holding either a single value or,
        /// once more than one element is added, an <see cref="ImmutableHashSet{TValue}"/>.
        /// We use <see cref="ImmutableHashSet{TValue}"/> rather than <see cref="HashSet{TValue}"/> so that:
        /// <list type="bullet">
        /// <item>
        /// Every mutation (Add/Insert/Remove) returns a fresh instance with structural sharing,
        /// ensuring efficient O(log n) updates without copying the entire collection on each change.
        /// </item>
        /// <item>
        /// The state machine can safely snapshot and replay history without risking out‑of‑band
        /// mutations or shared mutable state.
        /// </item>
        /// <item>
        /// We avoid unnecessary allocations for the very common single‐value case,
        /// falling back to an <see cref="ImmutableHashSet{TValue}"/> only when needed.
        /// </item>
        /// </list>
        /// </summary>
        private readonly struct ValueSet(object? value) : IEnumerable<TValue>
        {
            /// <summary>
            /// Stores either a single value or an <see cref="ImmutableHashSet{TValue}"/>, we avoid allocating a collection
            /// for keys that only have one value associated with them, which is very common.
            /// </summary>
            private readonly object? _value = value;

            public int Count
            {
                get
                {
                    if (_value is null)
                    {
                        return 0;
                    }

                    if (_value is not ImmutableHashSet<TValue> set)
                    {
                        return 1;
                    }

                    return set.Count;
                }
            }

            public bool Contains(TValue value)
            {
                if (_value is null)
                {
                    return false;
                }

                // If we have multiple items, we use the hash set for efficient lookup.
                if (_value is ImmutableHashSet<TValue> set)
                {
                    return set.Contains(value);
                }

                // Otherwise, we are storing a single item.
                return EqualityComparer<TValue>.Default.Equals((TValue)_value, value);
            }

            public ValueSet Add(TValue value)
            {
                if (_value is null)
                {
                    // No existing value, we store it directly to avoid set allocation.
                    return new ValueSet(value);
                }

                // Try to view the stored value as a set.
                var set = _value as ImmutableHashSet<TValue>;

                // If it is not a set, we must have exactly one item stored.
                if (set is null)
                {
                    // If the new value equals that single stored element, nothing changes.
                    if (ImmutableHashSet<TValue>.Empty.KeyComparer.Equals((TValue)_value, value))
                    {
                        return this;
                    }

                    // Otherwise, we create a new set containing both the old and new items.
                    set = [(TValue)_value];
                }

                return new ValueSet(set.Add(value));
            }

            public ValueSet Remove(TValue value)
            {
                // If there's no value stored, there's nothing to remove.
                if (_value is null)
                {
                    return this;
                }

                // Try to interpret the stored value as a set.
                if (_value is not ImmutableHashSet<TValue> set)
                {
                    // We must be storing a single item.
                    if (Comparer.Equals((TValue)_value, value))
                    {
                        // It matches the item to remove, so we clear the value entirely.
                        return new ValueSet(null);
                    }

                    // Otherwise, the item doesn't match and there's nothing to remove.
                    return this;
                }

                // Remove the value from the set.
                var newSet = set.Remove(value);

                // If the set is now empty, clear the stored value.
                if (newSet.Count == 0)
                {
                    return new ValueSet(null);
                }

                // If there's only one item left, simplify by storing the value directly.
                if (newSet.Count == 1)
                {
                    return new ValueSet(newSet.First());
                }

                // Otherwise, store the updated set.
                return new ValueSet(newSet);
            }

            public TValue Single()
            {
                Debug.Assert(_value is TValue);
                return (TValue)_value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            public Enumerator GetEnumerator() => new(this);

            public bool Equals(ValueSet other) => _value == other._value;

            public struct Enumerator : IEnumerator<TValue>
            {
                private int _count;
                [AllowNull] private readonly TValue _value;
                private ImmutableHashSet<TValue>.Enumerator _enumerator;

                object? IEnumerator.Current => Current;
                public TValue Current => _count > 1 ? _enumerator.Current : _value;

                internal Enumerator(ValueSet valueSet)
                {
                    if (valueSet._value is null)
                    {
                        _value = default;
                        _enumerator = default;
                        _count = 0;
                    }
                    else
                    {
                        if (valueSet._value is ImmutableHashSet<TValue> set)
                        {
                            _value = default;
                            _enumerator = set.GetEnumerator();
                            _count = set.Count;

                            Debug.Assert(_count > 1);
                        }
                        else
                        {
                            _value = (TValue?)valueSet._value;
                            _enumerator = default;
                            _count = 1;
                        }

                        Debug.Assert(_count == valueSet.Count);
                    }
                }

                readonly void IDisposable.Dispose() { }
                void IEnumerator.Reset() => throw new NotImplementedException();

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
