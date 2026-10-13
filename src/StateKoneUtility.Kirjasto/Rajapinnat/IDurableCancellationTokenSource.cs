namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableCancellationTokenSource
    {
        /// <summary>
        /// Gets the <see cref="CancellationToken"/> associated with this source.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This token is signaled only <strong>after</strong> a cancellation request has been durably persisted.
        /// It represents the <strong>committed</strong> cancellation state. For the immediate, in-memory status, check the
        /// <see cref="IsCancellationPending"/> property.
        /// </para>
        /// <para>
        /// Any callbacks registered via <see cref="CancellationToken.Register(Action)"/> are executed <strong>after</strong>
        /// the cancellation request has been durably persisted.
        /// </para>
        /// <para>
        /// Any callbacks registered via <see cref="CancellationToken.Register(Action)"/> are executed on the <see cref="TaskScheduler.Default"/>.
        /// In the majority of the cases this will be the <see cref="ThreadPool"/>, in other words the callbacks will execute <strong>outside</strong> the grain context.
        /// </para>
        /// </remarks>
        CancellationToken Token { get; }

        /// <summary>
        /// Gets a value indicating whether cancellation is pending for this source.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property returns <see langword="true"/> if <see cref="Cancel()"/> has been called, or if the delay
        /// from a prior <see cref="CancelAfter(TimeSpan)"/> call has elapsed.
        /// </para>
        /// <para>
        /// This value reflects the current computed state and can be <see langword="true"/> <strong>before</strong> the state has been
        /// durably persisted. It is useful for checking the cancellation status before the state is saved durably.
        /// </para>
        /// </remarks>
        bool IsCancellationPending { get; }

        /// <summary>
        /// Communicates a request for cancellation.
        /// </summary>
        /// <remarks>
        /// This method only updates the in-memory state. The cancellation request is not durable and will be lost
        /// on deactivation until an explicit persistence call is successfully completed.
        /// Upon successful persistence, the <see cref="Token"/> will be signaled.
        /// </remarks>
        void Cancel();

        /// <summary>
        /// Schedules a cancellation request to occur after <paramref name="delay"/> has elapsed.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling. Must be a non-negative value.</param>
        /// <remarks>
        /// <para>
        /// This method initiates a two-step cancellation process that distinguishes it from <see cref="Cancel()"/>:
        /// </para>
        /// <para>
        /// <strong>Persisting the Intent:</strong> This method only records the <strong>intent</strong> to cancel in memory.
        /// To make this scheduled cancellation durable, you must subsequently call <i>WriteStateAsync</i>.
        /// If this is not done, the scheduled cancellation will be lost upon deactivation.
        /// </para>
        /// <para>
        /// <strong>Automatic Persistence on Expiration:</strong> Once the <paramref name="delay"/> expires, this
        /// component will automatically trigger an internal process to durably persist the final state.
        /// This is a key difference from <see cref="Cancel()"/>, which requires a <strong>manual</strong> persistence call to
        /// finalize the cancellation.
        /// </para>
        /// <para>
        /// <strong>Durability Across Deactivation:</strong> If the host is deactivated after the intent has been persisted
        /// but before the <paramref name="delay"/> elapses, the state machine guarantees that the remaining time will be
        /// honored upon the next activation, after which the final state will be automatically persisted.
        /// </para>
        /// <para>
        /// <strong>Behavior Notes:</strong> If this method is called multiple times, only the request with the earliest
        /// cancellation time is honored. A subsequent call with a longer delay will not override an existing, shorter delay.
        /// A delay of <see cref="TimeSpan.Zero"/> will immediately trigger the automatic persistence of the state,
        /// behaving like an auto-committing version of <see cref="Cancel()"/>.
        /// </para>
        /// </remarks>
        void CancelAfter(TimeSpan delay);
    }
}
