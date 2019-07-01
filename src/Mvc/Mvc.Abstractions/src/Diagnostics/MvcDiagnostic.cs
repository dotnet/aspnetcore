// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public abstract class MvcDiagnostic : IReadOnlyList<KeyValuePair<string, object>>
    {
        protected const string EventNamespace = "Microsoft.AspNetCore.Mvc.";

        protected abstract int Count { get; }
        protected abstract KeyValuePair<string, object> this[int index] { get; }

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => Count;
        KeyValuePair<string, object> IReadOnlyList<KeyValuePair<string, object>>.this[int index] => this[index];

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            var count = Count;
            for (var i = 0; i < count; i++)
            {
                yield return this[i];
            }
        }
    }
}