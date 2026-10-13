namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurablePriorityQueueCommandHandler<TElement, TPriority>
    {
        /// <summary>Applies an enqueue command.</summary>
        void ApplyEnqueue(TElement element, TPriority priority);

        /// <summary>Applies a dequeue command.</summary>
        void ApplyDequeue();

        /// <summary>Applies a try dequeue command.</summary>
        void ApplyTryDequeue();

        /// <summary>Applies a clear command.</summary>
        void ApplyClear();

        /// <summary>Resets the receiver before applying replacement entries.</summary>
        void Reset(int capacityHint);
    }

}
