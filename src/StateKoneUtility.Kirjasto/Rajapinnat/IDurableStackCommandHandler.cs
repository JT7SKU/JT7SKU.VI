namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    /// <summary>
    /// Receives decoded durable stack commands from a codec implementation.
    /// </summary>
    public interface IDurableStackCommandHandler<T>
    {
        /// <summary>Applies a push command.</summary>
        void ApplyPush(T item);

        /// <summary>Applies a pop command.</summary>
        void ApplyPop();

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset(int capacityHint);
    }
}

