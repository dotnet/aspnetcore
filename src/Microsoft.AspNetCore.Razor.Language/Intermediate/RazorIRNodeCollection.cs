// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class RazorIRNodeCollection : IList<RazorIRNode>
    {
        public abstract RazorIRNode this[int index] { get; set; }

        public abstract int Count { get; }

        public abstract bool IsReadOnly { get; }

        public abstract void Add(RazorIRNode item);

        public abstract void Clear();

        public abstract bool Contains(RazorIRNode item);

        public abstract void CopyTo(RazorIRNode[] array, int arrayIndex);

        public abstract IEnumerator<RazorIRNode> GetEnumerator();

        public abstract int IndexOf(RazorIRNode item);

        public abstract void Insert(int index, RazorIRNode item);

        public abstract bool Remove(RazorIRNode item);

        public abstract void RemoveAt(int index);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
