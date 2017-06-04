// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class ItemCollection : ICollection<KeyValuePair<object, object>>
    {
        public abstract object this[object key] { get; set; }

        public abstract int Count { get; }

        public abstract bool IsReadOnly { get; }

        public abstract void Add(KeyValuePair<object, object> item);

        public abstract void Clear();

        public abstract bool Contains(KeyValuePair<object, object> item);

        public abstract void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex);

        public abstract IEnumerator<KeyValuePair<object, object>> GetEnumerator();

        public abstract bool Remove(KeyValuePair<object, object> item);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
