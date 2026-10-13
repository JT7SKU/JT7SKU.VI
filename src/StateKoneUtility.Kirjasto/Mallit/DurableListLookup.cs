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
    internal sealed class DurableListLookup<TKey, TValue> :
    IDurableListLookup<TKey, TValue>,
    IDurableListLookupCommandHandler<TKey, TValue>,
    IStateMachine
        where TKey : notnull
    {
        private JournalStreamWriter? _writer;
        private readonly Dictionary<TKey, ValueList> _items = [];
        private readonly IDurableListLookupCommandCodec<TKey, TValue> _codec;

        public DurableListLookup(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableListLookupCommandCodec<TKey, TValue>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _items.Count;
        public IReadOnlyCollection<TKey> Keys => _items.Keys;

        public IReadOnlyCollection<TValue> this[TKey key]
        {
            get
            {
                if (!_items.TryGetValue(key, out var list) || list.Count == 0)
                {
                    return ImmutableList<TValue>.Empty;
                }

                if (list.Count == 1)
                {
                    return [list.Single()];
                }

                return [.. list];
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

        #region IDurableListLookupCommandHandler

        void IDurableListLookupCommandHandler<TKey, TValue>.ApplyAdd(TKey key, TValue value) => ApplyAdd(key, value);
        void IDurableListLookupCommandHandler<TKey, TValue>.ApplyRemoveKey(TKey key) => ApplyRemoveKey(key);
        void IDurableListLookupCommandHandler<TKey, TValue>.ApplyRemoveItem(TKey key, TValue value) => ApplyRemoveItem(key, value);
        void IDurableListLookupCommandHandler<TKey, TValue>.ApplyClear() => ApplyClear();
        void IDurableListLookupCommandHandler<TKey, TValue>.Reset(int capacityHint)
        {
            ApplyClear();
            _items.EnsureCapacity(capacityHint);
        }

        #endregion

        public bool Contains(TKey key) => _items.ContainsKey(key);

        public bool Contains(TKey key, TValue value) => _items.TryGetValue(key, out var list) && list.Contains(value);

        public void Add(TKey key, TValue value)
        {
            _codec.WriteAdd(key, value, GetWriter());
            ApplyAdd(key, value);
        }

        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            foreach (var value in values)
            {
                Add(key, value);
            }
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
            if (!_items.TryGetValue(key, out var list))
            {
                list = new ValueList(null);
            }

            _items[key] = list.Add(value);
        }

        private void ApplyRemoveItem(TKey key, TValue value)
        {
            if (!_items.TryGetValue(key, out ValueList list))
            {
                return;
            }

            var updated = list.Remove(value);

            if (updated.Equals(list))
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
        /// Initializes a new <see cref="ValueList"/> holding either a single value or,
        /// once more than one element is added, an <see cref="ImmutableList{TValue}"/>.
        /// We use <see cref="ImmutableList{TValue}"/> rather than <see cref="List{TValue}"/> so that:
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
        /// We avoid unnecessary allocations for the very common “single‐value” case,
        /// falling back to an <see cref="ImmutableList{TValue}"/> only when needed.
        /// </item>
        /// </list>
        /// </summary>
        private readonly struct ValueList(object? value) : IEnumerable<TValue>
        {
            /// <summary>
            /// Stores either a single value or an <see cref="ImmutableList{TValue}"/>, we avoid allocating a collection
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

                    if (_value is not ImmutableList<TValue> list)
                    {
                        return 1;
                    }

                    return list.Count;
                }
            }

            public bool Contains(TValue value)
            {
                if (_value is null)
                {
                    return false;
                }

                if (_value is ImmutableList<TValue> list)
                {
                    return list.Contains(value);
                }

                // Otherwise, we are storing a single item.
                return EqualityComparer<TValue>.Default.Equals((TValue)_value, value);
            }

            public ValueList Add(TValue value)
            {
                if (_value is null)
                {
                    // No existing value, we store it directly to avoid list allocation.
                    return new ValueList(value);
                }

                // If currently a single value, we wrap it into a 1‑element list; otherwise we cast.
                var list = _value as ImmutableList<TValue> ?? [(TValue)_value];

                // We always append the new value (as list allows duplicates)
                return new ValueList(list.Add(value));
            }

            public ValueList Remove(TValue value)
            {
                // If there's no value stored, there's nothing to remove.
                if (_value is null)
                {
                    return this;
                }

                // If we are storing a single value, compare it directly.
                if (_value is TValue single)
                {
                    // If it matches, we remove it by clearing the stored value. Otherwise, nothing changes.
                    return EqualityComparer<TValue>.Default.Equals(single, value) ? new ValueList(null) : this;
                }

                // We are storing a list, so attempt to remove the value from it.
                var list = (ImmutableList<TValue>)_value;
                var newList = list.Remove(value); // Only removes the first match

                // If the list is now empty, clear the stored value entirely.
                if (newList.Count == 0)
                {
                    return new ValueList(null);
                }

                // If only one item remains, store it directly instead of keeping a list.
                if (newList.Count == 1)
                {
                    return new ValueList(newList[0]);
                }

                // Otherwise, store the updated list.
                return new ValueList(newList);
            }


            public TValue Single()
            {
                Debug.Assert(_value is TValue);
                return (TValue)_value;
            }

            public bool Equals(ValueList other) => _value == other._value;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            public Enumerator GetEnumerator() => new(this);

            public struct Enumerator : IEnumerator<TValue>
            {
                private int _count;
                [AllowNull] private readonly TValue _value;
                private ImmutableList<TValue>.Enumerator _enumerator;

                object? IEnumerator.Current => Current;
                public TValue Current => _count > 1 ? _enumerator.Current : _value;

                internal Enumerator(ValueList valueList)
                {
                    if (valueList._value == null)
                    {
                        _value = default;
                        _enumerator = default;
                        _count = 0;
                    }
                    else
                    {
                        if (valueList._value is ImmutableList<TValue> list)
                        {
                            _value = default;
                            _enumerator = list.GetEnumerator();
                            _count = list.Count;

                            Debug.Assert(_count > 1);
                        }
                        else
                        {
                            _value = (TValue)valueList._value;
                            _enumerator = default;
                            _count = 1;
                        }
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
