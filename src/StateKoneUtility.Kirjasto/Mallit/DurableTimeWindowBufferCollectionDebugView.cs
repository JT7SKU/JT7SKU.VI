using System.Diagnostics;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableTimeWindowBufferCollectionDebugView<TKey, TValue>(
        DurableTimeWindowBufferCollection<TKey, TValue> collection)
            where TKey : notnull
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue[]>[] Items
        {
            get
            {
                var i = 0;
                var result = new KeyValuePair<TKey, TValue[]>[collection.Count];

                foreach (var key in collection.Keys)
                {
                    var proxy = collection.GetOrCreateProxy(key);
                    result[i++] = new KeyValuePair<TKey, TValue[]>(key, [.. proxy]);
                }

                return result;
            }
        }
    }
}
