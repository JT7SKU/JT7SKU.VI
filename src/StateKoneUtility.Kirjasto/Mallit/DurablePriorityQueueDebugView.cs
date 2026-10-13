using System.Diagnostics;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurablePriorityQueueDebugView<TElement, TPriority>(DurablePriorityQueue<TElement, TPriority> queue)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public (TElement Element, TPriority Priority)[] Items
        {
            get
            {
                int i = 0;
                var result = new (TElement, TPriority)[queue.Count];

                foreach (var pair in queue)
                {
                    result[i++] = pair;
                }

                return result;
            }
        }
    }
}
