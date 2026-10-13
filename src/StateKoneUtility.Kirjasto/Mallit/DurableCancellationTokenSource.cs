using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections.Generic;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{
    internal sealed class DurableCancellationTokenSource :
    IDurableCancellationTokenSource,
    IDurableCancellationTokenSourceCommandHandler,
    IStateMachine,
    IDisposable
    {
        private bool _isCanceled;
        private ScheduledCancellation _scheduledCancellation;
        private CancellationTokenSource _cts;
        private CancellationTokenRegistration _ctsr;

        private readonly ReaderWriterLockSlim _lock = new();
        private readonly TimeProvider _timeProvider;
        private readonly IJournaledStateManager _manager;
        private readonly IDurableCancellationTokenSourceCommandCodec _codec;

        private struct ScheduledCancellation
        {
            /// <summary>
            /// When the request was made.
            /// </summary>
            public long RequestTicks;

            /// <summary>
            /// The original delay requested.
            /// </summary>
            public long DelayTicks;

            public readonly bool IsScheduled => RequestTicks != -1;
            public readonly long ExpirationTicks => RequestTicks + DelayTicks;

            /// <summary>
            /// If a scheduled cancellation exists, this resets/invalidates it.
            /// </summary>
            public void Reset()
            {
                DelayTicks = -1;
                RequestTicks = -1;
            }
        }

        public DurableCancellationTokenSource(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options,
            TimeProvider timeProvider, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            _timeProvider = timeProvider;
            _manager = manager;
            _codec = Apulaiset.GetCodec<IDurableCancellationTokenSourceCommandCodec>(serviceProvider, options);
            _cts = new(Timeout.InfiniteTimeSpan, _timeProvider);
            _ctsr = ObserveCancellation();
            _scheduledCancellation.Reset();

            manager.RegisterStateMachine(key, this);
        }

        public CancellationToken Token => _cts.Token;

        public bool IsCancellationPending
        {
            get
            {
                _lock.EnterReadLock();

                try
                {
                    if (_isCanceled)
                    {
                        return true;
                    }

                    if (!_scheduledCancellation.IsScheduled)
                    {
                        return false;
                    }

                    // We calculate the expiration time based on the original request and check if it has passed.
                    // This returns true if the calculated expiration time is less than, or equal to the current time,
                    // meaning the scheduled cancellation moment has been reached or has passed.

                    return _scheduledCancellation.ExpirationTicks <= _timeProvider.GetUtcNow().UtcTicks;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #region IStateMachine

        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            if (_isCanceled || _scheduledCancellation.IsScheduled)
            {
                // We apply the write operation only if we did *change* our default state i.e:
                // non-canceled & no cancellation scheduled, making this slightly more space efficient.

                _codec.WriteState(_isCanceled, _scheduledCancellation.IsScheduled, _scheduledCancellation.RequestTicks, _scheduledCancellation.DelayTicks, writer);
            }
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter)
        {
            _codec.WriteState(_isCanceled, _scheduledCancellation.IsScheduled, _scheduledCancellation.RequestTicks, _scheduledCancellation.DelayTicks, snapshotWriter);
        }

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            if (_cts.IsCancellationRequested)
            {
                _ctsr.Dispose();
                _cts.Dispose();

                // It is best we crate a new CTS upon reseting, because we publicly expose cts.Token, and we cannot "un-cancel" its source
                // without violating the contract with consumers who may have registered callbacks, or have passed the token to other operations.

                _cts = new(Timeout.InfiniteTimeSpan, _timeProvider);
                _ctsr = ObserveCancellation();
            }

            _isCanceled = false;
            _scheduledCancellation.Reset();
        }

        void IStateMachine.OnWriteCompleted()
        {
            if (IsCancellationPending)
            {
                _cts.Cancel();
            }
        }

        void IStateMachine.OnRecoveryCompleted()
        {
            if (IsCancellationPending)
            {
                _cts.Cancel();
                return;
            }

            if (_scheduledCancellation.IsScheduled)
            {
                var timeElapsedTicks = _timeProvider.GetUtcNow().UtcTicks - _scheduledCancellation.RequestTicks;
                var remainingTicks = _scheduledCancellation.DelayTicks - timeElapsedTicks;

                if (remainingTicks <= 0)
                {
                    _cts.Cancel(); // The scheduled time has passed while deactivated. Trigger cancellation now.
                }
                else
                {
                    _cts.CancelAfter(TimeSpan.FromTicks(remainingTicks)); // The scheduled time is still in the future. Re-arm the in-memory timer.
                }
            }
        }

        #endregion

        #region IDurableCancellationTokenSourceCommandHandler

        void IDurableCancellationTokenSourceCommandHandler.ApplyState(bool isCanceled, bool isScheduled, long requestTicks, long delayTicks)
        {
            _isCanceled = isCanceled;
            if (isScheduled)
            {
                _scheduledCancellation.RequestTicks = requestTicks;
                _scheduledCancellation.DelayTicks = delayTicks;
            }
            else
            {
                _scheduledCancellation.Reset();
            }
        }

        void IDurableCancellationTokenSourceCommandHandler.Reset()
        {
            _isCanceled = false;
            _scheduledCancellation.Reset();
        }

        #endregion

        public void Cancel()
        {
            _lock.EnterWriteLock();

            try
            {
                _isCanceled = true;
                _scheduledCancellation.Reset();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void CancelAfter(TimeSpan delay)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero, nameof(delay));

            var utcNowTicks = _timeProvider.GetUtcNow().UtcTicks;
            var newExpirationTicks = utcNowTicks + delay.Ticks;

            _lock.EnterWriteLock();

            try
            {
                // We only set the new schedule if there is none, or if it is sooner than the existing one.
                if (!_scheduledCancellation.IsScheduled || newExpirationTicks < _scheduledCancellation.ExpirationTicks)
                {
                    _scheduledCancellation.RequestTicks = utcNowTicks;
                    _scheduledCancellation.DelayTicks = delay.Ticks;

                    _cts.CancelAfter(delay);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _lock.EnterWriteLock();

            try
            {
                _ctsr.Dispose();
                _cts.Dispose();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <remarks>
        /// <para>
        /// We register ourselves as a callback, in addition to potential other sources.
        /// When a scheduled cancellation is requested, we need to update our in-memory state
        /// (durability comes after WriteStateAsync is called) to reflect the scheduled cancellation has occurred.
        /// </para>
        /// <para>
        /// We use <see cref="CancellationToken.UnsafeRegister(Action{object?}, object?)"/> because the <see cref="IJournaledStateManager"/>
        /// already suppresses the execution context, so there is no context to flow anyway.
        /// </para>
        /// </remarks>
        private CancellationTokenRegistration ObserveCancellation() =>
            Token.UnsafeRegister(static async state =>
            {
                var self = (DurableCancellationTokenSource)state!;
                var rwLock = self._lock;

                bool isCanceled;
                ScheduledCancellation scheduled;

                rwLock.EnterReadLock();

                try
                {
                    isCanceled = self._isCanceled;
                    scheduled = self._scheduledCancellation;
                }
                finally
                {
                    rwLock.ExitReadLock();
                }

                self.Cancel(); // This sets: _isCanceled = true, and resets _scheduledCancellation too.

                try
                {
                    await self._manager.WriteStateAsync(CancellationToken.None);
                }
                catch
                {
                    // Something was wrong with persisting, so we restore the state (undo Cancel), conditionally!
                    rwLock.EnterWriteLock();

                    try
                    {
                        // We should only rollback if the state is still the one we set above.
                        // When we call WriteStateAsync, we yield back, and a call to CancelAfter could update the state.
                        // In that case, we should not rollback, as we would be destroying a more recent state change.

                        if (self._isCanceled && !self._scheduledCancellation.IsScheduled)
                        {
                            // The state is still the one we set, so we perform the rollback now.
                            self._isCanceled = isCanceled;
                            self._scheduledCancellation = scheduled;
                        }
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }
                }
            }, this);
    }
}
