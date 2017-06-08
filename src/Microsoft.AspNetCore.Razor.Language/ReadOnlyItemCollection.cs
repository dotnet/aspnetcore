// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class ReadOnlyItemCollection : ItemCollection
    {
        public static readonly ItemCollection Empty = new ReadOnlyItemCollection();

        public override object this[object key]
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public override int Count => 0;

        public override bool IsReadOnly => true;

        public override void Add(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }

            throw new NotSupportedException();
        }

        public override void Clear()
        {
            throw new NotSupportedException();
        }

        public override bool Contains(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }

            return false;
        }

        public override void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            else if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            // Do nothing.
        }

        public override IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return Enumerable.Empty<KeyValuePair<object, object>>().GetEnumerator();
        }

        public override bool Remove(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }

            throw new NotSupportedException();
        }
    }
}
