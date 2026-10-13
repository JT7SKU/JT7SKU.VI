using StateKoneUtility.Kirjasto.Rajapinnat;
using System.Buffers;
using System.Diagnostics;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableTreeDebugView<T>(IDurableTree<T> tree) where T : notnull
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DebugViewItem[] Items => tree.IsEmpty ? [] : BuildDebugView(tree.Root, tree);

        private static DebugViewItem[] BuildDebugView(T value, IDurableTree<T> tree)
        {
            var children = tree.GetChildren(value).Select(c =>
            {
                var grandChildren = BuildDebugView(c, tree);
                return new DebugViewItem(c, grandChildren);
            }).ToArray();

            return children;
        }

        [DebuggerDisplay("{Value}, Children = {Children.Length}")]
        internal readonly struct DebugViewItem(T value, DebugViewItem[] children)
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public T Value { get; } = value;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public DebugViewItem[] Children { get; } = children;
        }
    }

}


