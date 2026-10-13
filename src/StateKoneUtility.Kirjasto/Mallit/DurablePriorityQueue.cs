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
    [DebuggerTypeProxy(typeof(DurablePriorityQueueDebugView<,>))]
    internal sealed class DurablePriorityQueue<TElement, TPriority> :
    IDurablePriorityQueue<TElement, TPriority>,
    IDurablePriorityQueueCommandHandler<TElement, TPriority>,
    IStateMachine
    {
        private JournalStreamWriter? _writer;
        private readonly PriorityQueue<TElement, TPriority> _items = new();
        private readonly IDurablePriorityQueueCommandCodec<TElement, TPriority> _codec;

        public DurablePriorityQueue(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurablePriorityQueueCommandCodec<TElement, TPriority>>(serviceProvider, options);
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

        #region IDurablePriorityQueueCommandHandler

        void IDurablePriorityQueueCommandHandler<TElement, TPriority>.ApplyEnqueue(TElement element, TPriority priority) => _items.Enqueue(element, priority);
        void IDurablePriorityQueueCommandHandler<TElement, TPriority>.ApplyDequeue() => _items.Dequeue();
        void IDurablePriorityQueueCommandHandler<TElement, TPriority>.ApplyTryDequeue() => _items.TryDequeue(out _, out _);
        void IDurablePriorityQueueCommandHandler<TElement, TPriority>.ApplyClear() => _items.Clear();
        void IDurablePriorityQueueCommandHandler<TElement, TPriority>.Reset(int capacityHint)
        {
            _items.Clear();
            _items.EnsureCapacity(capacityHint);
        }

        #endregion

        public TElement Peek() => _items.Peek();

        public bool TryPeek(out TElement element, out TPriority priority) => _items.TryPeek(out element!, out priority!);

        public void Enqueue(TElement element, TPriority priority)
        {
            _codec.WriteEnqueue(element, priority, GetWriter());
            _items.Enqueue(element, priority);
        }

        public TElement Dequeue()
        {
            var result = _items.Peek(); // If the queue is empty, this throws before we touch the journal stream.

            _codec.WriteDequeue(GetWriter());
            _items.Dequeue();

            return result;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            // We only want to write to the journal if the queue is not empty.
            // We use TryPeek to check without mutating the state yet.

            if (!_items.TryPeek(out element, out priority))
            {
                return false;
            }

            _codec.WriteDequeue(GetWriter());
            _items.TryDequeue(out _, out _);

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

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<(TElement, TPriority)> GetEnumerator() => _items.UnorderedItems.GetEnumerator();
    }
}
