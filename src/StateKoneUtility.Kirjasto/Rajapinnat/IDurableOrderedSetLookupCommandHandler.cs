namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableOrderedSetLookupCommandHandler<TKey, TValue>
    {
        /// <summary>Applies an add command.</summary>
        void ApplyAdd(TKey key, TValue value);

        /// <summary>Applies a remove key command.</summary>
        void ApplyRemoveKey(TKey key);

        /// <summary>Applies a remove item command.</summary>
        void ApplyRemoveItem(TKey key, TValue value);

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset(int capacityHint);
    }


}
