// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public abstract class MvcDiagnostic : IReadOnlyList<KeyValuePair<string, object>>
    {
        protected const string EventNamespace = "Microsoft.AspNetCore.Mvc.";

        public abstract int Count { get; }
        public abstract KeyValuePair<string, object> this[int index] { get; }
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            var count = Count;
            for (var i = 0; i < count; i++)
            {
                yield return this[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}