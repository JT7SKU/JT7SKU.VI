using System.Diagnostics;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableTimeWindowBufferDebugView<T>(DurableTimeWindowBuffer<T> buffer)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => [.. buffer];
    }

}
