// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class RazorDiagnosticCollection : IList<RazorDiagnostic>
    {
        public abstract RazorDiagnostic this[int index] { get; set; }

        public abstract int Count { get; }

        public abstract bool IsReadOnly { get; }

        public abstract void Add(RazorDiagnostic item);

        public abstract void Clear();

        public abstract bool Contains(RazorDiagnostic item);

        public abstract void CopyTo(RazorDiagnostic[] array, int arrayIndex);

        public abstract IEnumerator<RazorDiagnostic> GetEnumerator();

        public abstract int IndexOf(RazorDiagnostic item);

        public abstract void Insert(int index, RazorDiagnostic item);

        public abstract bool Remove(RazorDiagnostic item);

        public abstract void RemoveAt(int index);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
