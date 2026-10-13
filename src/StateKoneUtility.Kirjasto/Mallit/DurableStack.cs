using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace StateKoneUtility.Kirjasto.Mallit
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(DurableStackDebugView<>))]
    internal sealed class DurableStack<T> :
        IDurableStack<T>,
        IDurableStackCommandHandler<T>,
        IStateMachine
    {
        private JournalStreamWriter? _writer;
        private readonly Stack<T> _items = new();
        private readonly IDurableStackCommandCodec<T> _codec;

        public DurableStack(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableStackCommandCodec<T>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public int Count => _items.Count;

        #region IStateMachine


        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We use a push model, and appened entries upon modification.
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter) => _codec.WriteSnapshot(this, snapshotWriter);

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            _items.Clear();
            _writer = writer;
        }

        #endregion

        #region IDurableStackCommandHandler

        void IDurableStackCommandHandler<T>.ApplyPush(T item) => _items.Push(item);
        void IDurableStackCommandHandler<T>.ApplyPop() => _items.Pop();
        void IDurableStackCommandHandler<T>.ApplyClear() => _items.Clear();
        void IDurableStackCommandHandler<T>.Reset(int capacityHint)
        {
            _items.Clear();
            _items.EnsureCapacity(capacityHint);
        }

        #endregion

        public void Push(T item)
        {
            _codec.WritePush(item, GetWriter());
            _items.Push(item);
        }

        public T Pop()
        {
            var result = _items.Peek(); // If the queue is empty, this throws before we touch the journal stream.

            _codec.WritePop(GetWriter());
            _items.Pop();

            return result;
        }

        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            if (!_items.TryPeek(out item))
            {
                return false;
            }

            _codec.WritePop(GetWriter());
            _items.Pop();

            return true;
        }

        public void Clear()
        {
            _codec.WriteClear(GetWriter());
            _items.Clear();
        }

        private JournalStreamWriter GetWriter()
        {
            Debug.Assert(_writer.HasValue);
            return _writer.Value;
        }

        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public T Peek() => _items.Peek();
        public bool TryPeek([MaybeNullWhen(false)] out T item) => _items.TryPeek(out item);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    }
}

