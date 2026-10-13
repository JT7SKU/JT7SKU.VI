namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableOrderedSetCommandHandler<T>
    {
        /// <summary>Applies an add command.</summary>
        void ApplyAdd(T item);

        /// <summary>Applies a remove command.</summary>
        void ApplyRemove(T item);

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset(int capacityHint);
    }


}
