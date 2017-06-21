// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class ReadOnlyIntermediateNodeCollection : IntermediateNodeCollection
    {
        public static readonly ReadOnlyIntermediateNodeCollection Instance = new ReadOnlyIntermediateNodeCollection();

        private ReadOnlyIntermediateNodeCollection()
        {
        }

        public override IntermediateNode this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                throw null; // Unreachable
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                throw null; // Unreachable
            }
        }

        public override int Count => 0;

        public override bool IsReadOnly => true;

        public override void Add(IntermediateNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            throw new NotSupportedException();
        }

        public override void Clear()
        {
            throw new NotSupportedException();
        }

        public override bool Contains(IntermediateNode item)
        {
            return false;
        }

        public override void CopyTo(IntermediateNode[] array, int arrayIndex)
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

            throw new NotSupportedException();
        }

        public override IEnumerator<IntermediateNode> GetEnumerator()
        {
            return Enumerable.Empty<IntermediateNode>().GetEnumerator();
        }

        public override int IndexOf(IntermediateNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return -1;
        }

        public override void Insert(int index, IntermediateNode item)
        {
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            throw new NotSupportedException();
        }

        public override bool Remove(IntermediateNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return false;
        }

        public override void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            throw new NotSupportedException();
        }
    }
}
