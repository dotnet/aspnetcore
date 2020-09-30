// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    /// <summary>
    /// A base class that for an event.
    /// </summary>
    public abstract class EventData : IReadOnlyList<KeyValuePair<string, object>>
    {
        /// <summary>
        /// The namespace of the event.
        /// </summary>
        protected const string EventNamespace = "Microsoft.AspNetCore.Mvc.";

        /// <summary>
        /// The event count.
        /// </summary>
        protected abstract int Count { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected abstract KeyValuePair<string, object> this[int index] { get; }

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => Count;
        KeyValuePair<string, object> IReadOnlyList<KeyValuePair<string, object>>.this[int index] => this[index];

        Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        /// A struct that represents an Enumerator
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private readonly EventData _eventData;
            private readonly int _count;

            private int _index;

            /// <summary>
            /// Current keyvalue pair.
            /// </summary>
            public KeyValuePair<string, object> Current { get; private set; }

            internal Enumerator(EventData eventData)
            {
                _eventData = eventData;
                _count = eventData.Count;
                _index = -1;
                Current = default;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                var index = _index + 1;
                if (index >= _count)
                {
                    return false;
                }

                _index = index;

                Current = _eventData[index];
                return true;
            }

            /// <inheritdoc/>
            public void Dispose() { }
            object IEnumerator.Current => Current;
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
