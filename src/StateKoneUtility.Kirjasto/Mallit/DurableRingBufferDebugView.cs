using System.Diagnostics;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableRingBufferDebugView<T>(DurableRingBuffer<T> buffer)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => [.. buffer];
    }

}
