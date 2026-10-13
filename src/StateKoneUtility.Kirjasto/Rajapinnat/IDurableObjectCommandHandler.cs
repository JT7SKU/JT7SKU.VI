namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableObjectCommandHandler<T>
    {
        /// <summary>Applies a state command.</summary>
        void ApplyState(T value);

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset();
    }
}
