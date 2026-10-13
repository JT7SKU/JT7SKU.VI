using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Journaling;
using StateKoneUtility.Kirjasto.Rajapinnat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace StateKoneUtility.Kirjasto.Mallit
{

    [DebuggerDisplay("RecordExists: {RecordExists}, Value: {Value}")]
    internal sealed class DurableObject<T> :
    IDurableObject<T>,
    IDurableObjectCommandHandler<T>,
    IStateMachine where T : class, new()
    {
        private T? _value;
        private bool _exists;
        private readonly IDurableObjectCommandCodec<T> _codec;

        public DurableObject(
            [ServiceKey] string key, IJournaledStateManager manager,
            IOptions<JournaledStateManagerOptions> options, IServiceProvider serviceProvider)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _codec = Apulaiset.GetCodec<IDurableObjectCommandCodec<T>>(serviceProvider, options);
            manager.RegisterStateMachine(key, this);
        }

        public T Value
        {
            get => _value ??= new();
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _value = value;
            }
        }

        public bool RecordExists => _exists;

        #region IStateMachine


        void IStateMachine.ReplayEntry(JournalEntry entry, JournalReplayContext context) =>
            context.GetRequiredCommandCodec(entry.FormatKey, _codec).Apply(entry.Reader, this);

        void IStateMachine.WritePendingEntries(JournalStreamWriter writer)
        {
            // We always write the state when AppendEntries is called, because the state
            // object is mutated directly, and we can't easily track if it's dirty or not.
            // The whole goal of this component is the convenient direct mutation.
            _codec.WriteState(Value, writer);
        }

        void IStateMachine.WriteSnapshot(JournalStreamWriter snapshotWriter) => _codec.WriteState(Value, snapshotWriter);

        void IStateMachine.Reset(JournalStreamWriter writer)
        {
            _value = null;
            _exists = false;
        }

        void IStateMachine.OnWriteCompleted() => _exists = true;

        #endregion

        #region IDurableObjectCommandHandler

        void IDurableObjectCommandHandler<T>.ApplyState(T value)
        {
            _value = value;
            _exists = true;
        }

        void IDurableObjectCommandHandler<T>.Reset()
        {
            _value = null;
            _exists = false;
        }

        #endregion
    }
}