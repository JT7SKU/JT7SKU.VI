using System.Diagnostics;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableOrderedSetLookupDebugView<TKey, TValue>(DurableOrderedSetLookup<TKey, TValue> lookup) where TKey : notnull
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DebugViewItem[] Items => lookup.Select(kvp => new DebugViewItem(kvp.Item1, kvp.Item2)).ToArray();

        [DebuggerDisplay("[{Key}] Count = {Values.Length}")]
        internal readonly struct DebugViewItem(TKey key, IReadOnlyCollection<TValue> values)
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public TKey Key { get; } = key;

            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public TValue[] Values { get; } = [.. values];
        }
    }
}
