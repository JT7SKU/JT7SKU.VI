namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableTreeCommandHandler<T>
    {
        /// <summary>Applies a set root command.</summary>
        void ApplySetRoot(T root);

        /// <summary>Applies an add command.</summary>
        void ApplyAdd(T parent, T value);

        /// <summary>Applies a remove command.</summary>
        void ApplyRemove(T value);

        /// <summary>Applies a move command.</summary>
        void ApplyMove(T value, T parent);

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset();
    }
}
