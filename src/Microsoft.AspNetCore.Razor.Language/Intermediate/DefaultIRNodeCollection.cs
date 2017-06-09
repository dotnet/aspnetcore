// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class DefaultIRNodeCollection : RazorIRNodeCollection
    {
        private readonly List<RazorIRNode> _inner = new List<RazorIRNode>();

        public override RazorIRNode this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _inner[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                _inner[index] = value;
            }
        }

        public override int Count => _inner.Count;

        public override bool IsReadOnly => false;

        public override void Add(RazorIRNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _inner.Add(item);
        }

        public override void Clear()
        {
            _inner.Clear();
        }

        public override bool Contains(RazorIRNode item)
        {
            return _inner.Contains(item);
        }

        public override void CopyTo(RazorIRNode[] array, int arrayIndex)
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

            _inner.CopyTo(array, arrayIndex);
        }

        public override IEnumerator<RazorIRNode> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public override int IndexOf(RazorIRNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return _inner.IndexOf(item);
        }

        public override void Insert(int index, RazorIRNode item)
        {
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _inner.Insert(index, item);
        }

        public override bool Remove(RazorIRNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return _inner.Remove(item);
        }

        public override void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _inner.RemoveAt(index);
        }
    }
}
