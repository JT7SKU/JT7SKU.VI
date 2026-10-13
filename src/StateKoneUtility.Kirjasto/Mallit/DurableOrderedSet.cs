using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{

    [DebuggerDisplay("Count = {Count}")]
    internal sealed class DurableOrderedSet<T> :
    IDurableOrderedSet<T>,
    IDurableOrderedSetCommandHandler<T>,
    IStateMachine
    {
        private JournalStreamWriter? _writer;
        private readonly HashSet<T> _set = [];
        private readonly List<T> _list = [];
        private readonly IDurableOrderedSetCommandCodec<T> _codec;

        public DurableOrderedSet(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableOrderedSetCommandCodec<T>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count
        {
            get
            {
                Debug.Assert(_set.Count == _list.Count);
                return _set.Count;
            }
        }

        public ReadOnlySpan<T> OrderedItems => CollectionsMarshal.AsSpan(_list);

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

        #region IDurableOrderedSetCommandHandler

        void IDurableOrderedSetCommandHandler<T>.ApplyAdd(T item) => ApplyAdd(item);
        void IDurableOrderedSetCommandHandler<T>.ApplyRemove(T item) => ApplyRemove(item);
        void IDurableOrderedSetCommandHandler<T>.ApplyClear() => ApplyClear();
        void IDurableOrderedSetCommandHandler<T>.Reset(int capacityHint)
        {
            ApplyClear();
            _set.EnsureCapacity(capacityHint);
            _list.Capacity = capacityHint;
        }

        #endregion

        // We use the list's CopyTo in order to preserve the order when the elements are copied to the destination array.
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public bool Contains(T item) => _set.Contains(item);
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue) => _set.TryGetValue(equalValue, out actualValue);

        public bool Add(T item)
        {
            if (_set.Contains(item))
            {
                return false;
            }

            _codec.WriteAdd(item, GetWriter());
            ApplyAdd(item);

            return true;
        }

        public bool Remove(T item)
        {
            if (!_set.Contains(item))
            {
                return false;
            }

            _codec.WriteRemove(item, GetWriter());
            ApplyRemove(item);

            return true;
        }

        public void Clear()
        {
            _codec.WriteClear(GetWriter());
            ApplyClear();
        }

        private void ApplyAdd(T item)
        {
            _set.Add(item);
            _list.Add(item);
        }

        private void ApplyRemove(T item)
        {
            _set.Remove(item);
            _list.Remove(item);
        }

        private void ApplyClear()
        {
            _set.Clear();
            _list.Clear();
        }

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }

        // We return the list enumerator since this type is an ordered collection.
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
