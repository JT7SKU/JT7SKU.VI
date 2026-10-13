using System;
using System.Collections.Generic;
using System.Text;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableObject<T> where T : class, new()
    {
        /// <summary>
        /// <para>Gets or sets the state object. This property will never return <c>null</c>.</para>
        /// <para>When getting, if the state has not been set or loaded, a new instance is created.</para>
        /// <para>When setting, the state is unconditionally marked for persistence on the next WriteStateAsync call.</para>
        /// </summary>
        /// <remarks>Null values are not allowed.</remarks>
        /// <exception cref="ArgumentNullException"/>
        T Value { get; set; }

        /// <summary>
        /// Gets a value indicating whether the state record was loaded from storage, or has been successfully written at least once.
        /// </summary>
        bool RecordExists { get; }
    }
}
