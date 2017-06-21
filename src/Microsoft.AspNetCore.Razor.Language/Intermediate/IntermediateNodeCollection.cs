// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class IntermediateNodeCollection : IList<IntermediateNode>
    {
        public abstract IntermediateNode this[int index] { get; set; }

        public abstract int Count { get; }

        public abstract bool IsReadOnly { get; }

        public abstract void Add(IntermediateNode item);

        public abstract void Clear();

        public abstract bool Contains(IntermediateNode item);

        public abstract void CopyTo(IntermediateNode[] array, int arrayIndex);

        public abstract IEnumerator<IntermediateNode> GetEnumerator();

        public abstract int IndexOf(IntermediateNode item);

        public abstract void Insert(int index, IntermediateNode item);

        public abstract bool Remove(IntermediateNode item);

        public abstract void RemoveAt(int index);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
